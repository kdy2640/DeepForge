using System.Collections;
using UnityEngine;

// 지형 밀도 생성, 청크 메시 갱신과 대상 주변 스트리밍을 관리한다.
[DisallowMultipleComponent]
public class TerrainManager : MonoBehaviour
{
    [SerializeField] private TerrainSettings settings = new TerrainSettings();
    private TerrainChunkManager chunkManager;

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

        bool changed = data.ModifyDensitySphere(
            localPosition,
            mining,
            isAdding,
            settings.Density.DensityThreshold,
            worldErosionDirection.normalized,
            transform.worldToLocalMatrix.transpose,
            out Vector3Int minChangedIndex,
            out Vector3Int maxChangedIndex);
        if (changed)
        {
            chunkManager.RegenerateChunksInBounds(minChangedIndex, maxChangedIndex);
            DensityChanged?.Invoke(minChangedIndex, maxChangedIndex);
        }
        return changed;
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
        }

        data = new TerrainData(chunkManager.Grid);
        generator = generator ?? new TerrainDensityFormer();
        generator.Generate(data, settings.Surface, settings.Density);
        GenerateCaves();
        if (chunkManager.Registry == null)
        {
            chunkManager.Initialize(this);
        }
        chunkManager.Registry.ClearChunks();
        generationRoutine = StartCoroutine(GenerateTerrainRoutine());
    }

    // 초기 청크를 여러 프레임에 나눠 생성한 뒤 스트리밍을 시작한다.
    private IEnumerator GenerateTerrainRoutine()
    {
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
        {
            data = new TerrainData(chunkManager.Grid);
            generator = generator ?? new TerrainDensityFormer();
            generator.Generate(data, settings.Surface, settings.Density);
            GenerateCaves();
        }

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
            data, cave, settings.Density.DensityThreshold);

        // 최초 메시 생성 전이므로 변경 bounds를 이용한 별도 메시 갱신은 필요 없다.
        data.CarvePassages(segments, settings.Density.DensityThreshold, cave.TransitionWidth,
            cave.Seed, cave.NoiseScale, cave.NoiseAmplitude, out _, out _);
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
