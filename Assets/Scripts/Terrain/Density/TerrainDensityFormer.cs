using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;

// 고정 시드의 3D 노이즈 또는 2D 높이 노이즈로 지형의 초기 밀도를 채운다.
public class TerrainDensityFormer
{
    // 초기 밀도 생성 전체와 준비·예약·완료 대기 시간을 구분한다.
    private static readonly ProfilerMarker FormMarker = new ProfilerMarker("TerrainDensity.Form");
    private static readonly ProfilerMarker PrepareMarker = new ProfilerMarker("TerrainDensity.Form.Prepare");
    private static readonly ProfilerMarker ScheduleMarker = new ProfilerMarker("TerrainDensity.Form.Schedule");
    private static readonly ProfilerMarker CompleteMarker = new ProfilerMarker("TerrainDensity.Form.Complete");

    // 동일한 입력에서 지형을 재현하기 위한 고정 시드
    private const int NoiseSeed = 15;

    // 청크별 밀도 채우기를 예약하고 모든 Job이 완료된 뒤 반환한다.
    public void Generate(TerrainData data, TerrainSurfaceSettings surface, TerrainDensitySettings density)
    {
        using var formScope = FormMarker.Auto();
        int surfaceWidth = data.Width + 1;
        NativeArray<float> surfaceHeights;
        Vector3Int counts = data.ChunkCounts;
        NativeArray<JobHandle> handles;
        using (PrepareMarker.Auto())
        {
            surfaceHeights = new NativeArray<float>(
                density.Use3DNoise ? 0 : surfaceWidth * surfaceWidth,
                Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            handles = new NativeArray<JobHandle>(
                counts.x * counts.y * counts.z, Allocator.Temp);
            if (!density.Use3DNoise)
            {
                for (int x = 0; x < surfaceWidth; x++)
                {
                    for (int z = 0; z < surfaceWidth; z++)
                    {
                        float heightNoise = surface.Amplitude == 0f
                            ? 0f
                            : Mathf.PerlinNoise(x * density.NoiseScale, z * density.NoiseScale) * 2f - 1f;
                        surfaceHeights[x * surfaceWidth + z] = surface.BaseSurfaceHeight + heightNoise * surface.Amplitude;
                    }
                }
            }
        }

        using (ScheduleMarker.Auto())
        {
            int jobIndex = 0;
            for (int x = 0; x < counts.x; x++)
            {
                for (int y = 0; y < counts.y; y++)
                {
                    for (int z = 0; z < counts.z; z++)
                    {
                        Vector3Int coordinate = new Vector3Int(x, y, z);
                        ChunkDensityState state = DetermineInitialState(
                            data.GetChunkData(coordinate), surfaceHeights, surfaceWidth, surface, density);
                        ChunkDensityData chunk = data.InitializeChunk(coordinate, state);
                        if (state != ChunkDensityState.Complicate) continue;
                        FormTerrainDensityJob job = new FormTerrainDensityJob
                        {
                            Densities = chunk.Densities,
                            ArtificialFlags = chunk.ArtificialFlags,
                            Origin = chunk.Origin,
                            SampleCount = chunk.SampleCount,
                            SurfaceHeights = surfaceHeights,
                            SurfaceWidth = surfaceWidth,
                            NoiseSeed = NoiseSeed,
                            NoiseScale = density.NoiseScale,
                            DensityThreshold = density.DensityThreshold,
                            Use3DNoise = density.Use3DNoise
                        };
                        handles[jobIndex++] = job.Schedule(chunk.Densities.Length, 64);
                    }
                }
            }
        }

        using (CompleteMarker.Auto())
        {
            JobHandle.CompleteAll(handles);
        }
        handles.Dispose();
        surfaceHeights.Dispose();
    }

    // 높이 기반 밀도의 최솟값과 최댓값으로 배열이 필요한 청크만 고른다.
    private ChunkDensityState DetermineInitialState(
        ChunkDensityData chunk, NativeArray<float> surfaceHeights, int surfaceWidth,
        TerrainSurfaceSettings surface, TerrainDensitySettings density)
    {
        if (density.Use3DNoise) return ChunkDensityState.Complicate;

        float minHeight = surface.BaseSurfaceHeight;
        float maxHeight = surface.BaseSurfaceHeight;
        if (surface.Amplitude != 0f)
        {
            minHeight = float.PositiveInfinity;
            maxHeight = float.NegativeInfinity;
            for (int x = chunk.Origin.x; x < chunk.Origin.x + chunk.SampleCount.x; x++)
            {
                for (int z = chunk.Origin.z; z < chunk.Origin.z + chunk.SampleCount.z; z++)
                {
                    float height = surfaceHeights[x * surfaceWidth + z];
                    minHeight = Mathf.Min(minHeight, height);
                    maxHeight = Mathf.Max(maxHeight, height);
                }
            }
        }
        // FormTerrainDensityJob과 같은 연산 순서로 실제 0/1 여부를 판정한다.
        float minDensity = Mathf.Clamp01(
            (minHeight - (chunk.Origin.y + chunk.SampleCount.y - 1)) * 0.1f + density.DensityThreshold);
        float maxDensity = Mathf.Clamp01(
            (maxHeight - chunk.Origin.y) * 0.1f + density.DensityThreshold);
        if (minDensity == 1f) return ChunkDensityState.Fill;
        if (maxDensity == 0f) return ChunkDensityState.Blank;
        return ChunkDensityState.Complicate;
    }
}
