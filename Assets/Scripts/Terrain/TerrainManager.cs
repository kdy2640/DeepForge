using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Rendering;
#endif

// 지형 밀도 생성, 청크 메시 갱신과 대상 주변 스트리밍을 관리한다.
[DisallowMultipleComponent]
public class TerrainManager : MonoBehaviour
{
    [SerializeField] private TerrainSettings settings = new TerrainSettings();
    [SerializeField] private BaseCamp baseCamp;
    private TerrainChunkManager chunkManager;

#if UNITY_EDITOR
    [Header("씬 뷰 촬영")]
    [Tooltip("씬 뷰에서 돌을 숨기고 전체 청크를 밝게 표시합니다. 플레이 일시정지 중에도 전환할 수 있습니다.")]
    [SerializeField] private bool photoMode;
    private static readonly int PhotoModeId = Shader.PropertyToID("_DeepForgeScenePhotoMode");
    private readonly List<StoneActor> photoStones = new List<StoneActor>();
    private readonly List<Renderer> photoStoneRenderers = new List<Renderer>();
    private readonly List<MeshFilter> photoTerrainMeshes = new List<MeshFilter>();
    private readonly HashSet<GameObject> photoHiddenObjects = new HashSet<GameObject>();
    private readonly List<GameObject> photoVisibilityChanges = new List<GameObject>();
    private readonly Dictionary<SceneView, (bool Lighting, bool Effects)> photoSceneViews =
        new Dictionary<SceneView, (bool Lighting, bool Effects)>();

    private void OnEnable()
    {
        SceneView.beforeSceneGui += ApplyPhotoMode;
        RenderPipelineManager.beginCameraRendering += BeginPhotoModeCamera;
        RenderPipelineManager.endCameraRendering += EndPhotoModeCamera;
    }

    private void OnDisable()
    {
        SceneView.beforeSceneGui -= ApplyPhotoMode;
        RenderPipelineManager.beginCameraRendering -= BeginPhotoModeCamera;
        RenderPipelineManager.endCameraRendering -= EndPhotoModeCamera;
        RestorePhotoMode();
        Shader.SetGlobalFloat(PhotoModeId, 0f);
    }

    // Update 대신 씬 뷰 repaint에서 처리하여 플레이 일시정지 중에도 적용한다.
    private void ApplyPhotoMode(SceneView sceneView)
    {
        if (Event.current.type != EventType.Repaint) return;
        if (!photoMode)
        {
            if (photoSceneViews.Count > 0 || photoHiddenObjects.Count > 0) RestorePhotoMode();
            return;
        }

        if (!photoSceneViews.ContainsKey(sceneView))
            photoSceneViews.Add(sceneView, (sceneView.sceneLighting, sceneView.sceneViewState.fxEnabled));
        sceneView.sceneLighting = false;
        sceneView.sceneViewState.fxEnabled = false;

        // 스트리밍 밖 청크도 씬 카메라에만 그린다. 돌과 충돌체의 활성 상태는 유지한다.
        GetComponentsInChildren(true, photoTerrainMeshes);
        foreach (MeshFilter filter in photoTerrainMeshes)
        {
            if (filter.transform.parent != transform || filter.gameObject.activeInHierarchy ||
                SceneVisibilityManager.instance.IsHidden(filter.gameObject)) continue;
            Mesh mesh = filter.sharedMesh;
            if (mesh == null) continue; // 균일한 청크에는 표면 메시가 없다.
            Graphics.DrawMesh(mesh, filter.transform.localToWorldMatrix, settings.Material.Material,
                gameObject.layer, sceneView.camera, 0, null, ShadowCastingMode.Off, false, null, LightProbeUsage.Off);
        }

        // 활성 상태와 Renderer.enabled는 바꾸지 않는다. 새로 활성화된 청크의 돌도 포함한다.
        GetComponentsInChildren(true, photoStones);
        SceneVisibilityManager visibility = SceneVisibilityManager.instance;
        photoVisibilityChanges.Clear();
        foreach (StoneActor stone in photoStones)
        {
            stone.GetComponentsInChildren(true, photoStoneRenderers);
            foreach (Renderer renderer in photoStoneRenderers)
            {
                GameObject model = renderer.gameObject;
                if (visibility.IsHidden(model)) continue;
                if (photoHiddenObjects.Add(model)) photoVisibilityChanges.Add(model);
            }
        }
        if (photoVisibilityChanges.Count > 0) visibility.Hide(photoVisibilityChanges.ToArray(), false);
    }

