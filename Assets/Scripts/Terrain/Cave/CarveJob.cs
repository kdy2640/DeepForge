using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

// 지형 로컬 좌표의 Capsule 구간이다. Start와 End가 같으면 Sphere로 처리한다.
public struct CaveCarveSegment
{
    public Vector3 Start;
    public Vector3 End;
    public float Radius;
}

// 한 청크가 소유하는 밀도에 통로 구간들을 순서와 중복에 무관하게 반영한다.
[BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.High)]
internal struct CarveJob : IJob
{
    public NativeArray<float> Densities;
    public Vector3Int Origin;
    public Vector3Int SampleCount;
    public float Resolution;
    [ReadOnly] public NativeArray<CaveCarveSegment> Segments;
    public float DensityThreshold;
    public float TransitionWidth;
    public int NoiseSeed;
    public float NoiseScale;
    public float NoiseAmplitude;
    [WriteOnly] public NativeArray<Vector3Int> ChangedBounds;

    public void Execute()
    {
        Vector3Int minChanged = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        Vector3Int maxChanged = new Vector3Int(-1, -1, -1);
        Vector3Int chunkMax = Origin + SampleCount - Vector3Int.one;
        float outerTransition = (1f - DensityThreshold) * TransitionWidth;

        for (int i = 0; i < Segments.Length; i++)
        {
            CaveCarveSegment segment = Segments[i];
            float extent = segment.Radius + NoiseAmplitude + outerTransition;
            Vector3 boundsMin = (Vector3.Min(segment.Start, segment.End) - Vector3.one * extent) / Resolution;
            Vector3 boundsMax = (Vector3.Max(segment.Start, segment.End) + Vector3.one * extent) / Resolution;
            Vector3Int minIndex = Vector3Int.Max(Origin, new Vector3Int(
                Mathf.FloorToInt(boundsMin.x), Mathf.FloorToInt(boundsMin.y), Mathf.FloorToInt(boundsMin.z)));
            Vector3Int maxIndex = Vector3Int.Min(chunkMax, new Vector3Int(
                Mathf.CeilToInt(boundsMax.x), Mathf.CeilToInt(boundsMax.y), Mathf.CeilToInt(boundsMax.z)));
            Vector3 direction = segment.End - segment.Start;
            float lengthSquared = direction.sqrMagnitude;

            for (int x = minIndex.x; x <= maxIndex.x; x++)
            {
                for (int y = minIndex.y; y <= maxIndex.y; y++)
                {
                    for (int z = minIndex.z; z <= maxIndex.z; z++)
                    {
                        Vector3 position = new Vector3(x, y, z) * Resolution;
                        // 길이가 0인 구간은 시작점을 중심으로 한 Sphere다.
                        float t = lengthSquared == 0f ? 0f : Mathf.Clamp01(
                            Vector3.Dot(position - segment.Start, direction) / lengthSquared);
                        float distance = Vector3.Distance(position, segment.Start + direction * t);
                        if (distance >= extent)
                        {
                            continue;
                        }

                        // 청크나 구간 번호를 섞지 않아 같은 지형 위치에서는 같은 벽면 보정이 나온다.
                        float noise = NoiseAmplitude == 0f ? 0f :
                            PerlinNoise3D.GetRandomValue(position.x * NoiseScale,
                                position.y * NoiseScale, position.z * NoiseScale, NoiseSeed) * 2f - 1f;
                        float effectiveRadius = segment.Radius + noise * NoiseAmplitude;
                        float carveDensity = Mathf.Clamp01(
                            DensityThreshold + (distance - effectiveRadius) / TransitionWidth);
                        int flatIndex = ((x - Origin.x) * SampleCount.y + y - Origin.y)
                            * SampleCount.z + z - Origin.z;
                        float before = Densities[flatIndex];
                        float after = Mathf.Min(before, carveDensity);
                        if (after == before)
                        {
                            continue;
                        }

                        Densities[flatIndex] = after;
                        Vector3Int index = new Vector3Int(x, y, z);
                        minChanged = Vector3Int.Min(minChanged, index);
                        maxChanged = Vector3Int.Max(maxChanged, index);
                    }
                }
            }
        }

        ChangedBounds[0] = minChanged;
        ChangedBounds[1] = maxChanged;
    }
}
