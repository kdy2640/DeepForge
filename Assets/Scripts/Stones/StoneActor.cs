using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(StonePresenter), typeof(Rigidbody))]
public sealed class StoneActor : Poolable
{
    [System.Serializable]
    private sealed class StoneModel
    {
        public int StoneID;
        public GameObject Model;
        [System.NonSerialized] public BoxCollider[] Colliders;
        [System.NonSerialized] public Vector3 LocalPosition;
        [System.NonSerialized] public Vector3 LocalScale;
    }

    [SerializeField] private StonePresenter presenter;
    [SerializeField] private StoneModel[] models;

    private StoneDataSO dataSO;
    private StoneSpawnData spawnData;
    private StoneModel activeModel;
    private BoxCollider[] modelColliders;
    private TerrainManager terrainManager;
    // 원래 모델 크기에서 만든 밀도 격자 좌표. 등장·제거 연출에 따라 움직이지 않는다.
    private Vector3[] supportPoints;
    private Vector3Int minSupportIndex;
    private Vector3Int maxSupportIndex;
    private bool isListening;
    private bool isBreaking;

    public StoneDataSO DataSO => dataSO;
    public StonePresenter Presenter => presenter;

    public override void InitializePoolItem()
    {
        foreach (StoneModel model in models)
        {
            model.LocalPosition = model.Model.transform.localPosition;
            model.LocalScale = model.Model.transform.localScale;
            model.Colliders = model.Model.GetComponentsInChildren<BoxCollider>(true);
            foreach (BoxCollider modelCollider in model.Colliders)
            {
                modelCollider.gameObject.layer = gameObject.layer;
                modelCollider.isTrigger = true;
            }
            model.Model.SetActive(false);
        }
    }

    public void Spawn(StoneSpawnData spawn, Transform parent, TerrainManager owner)
    {
        transform.SetParent(parent, false);
        transform.localPosition = spawn.TerrainLocalPosition;
        transform.localRotation = Quaternion.identity;
        SetData(spawn.StoneID);
        spawnData = spawn;
        if (!InitializeTerrainSupport(owner))
        {
            RequestReturn();
            return;
        }
        gameObject.SetActive(true);
        presenter.PlaySpawnTween();
    }

    public void SetData(int stoneID)
    {
        ResetState();
        dataSO = StoneDataDB.GetData(stoneID);
        foreach (StoneModel model in models)
        {
            if (model.StoneID != stoneID) continue;
            activeModel = model;
            break;
        }
        activeModel.Model.transform.localPosition = activeModel.LocalPosition;
        activeModel.Model.transform.localScale = activeModel.LocalScale;
        activeModel.Model.SetActive(true);
        modelColliders = activeModel.Colliders;
        foreach (BoxCollider modelCollider in modelColliders)
        {
            modelCollider.enabled = true;
        }
        isBreaking = false;
        presenter.Initialize(activeModel.Model.transform);
    }

    public override void ResetState()
    {
        if (isListening)
        {
            terrainManager.DensityChanged -= OnDensityChanged;
            isListening = false;
        }
        if (activeModel != null)
        {
            presenter.ResetState();
            foreach (BoxCollider modelCollider in activeModel.Colliders)
                modelCollider.enabled = false;
            activeModel.Model.SetActive(false);
        }
        isBreaking = true;
        activeModel = null;
        modelColliders = null;
        dataSO = null;
        spawnData = null;
        terrainManager = null;
        supportPoints = null;
        minSupportIndex = Vector3Int.zero;
        maxSupportIndex = Vector3Int.zero;
    }