    private void RestorePhotoMode()
    {
        SceneVisibilityManager visibility = SceneVisibilityManager.instance;
        photoVisibilityChanges.Clear();
        foreach (GameObject model in photoHiddenObjects)
        {
            // 촬영 중 반환·파괴된 돌은 복원할 대상이 없다.
            if (model != null) photoVisibilityChanges.Add(model);
        }
        if (photoVisibilityChanges.Count > 0) visibility.Show(photoVisibilityChanges.ToArray(), false);
        photoVisibilityChanges.Clear();
        photoHiddenObjects.Clear();
        photoStones.Clear();
        photoStoneRenderers.Clear();
        photoTerrainMeshes.Clear();
        foreach (var saved in photoSceneViews)
        {
            if (saved.Key == null) continue;
            saved.Key.sceneLighting = saved.Value.Lighting;
            saved.Key.sceneViewState.fxEnabled = saved.Value.Effects;
            saved.Key.Repaint();
        }
        photoSceneViews.Clear();
    }

    private void BeginPhotoModeCamera(ScriptableRenderContext context, Camera camera)
    {
        if (!photoMode) return;
        CommandBuffer commands = CommandBufferPool.Get("Terrain Scene Photo Mode");
        commands.SetGlobalFloat(PhotoModeId, camera.cameraType == CameraType.SceneView ? 1f : 0f);
        context.ExecuteCommandBuffer(commands);
        CommandBufferPool.Release(commands);
    }

    private void EndPhotoModeCamera(ScriptableRenderContext context, Camera camera)
    {
        if (!photoMode) return;
        CommandBuffer commands = CommandBufferPool.Get("Terrain Scene Photo Mode End");
        commands.SetGlobalFloat(PhotoModeId, 0f);
        context.ExecuteCommandBuffer(commands);
        CommandBufferPool.Release(commands);
    }
#endif

    // 실행 중인 지형 데이터와 생성 작업
    private TerrainData data;
    private TerrainDensityFormer generator;
    private Coroutine generationRoutine;

    // 밀도 수정과 메시 갱신이 끝난 실제 격자 범위를 전달한다.
    public event System.Action<Vector3Int, Vector3Int> DensityChanged;

    // 외부에서 사용하는 설정과 실행 상태
    public TerrainSettings Settings => settings;
    public TerrainData Data => data;
    public bool IsInitialLoadComplete => data != null && chunkManager.Streamer.IsInitialLoadComplete;

    // 초기 지형을 생성한다.
    protected virtual void Start()
    {
        GenerateTerrain();
    }

    // 대상 위치에 따라 청크 활성화 대기열을 프레임 예산만큼 처리한다.
    private void Update()
    {
        if (chunkManager != null) chunkManager.Streamer.Tick();
    }

    // 물리 갱신에 맞춰 대상 주변 청크와 충돌체를 먼저 활성화한다.
    private void FixedUpdate()
    {
        // Run before player physics so a newly entered neighborhood has colliders.
        // The normal activation budget is processed only by Update.
        if (chunkManager != null) chunkManager.Streamer.UpdateTarget();
    }

    // 월드 좌표의 구 영역에 밀도를 더하고 영향을 받은 청크 메시만 갱신한다.
    public bool AddDensitySphere(
        Vector3 worldPosition, MiningSetting mining, bool isAdding,
        Vector3 worldErosionDirection)
    {
        EnsureInitialized();
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

        // 한 타 안에서는 위치와 방향을 고정하고, 새로 드러난 표면을 다시 계산한다.
        // 중간 메시와 이벤트는 내보내지 않고 전체 변경 범위를 마지막에 한 번 갱신한다.
        int passCount = isAdding ? 1 : mining.HitPassCount;
        Vector3 erosionDirection = worldErosionDirection.normalized;
        Matrix4x4 normalToWorld = transform.worldToLocalMatrix.transpose;
        Vector3Int minChangedIndex = new Vector3Int(data.Width, data.DensityFieldHeight, data.Width);
        Vector3Int maxChangedIndex = new Vector3Int(-1, -1, -1);
        bool changed = false;
        for (int pass = 0; pass < passCount; pass++)
        {
            if (!data.ModifyDensitySphere(
                localPosition,
                mining,
                isAdding,
                settings.MiningBounds,
                settings.Density.DensityThreshold,
                erosionDirection,
                normalToWorld,
                out Vector3Int passMin,
                out Vector3Int passMax))
            {
                break;
            }

            minChangedIndex = Vector3Int.Min(minChangedIndex, passMin);
            maxChangedIndex = Vector3Int.Max(maxChangedIndex, passMax);
            changed = true;
        }
        if (changed)
        {
            GameManager.Instance.Utility.World.MarkDensityChanged(minChangedIndex, maxChangedIndex);
            chunkManager.RegenerateChunksInBounds(minChangedIndex, maxChangedIndex);
            DensityChanged?.Invoke(minChangedIndex, maxChangedIndex);
        }
        return changed;
    }

