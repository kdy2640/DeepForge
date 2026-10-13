using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

// 표면 노출 여부와 관계없이 유한 원기둥 내부를 제거한다.
[BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.High)]
internal struct ModifyDensityCylinderJob : IJob
{
    public NativeArray<byte> Densities;
    public Vector3Int Origin;
    public Vector3Int SampleCount;
    public Vector3Int MinIndex;
    public Vector3Int MaxIndex;
    public Vector3 Start;
    public Vector3 Direction;
    public float Length;
    public float Radius;
    public float Resolution;
    [WriteOnly] public NativeArray<Vector3Int> ChangedBounds;

    public void Execute()
    {
        Vector3Int minChanged = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        Vector3Int maxChanged = new Vector3Int(-1, -1, -1);
        for (int x = MinIndex.x; x <= MaxIndex.x; x++)
        for (int y = MinIndex.y; y <= MaxIndex.y; y++)
        for (int z = MinIndex.z; z <= MaxIndex.z; z++)
        {
            Vector3Int index = new Vector3Int(x, y, z);
            Vector3 offset = (Vector3)index * Resolution - Start;
            float along = Vector3.Dot(offset, Direction);
            if (along < 0f || along > Length ||
                (offset - Direction * along).sqrMagnitude > Radius * Radius)
                continue;

            Vector3Int local = index - Origin;
            int flatIndex = (local.x * SampleCount.y + local.y) * SampleCount.z + local.z;
            if (Densities[flatIndex] == 0) continue;
            Densities[flatIndex] = 0;
            minChanged = Vector3Int.Min(minChanged, index);
            maxChanged = Vector3Int.Max(maxChanged, index);
        }
        ChangedBounds[0] = minChanged;
        ChangedBounds[1] = maxChanged;
    }
}