    // SetData와 배치가 끝난 뒤 한 번 호출한다. 현재 자원 모델의 BoxCollider 부피를 사용한다.
    private bool InitializeTerrainSupport(TerrainManager owner)
    {
        terrainManager = owner;
        float resolution = owner.Data.Resolution;
        var points = new List<Vector3>();
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        foreach (BoxCollider modelCollider in modelColliders)
        {
            Matrix4x4 localToTerrain = owner.transform.worldToLocalMatrix * modelCollider.transform.localToWorldMatrix;
            Vector3 size = modelCollider.size;
            Vector3 start = modelCollider.center - size * 0.5f;
            // 중심·각 면을 포함하고 지형 해상도 이하의 간격이 되도록 짝수로 나눈다.
            int countX = 2 * Mathf.Max(1, Mathf.CeilToInt(localToTerrain.MultiplyVector(Vector3.right * size.x).magnitude / (2f * resolution)));
            int countY = 2 * Mathf.Max(1, Mathf.CeilToInt(localToTerrain.MultiplyVector(Vector3.up * size.y).magnitude / (2f * resolution)));
            int countZ = 2 * Mathf.Max(1, Mathf.CeilToInt(localToTerrain.MultiplyVector(Vector3.forward * size.z).magnitude / (2f * resolution)));
            for (int x = 0; x <= countX; x++)
            {
                for (int y = 0; y <= countY; y++)
                {
                    for (int z = 0; z <= countZ; z++)
                    {
                        Vector3 position = start + Vector3.Scale(size, new Vector3(
                            (float)x / countX, (float)y / countY, (float)z / countZ));
                        Vector3 point = localToTerrain.MultiplyPoint3x4(position) / resolution;
                        points.Add(point);
                        min = Vector3.Min(min, point);
                        max = Vector3.Max(max, point);
                    }
                }
            }
        }
        supportPoints = points.ToArray();
        // 보간에 사용하는 이웃 격자까지 포함해야 청크 경계의 변경도 놓치지 않는다.
        minSupportIndex = Vector3Int.FloorToInt(min);
        maxSupportIndex = Vector3Int.FloorToInt(max) + Vector3Int.one;

        if (!HasTerrainSupport()) return false;

        terrainManager.DensityChanged += OnDensityChanged;
        isListening = true;
        return true;
    }

    // 판정점의 밀도를 8개 격자 샘플로 보간한다. 한 곳이라도 고체이면 아직 묻혀 있다.
    private bool HasTerrainSupport()
    {
        TerrainData terrainData = terrainManager.Data;
        foreach (Vector3 point in supportPoints)
        {
            Vector3Int index = Vector3Int.FloorToInt(point);
            Vector3 t = point - (Vector3)index;
            float x00 = Mathf.Lerp(terrainData.GetDensity(index),
                terrainData.GetDensity(index + new Vector3Int(1, 0, 0)), t.x);
            float x10 = Mathf.Lerp(terrainData.GetDensity(index + new Vector3Int(0, 1, 0)),
                terrainData.GetDensity(index + new Vector3Int(1, 1, 0)), t.x);
            float x01 = Mathf.Lerp(terrainData.GetDensity(index + new Vector3Int(0, 0, 1)),
                terrainData.GetDensity(index + new Vector3Int(1, 0, 1)), t.x);
            float x11 = Mathf.Lerp(terrainData.GetDensity(index + new Vector3Int(0, 1, 1)),
                terrainData.GetDensity(index + new Vector3Int(1, 1, 1)), t.x);
            float density = Mathf.Lerp(Mathf.Lerp(x00, x10, t.y), Mathf.Lerp(x01, x11, t.y), t.z);
            if (density > terrainManager.Settings.Density.DensityThreshold) return true;
        }
        return false;
    }

    private void OnDensityChanged(Vector3Int minChangedIndex, Vector3Int maxChangedIndex)
    {
        if (!IsRented || isBreaking ||
            maxChangedIndex.x < minSupportIndex.x || minChangedIndex.x > maxSupportIndex.x ||
            maxChangedIndex.y < minSupportIndex.y || minChangedIndex.y > maxSupportIndex.y ||
            maxChangedIndex.z < minSupportIndex.z || minChangedIndex.z > maxSupportIndex.z)
            return;

        if (!HasTerrainSupport()) Collect();
    }

    private void Collect()
    {
        if (!IsRented || isBreaking)
            return;

        isBreaking = true;
        spawnData.IsCollected = true;
        terrainManager.DensityChanged -= OnDensityChanged;
        isListening = false;
        foreach (BoxCollider modelCollider in modelColliders)
            modelCollider.enabled = false;

        GameManager.Instance.StockManager.AddOre(new List<OreAmount>(dataSO.RewardList));
        if (gameObject.activeInHierarchy)
            presenter.PlayBreakTween(RequestReturn);
        else
            RequestReturn();
    }

    private void OnDestroy()
    {
        if (isListening)
            terrainManager.DensityChanged -= OnDensityChanged;
    }
}