    // 원기둥 제거와 주변 수축이 모두 끝난 뒤 한 번만 메시와 변경 이벤트를 갱신한다.
    public bool MineCylinder(Vector3 worldPosition, MiningSetting mining, Vector3 worldDirection)
    {
        EnsureInitialized();
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        Vector3 localDirection = transform.InverseTransformVector(worldDirection).normalized;
        if (!data.ModifyDensityCylinder(localPosition, localDirection, mining,
            settings.MiningBounds, settings.Density.DensityThreshold,
            out Vector3Int minChangedIndex, out Vector3Int maxChangedIndex))
            return false;

        GameManager.Instance.Utility.World.MarkDensityChanged(minChangedIndex, maxChangedIndex);
        chunkManager.RegenerateChunksInBounds(minChangedIndex, maxChangedIndex);
        DensityChanged?.Invoke(minChangedIndex, maxChangedIndex);
        return true;
    }

    // 현재 지형을 정리하고 설정값으로 밀도와 청크 메시를 다시 생성한다.
    public void GenerateTerrain()
    {
        chunkManager ??= new TerrainChunkManager(settings.Grid, settings.Streaming);
        // 이전 지형의 돌은 Destroy 처리 시점까지 남을 수 있으므로 구독부터 정리한다.
        DensityChanged = null;
        if (generationRoutine != null)
        {
            StopCoroutine(generationRoutine);
            generationRoutine = null;
        }
        chunkManager.Streamer.Reset();
        if (chunkManager.Registry != null)
        {
            chunkManager.Registry.ClearChunks();
        }
        if (data != null)
        {
            data.Dispose();
            data = null;
        }

        generationRoutine = StartCoroutine(GenerateTerrainRoutine());
    }

    private IEnumerator GenerateTerrainRoutine()
    {
        var loading = GameManager.Instance.Utility.World.LoadOrCreateWorldAsync(settings);
        while (!loading.IsCompleted) yield return null;
        loading.GetAwaiter().GetResult();
        data = new TerrainData(chunkManager.Grid);
        generator = generator ?? new TerrainDensityFormer();
        generator.Generate(data, settings.Surface, settings.Density);
        baseCamp.Place(this);
        data.SetLayerOrigin(transform.InverseTransformPoint(baseCamp.EntrancePoint.position).y);
        GenerateCaves();
        GameManager.Instance.Utility.World.ApplySavedChunks(data);
        baseCamp.SpawnPlayer();
        if (chunkManager.Registry == null)
        {
            chunkManager.Initialize(this);
        }
        chunkManager.Registry.ClearChunks();
        yield return chunkManager.GenerateInitialChunks(settings.Streaming.Target);
        generationRoutine = null;
    }

    // 현재 밀도 데이터를 유지한 채 모든 청크 메시를 다시 만든다.
    public void RegenerateAllChunks()
    {
        EnsureInitialized();
        chunkManager.RegenerateAllChunks();
    }

    // 밀도 데이터와 청크 관리자가 아직 없으면 생성한다.
    private void EnsureInitialized()
    {
        chunkManager ??= new TerrainChunkManager(settings.Grid, settings.Streaming);
        if (data == null)
            throw new System.InvalidOperationException("월드 로드가 끝나기 전에 지형에 접근했습니다.");

        if (chunkManager.Registry == null)
        {
            chunkManager.Initialize(this);
        }
    }

    // 기본 밀도 위에 지하 동굴과 지표 진입로를 모두 반영한 뒤 메시 생성을 시작한다.
    private void GenerateCaves()
    {
        CaveSettings cave = settings.Cave;
        if (!cave.Enabled) return;

        CaveCarveSegment[] segments = new CaveGenerator().GenerateWithEntrance(
            cave, settings.Density.DensityThreshold,
            transform.InverseTransformPoint(baseCamp.EntrancePoint.position),
            transform.InverseTransformDirection(baseCamp.EntrancePoint.forward));

        // 최초 메시 생성 전이므로 변경 bounds를 이용한 별도 메시 갱신은 필요 없다.
        data.CarvePassages(segments, settings.Density.DensityThreshold, cave.TransitionWidth,
            cave.Seed, cave.NoiseScale, cave.NoiseAmplitude,
            cave.DetailNoiseScale, cave.DetailNoiseAmplitude, out _, out _);
    }

    // 오브젝트가 파괴될 때 지형이 소유한 자원을 해제한다.
    private void OnDestroy()
    {
        ReleaseResources();
    }

    // 생성 코루틴과 스트리밍을 중단하고 메시와 밀도 버퍼를 해제한다.
    private void ReleaseResources()
    {
        DensityChanged = null;
        if (generationRoutine != null)
        {
            StopCoroutine(generationRoutine);
            generationRoutine = null;
        }
        if (chunkManager != null)
        {
            chunkManager.Streamer.Reset();
            if (chunkManager.Registry != null) chunkManager.Dispose();
        }

        if (data != null)
        {
            data.Dispose();
            data = null;
        }
    }

}
