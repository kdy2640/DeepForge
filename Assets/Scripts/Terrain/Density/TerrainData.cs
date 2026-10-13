using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;

// 청크를 좌표별로 관리하고 격자 좌표 변환과 밀도 수정 Job을 진행한다.
public class TerrainData : IDisposable
{
    public const byte ArtificialTypeId = 0;

    // 밀도 수정 단계별 성능 측정
    private static readonly ProfilerMarker ModifyMarker = new ProfilerMarker("TerrainDensity.Modify");
    private static readonly ProfilerMarker ScheduleMarker = new ProfilerMarker("TerrainDensity.Schedule");
    private static readonly ProfilerMarker CompleteMarker = new ProfilerMarker("TerrainDensity.Complete");
    // 상태가 바뀐 청크는 이 목록에 다시 반영한다.
    private readonly Dictionary<Vector3Int, ChunkDensityData> chunks =
        new Dictionary<Vector3Int, ChunkDensityData>();

    // 전체 격자 크기와 청크 분할 정보
    public int Width { get; }
    public int DensityFieldHeight { get; }
    public float Resolution { get; }
    public int ChunkSize { get; }
    public Vector3Int ChunkCounts { get; }
    public TerrainLayer[] Layers { get; }
    public float LayerOriginY { get; private set; }

    // 지형 크기에 맞춰 청크 정보만 준비한다. 초기 밀도 생성 시 상태를 결정한다.
    public TerrainData(TerrainGridGeometry grid)
    {
        Layers = TerrainTypeDB.GetLayers();

        Width = grid.Width;
        DensityFieldHeight = grid.DensityFieldHeight;
        Resolution = grid.Resolution;
        ChunkSize = grid.ChunkSize;
        ChunkCounts = new Vector3Int(
            Mathf.CeilToInt((float)Width / ChunkSize),
            Mathf.CeilToInt((float)DensityFieldHeight / ChunkSize),
            Mathf.CeilToInt((float)Width / ChunkSize));

        for (int x = 0; x < ChunkCounts.x; x++)
        {
            for (int y = 0; y < ChunkCounts.y; y++)
            {
                for (int z = 0; z < ChunkCounts.z; z++)
                {
                    Vector3Int chunkCoord = new Vector3Int(x, y, z);
                    Vector3Int origin = chunkCoord * ChunkSize;
                    Vector3Int cubeCount = new Vector3Int(
                        Mathf.Min(ChunkSize, Width - origin.x),
                        Mathf.Min(ChunkSize, DensityFieldHeight - origin.y),
                        Mathf.Min(ChunkSize, Width - origin.z));

                    // Only the last chunk on each axis owns the terrain's endpoint sample.
                    Vector3Int sampleCount = cubeCount + new Vector3Int(
                        x == ChunkCounts.x - 1 ? 1 : 0,
                        y == ChunkCounts.y - 1 ? 1 : 0,
                        z == ChunkCounts.z - 1 ? 1 : 0);
                    chunks.Add(chunkCoord, new ChunkDensityData(origin, cubeCount, sampleCount));
                }
            }
        }

        grid.Initialize(this);
    }

    // 기지 배치가 끝난 뒤, 입구의 지형 로컬 높이를 심도 0으로 고정한다.
    public void SetLayerOrigin(float localEntranceY)
    {
        LayerOriginY = localEntranceY;
    }

    public TerrainLayer GetLayerAtHeight(float localY)
    {
        float depth = LayerOriginY - localY;
        int index = 0;
        for (int i = 1; i < Layers.Length; i++)
        {
            if (depth < Layers[i].StartDepth) break;
            index = i;
        }
        return Layers[index];
    }

    // 모든 청크의 밀도를 0으로 초기화한다.
    public void ResetDensities()
    {
        foreach (Vector3Int coordinate in new List<Vector3Int>(chunks.Keys))
        {
            ChunkDensityData chunk = chunks[coordinate];
            chunk.ResetDensities();
            chunks[coordinate] = chunk;
        }
    }

    // 좌표에 해당하는 청크 데이터를 반환한다.
    // 반환값은 struct 복사본이며 배열은 빌려 쓰는 참조다. 수명 변경은 관리 목록을 통해 진행한다.
    public ChunkDensityData GetChunkData(Vector3Int chunkCoord)
    {
        return chunks[chunkCoord];
    }

    // 생성한 지층 소속은 유지하고 저장된 밀도만 복원한다. 인공 표시는 저장하지 않는다.
    public void ApplySavedChunk(ChunkSaveData saved)
    {
        if (saved.Encoding == WorldDensityEncoding.Original) return;
        ChunkDensityState state = saved.Encoding == WorldDensityEncoding.Blank ? ChunkDensityState.Blank :
            saved.Encoding == WorldDensityEncoding.Fill ? ChunkDensityState.Fill : ChunkDensityState.Complicate;
        ChunkDensityData chunk = InitializeChunk(saved.Coordinate, state);
        if (state == ChunkDensityState.Complicate)
        {
            if (saved.Densities.Length != chunk.Densities.Length) throw new System.IO.InvalidDataException("저장 밀도 샘플 수 불일치");
            chunk.Densities.CopyFrom(saved.Densities);
            for (int i = 0; i < chunk.ArtificialFlags.Length; i++) chunk.ArtificialFlags[i] = 0;
        }
    }

    // 청크를 초기화한 뒤 바뀐 상태와 배열을 관리 목록에 반영한다.
    internal ChunkDensityData InitializeChunk(Vector3Int coordinate, ChunkDensityState state)
    {
        ChunkDensityData chunk = chunks[coordinate];
        chunk.Initialize(state);
        chunks[coordinate] = chunk;
        return chunk;
    }

    // 쓰기 가능한 청크를 준비하고 struct 변경을 관리 목록에 반영한다.
    private ChunkDensityData EnsureChunkWritable(Vector3Int coordinate)
    {
        ChunkDensityData chunk = chunks[coordinate];
        chunk.EnsureWritable();
        chunks[coordinate] = chunk;
        return chunk;
    }

    // 모든 청크에 자원 해제를 요청하고 관리 목록을 비운다.
    public void Dispose()
    {
        foreach (ChunkDensityData chunk in chunks.Values)
        {
            chunk.Dispose();
        }

        chunks.Clear();
    }

    // 전체 격자 좌표에 해당하는 밀도를 읽고 범위 밖이면 0을 반환한다.
    public float GetDensity(Vector3Int index)
    {
        if (!IsValidIndex(index))
        {
            return 0f;
        }

        Vector3Int chunkCoord = new Vector3Int(
            Mathf.Min(index.x / ChunkSize, ChunkCounts.x - 1),
            Mathf.Min(index.y / ChunkSize, ChunkCounts.y - 1),
            Mathf.Min(index.z / ChunkSize, ChunkCounts.z - 1));
        ChunkDensityData chunk = chunks[chunkCoord];
        return chunk.GetDensity(index - chunk.Origin);
    }

    // 유효한 격자 좌표의 밀도를 0~1 범위로 제한해 저장한다.
    public void SetDensity(Vector3Int index, float density)
    {
        if (IsValidIndex(index))
        {
            Vector3Int chunkCoord = new Vector3Int(
                Mathf.Min(index.x / ChunkSize, ChunkCounts.x - 1),
                Mathf.Min(index.y / ChunkSize, ChunkCounts.y - 1),
                Mathf.Min(index.z / ChunkSize, ChunkCounts.z - 1));
            ChunkDensityData chunk = EnsureChunkWritable(chunkCoord);
            chunk.SetDensity(index - chunk.Origin, Mathf.Clamp01(density));
        }
    }

    // 지형 로컬 위치를 가장 가까운 밀도 샘플의 격자 좌표로 변환한다.
    public Vector3Int PositionToIndex(Vector3 localPosition)
    {
        return new Vector3Int(
            Mathf.RoundToInt(localPosition.x / Resolution),
            Mathf.RoundToInt(localPosition.y / Resolution),
            Mathf.RoundToInt(localPosition.z / Resolution));
    }

    // 밀도 샘플의 격자 좌표를 지형 로컬 위치로 변환한다.
    public Vector3 IndexToPosition(Vector3Int index)
    {
        return new Vector3(index.x, index.y, index.z) * Resolution;
    }

    // 구 영역의 밀도 수정을 청크별 Job으로 실행하고 수정 영역의 최소·최대 좌표를 반환한다.
    public bool ModifyDensitySphere(
        Vector3 localPosition,
        MiningSetting mining,
        bool isAdding,
        Bounds miningBounds,
        float densityThreshold,
        Vector3 worldErosionDirection,
        Matrix4x4 densityNormalToWorld,
        out Vector3Int minChangedIndex,
        out Vector3Int maxChangedIndex)
    {
        using var modifyScope = ModifyMarker.Auto();
        float radius = mining.AnchorRadius;
        float power = isAdding ? mining.EditPower : -mining.HitPower / mining.HitPassCount;
        minChangedIndex = new Vector3Int(Width, DensityFieldHeight, Width);
        maxChangedIndex = Vector3Int.zero;

        if (radius <= 0f || Mathf.Approximately(power, 0f))
        {
            return false;
        }

        Vector3Int center = PositionToIndex(localPosition);
        int indexRadius = Mathf.CeilToInt(radius / Resolution);
        // 표면 교차점은 구 안에 있어도 edge의 endpoint는 한 칸 밖에 있을 수 있다.
        if (power < 0f)
        {
            indexRadius++;
        }
        Vector3Int extent = Vector3Int.one * indexRadius;
        Vector3Int minIndex = Vector3Int.Max(center - extent, Vector3Int.zero);
        Vector3Int maxIndex = Vector3Int.Min(center + extent, minChangedIndex);
        // 청크 끝 샘플과 설정된 보호층을 모두 남긴다. 타격 구의 중심이 아니라
        // 실제 수정할 샘플 범위를 제한하므로 큰 채굴 반경도 외곽을 뚫지 못한다.
        Vector3Int minMiningIndex = Vector3Int.Max(Vector3Int.one,
            Vector3Int.CeilToInt(miningBounds.min / Resolution));
        Vector3Int maxMiningIndex = Vector3Int.Min(
            new Vector3Int(Width, DensityFieldHeight, Width) - Vector3Int.one,
            Vector3Int.FloorToInt(miningBounds.max / Resolution));
        if (!isAdding && (maxIndex.x < minMiningIndex.x || maxIndex.y < minMiningIndex.y || maxIndex.z < minMiningIndex.z ||
            minIndex.x > maxMiningIndex.x || minIndex.y > maxMiningIndex.y || minIndex.z > maxMiningIndex.z))
        {
            return false;
        }
        if (minIndex.x > maxIndex.x || minIndex.y > maxIndex.y || minIndex.z > maxIndex.z)
        {
            return false;
        }

        Vector3Int minChunk = new Vector3Int(
            Mathf.Min(minIndex.x / ChunkSize, ChunkCounts.x - 1),
            Mathf.Min(minIndex.y / ChunkSize, ChunkCounts.y - 1),
            Mathf.Min(minIndex.z / ChunkSize, ChunkCounts.z - 1));
        Vector3Int maxChunk = new Vector3Int(
            Mathf.Min(maxIndex.x / ChunkSize, ChunkCounts.x - 1),
            Mathf.Min(maxIndex.y / ChunkSize, ChunkCounts.y - 1),
            Mathf.Min(maxIndex.z / ChunkSize, ChunkCounts.z - 1));
        Vector3Int count = maxChunk - minChunk + Vector3Int.one;
        int jobCount = count.x * count.y * count.z;
        Vector3Int erosionSampleCount = maxIndex - minIndex + Vector3Int.one;
        NativeArray<float> erosionWeights = new NativeArray<float>(
            power < 0f ? erosionSampleCount.x * erosionSampleCount.y * erosionSampleCount.z : 0,
            Allocator.TempJob);

        NativeArray<byte> densitySnapshot = default;
        CalculateErosionWeightsJob weightsJob = default;
        if (power < 0f)
        {
            // endpoint 범위에 노멀 계산용 이웃 한 칸을 더해 시작 밀도를 복사한다.
            Vector3Int snapshotMin = Vector3Int.Max(minIndex - Vector3Int.one, Vector3Int.zero);
            Vector3Int snapshotMax = Vector3Int.Min(maxIndex + Vector3Int.one,
                new Vector3Int(Width, DensityFieldHeight, Width));
            Vector3Int snapshotCount = snapshotMax - snapshotMin + Vector3Int.one;
            densitySnapshot = new NativeArray<byte>(snapshotCount.x * snapshotCount.y * snapshotCount.z,
                Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            Vector3Int snapshotMinChunk = new Vector3Int(
                Mathf.Min(snapshotMin.x / ChunkSize, ChunkCounts.x - 1),
                Mathf.Min(snapshotMin.y / ChunkSize, ChunkCounts.y - 1),
                Mathf.Min(snapshotMin.z / ChunkSize, ChunkCounts.z - 1));
            Vector3Int snapshotMaxChunk = new Vector3Int(
                Mathf.Min(snapshotMax.x / ChunkSize, ChunkCounts.x - 1),
                Mathf.Min(snapshotMax.y / ChunkSize, ChunkCounts.y - 1),
                Mathf.Min(snapshotMax.z / ChunkSize, ChunkCounts.z - 1));

            for (int x = snapshotMinChunk.x; x <= snapshotMaxChunk.x; x++)
            {
                for (int y = snapshotMinChunk.y; y <= snapshotMaxChunk.y; y++)
                {
                    for (int z = snapshotMinChunk.z; z <= snapshotMaxChunk.z; z++)
                    {
                        ChunkDensityData chunk = chunks[new Vector3Int(x, y, z)];
                        Vector3Int copyMin = Vector3Int.Max(snapshotMin, chunk.Origin);
                        Vector3Int copyMax = Vector3Int.Min(snapshotMax,
                            chunk.Origin + chunk.SampleCount - Vector3Int.one);
                        int copyLength = copyMax.z - copyMin.z + 1;
                        // 청크의 연속된 Z 구간을 복사해 샘플마다 Dictionary를 조회하지 않는다.
                        for (int sampleX = copyMin.x; sampleX <= copyMax.x; sampleX++)
                        {
                            for (int sampleY = copyMin.y; sampleY <= copyMax.y; sampleY++)
                            {
                                int sourceIndex = ((sampleX - chunk.Origin.x) * chunk.SampleCount.y +
                                    sampleY - chunk.Origin.y) * chunk.SampleCount.z + copyMin.z - chunk.Origin.z;
                                int targetIndex = ((sampleX - snapshotMin.x) * snapshotCount.y +
                                    sampleY - snapshotMin.y) * snapshotCount.z + copyMin.z - snapshotMin.z;
                                if (chunk.State == ChunkDensityState.Complicate)
                                {
                                    NativeArray<byte>.Copy(chunk.Densities, sourceIndex, densitySnapshot, targetIndex, copyLength);
                                }
                                else
                                {
                                    byte density = chunk.State == ChunkDensityState.Fill ? (byte)255 : (byte)0;
                                    for (int sampleZ = 0; sampleZ < copyLength; sampleZ++)
                                        densitySnapshot[targetIndex + sampleZ] = density;
                                }
                            }
                        }
                    }
                }
            }

            weightsJob = new CalculateErosionWeightsJob
            {
                Densities = densitySnapshot,
                DensityOrigin = snapshotMin,
                DensitySampleCount = snapshotCount,
                MaxTerrainIndex = new Vector3Int(Width, DensityFieldHeight, Width),
                WeightOrigin = minIndex,
                WeightSampleCount = erosionSampleCount,
                LocalPosition = localPosition,
                Resolution = Resolution,
                Radius = radius,
                DensityThreshold = densityThreshold,
                WorldErosionDirection = worldErosionDirection,
                DensityNormalToWorld = densityNormalToWorld,
                ErosionSideStrength = isAdding ? 1f : mining.ErosionSideStrength,
                UseDistanceFalloff = !isAdding && mining.UseErosionDistanceFalloff,
                EdgeWidth = mining.HitEdgeWidth,
                Weights = erosionWeights
            };
        }

        NativeArray<JobHandle> handles = new NativeArray<JobHandle>(jobCount, Allocator.Temp);
        NativeArray<Vector3Int>[] changedBounds = new NativeArray<Vector3Int>[jobCount];

        using (ScheduleMarker.Auto())
        {
            JobHandle weightsHandle = power < 0f ? weightsJob.Schedule(erosionWeights.Length, 64) : default;
            int i = 0;
            for (int x = minChunk.x; x <= maxChunk.x; x++)
            {
                for (int y = minChunk.y; y <= maxChunk.y; y++)
                {
                    for (int z = minChunk.z; z <= maxChunk.z; z++)
                    {
                        ChunkDensityData chunk = EnsureChunkWritable(new Vector3Int(x, y, z));
                        // Each Job owns a separate density array and result buffer.
                        changedBounds[i] = new NativeArray<Vector3Int>(2, Allocator.TempJob);
                        ModifyDensitySphereJob job = new ModifyDensitySphereJob
                        {
                            Densities = chunk.Densities,
                            ArtificialFlags = chunk.ArtificialFlags,
                            DensityThreshold = densityThreshold,
                            Origin = chunk.Origin,
                            SampleCount = chunk.SampleCount,
                            MinIndex = Vector3Int.Max(minIndex, chunk.Origin),
                            MaxIndex = Vector3Int.Min(maxIndex, chunk.Origin + chunk.SampleCount - Vector3Int.one),
                            MinMiningIndex = minMiningIndex,
                            MaxMiningIndex = maxMiningIndex,
                            LocalPosition = localPosition,
                            Resolution = Resolution,
                            Radius = radius,
                            Power = power,
                            ErosionWeights = erosionWeights,
                            ErosionOrigin = minIndex,
                            ErosionSampleCount = erosionSampleCount,
                            ChangedBounds = changedBounds[i]
                        };
                        handles[i] = job.Schedule(weightsHandle);
                        i++;
                    }
                }
            }
        }

        using (CompleteMarker.Auto())
        {
            JobHandle.CompleteAll(handles);
        }
        handles.Dispose();
        erosionWeights.Dispose();
        if (power < 0f)
        {
            densitySnapshot.Dispose();
        }

        bool changed = false;
        for (int i = 0; i < jobCount; i++)
        {
            Vector3Int min = changedBounds[i][0];
            Vector3Int max = changedBounds[i][1];
            if (min.x <= max.x)
            {
                minChangedIndex = Vector3Int.Min(minChangedIndex, min);
                maxChangedIndex = Vector3Int.Max(maxChangedIndex, max);
                changed = true;
            }
            changedBounds[i].Dispose();
        }

        return changed;
    }

    // 기존 구형 수정과 별개로 원기둥 내부를 제거한 뒤 주변 표면만 수축한다.
    public bool ModifyDensityCylinder(
        Vector3 localPosition, Vector3 localDirection, MiningSetting mining,
        Bounds miningBounds, float densityThreshold,
        out Vector3Int minChangedIndex, out Vector3Int maxChangedIndex)
    {
        using var modifyScope = ModifyMarker.Auto();
        // 장비 기본 위력 0.4를 기준으로 깊이에 강화 배율을 적용한다.
        float depth = mining.HitDepthLayers * Resolution * (mining.HitPower / 0.4f);
        float radius = mining.AnchorRadius;
        float correctionWidth = mining.HitCorrectionWidth * Resolution;
        Vector3 start = localPosition - localDirection * Resolution;
        Vector3 end = localPosition + localDirection * depth;
        float length = depth + Resolution;
        Vector3Int terrainMax = new Vector3Int(Width, DensityFieldHeight, Width);
        minChangedIndex = terrainMax;
        maxChangedIndex = new Vector3Int(-1, -1, -1);

        // 표면 교차점의 양 끝 샘플까지 포함한다. 실제 쓰기는 보호층 안으로 제한한다.
        Vector3 extent = Vector3.one * (radius + correctionWidth + Resolution);
        Vector3Int minIndex = Vector3Int.Max(Vector3Int.zero,
            Vector3Int.FloorToInt((Vector3.Min(start, end) - extent) / Resolution));
        Vector3Int maxIndex = Vector3Int.Min(terrainMax,
            Vector3Int.CeilToInt((Vector3.Max(start, end) + extent) / Resolution));
        Vector3Int minMiningIndex = Vector3Int.Max(Vector3Int.one,
            Vector3Int.CeilToInt(miningBounds.min / Resolution));
        Vector3Int maxMiningIndex = Vector3Int.Min(terrainMax - Vector3Int.one,
            Vector3Int.FloorToInt(miningBounds.max / Resolution));
        Vector3Int writableMin = Vector3Int.Max(minIndex, minMiningIndex);
        Vector3Int writableMax = Vector3Int.Min(maxIndex, maxMiningIndex);
        if (writableMin.x > writableMax.x || writableMin.y > writableMax.y || writableMin.z > writableMax.z)
            return false;

        Vector3Int minChunk = Vector3Int.Min(minIndex / ChunkSize, ChunkCounts - Vector3Int.one);
        Vector3Int maxChunk = Vector3Int.Min(maxIndex / ChunkSize, ChunkCounts - Vector3Int.one);
        Vector3Int chunkCount = maxChunk - minChunk + Vector3Int.one;
        int jobCount = chunkCount.x * chunkCount.y * chunkCount.z;
        Vector3Int sampleCount = maxIndex - minIndex + Vector3Int.one;
        int sampleLength = sampleCount.x * sampleCount.y * sampleCount.z;
        NativeArray<byte> snapshot = new NativeArray<byte>(sampleLength, Allocator.TempJob,
            NativeArrayOptions.UninitializedMemory);
        NativeArray<float> weights = new NativeArray<float>(sampleLength, Allocator.TempJob);
        NativeArray<JobHandle> handles = new NativeArray<JobHandle>(jobCount, Allocator.Temp);
        ChunkDensityData[] affectedChunks = new ChunkDensityData[jobCount];
        NativeArray<Vector3Int>[] changedBounds = new NativeArray<Vector3Int>[jobCount];
        int chunkIndex = 0;
        for (int x = minChunk.x; x <= maxChunk.x; x++)
        for (int y = minChunk.y; y <= maxChunk.y; y++)
        for (int z = minChunk.z; z <= maxChunk.z; z++)
        {
            affectedChunks[chunkIndex] = EnsureChunkWritable(new Vector3Int(x, y, z));
            changedBounds[chunkIndex] = new NativeArray<Vector3Int>(2, Allocator.TempJob);
            chunkIndex++;
        }

        bool changed = false;
        // 첫 단계는 통째 제거, 이후 단계는 매번 새 표면을 계산하는 수축 보정이다.
        for (int pass = -1; pass < mining.HitPassCount; pass++)
        {
            JobHandle weightsHandle = default;
            if (pass >= 0)
            {
                foreach (ChunkDensityData chunk in affectedChunks)
                {
                    Vector3Int copyMin = Vector3Int.Max(minIndex, chunk.Origin);
                    Vector3Int copyMax = Vector3Int.Min(maxIndex, chunk.Origin + chunk.SampleCount - Vector3Int.one);
                    int copyLength = copyMax.z - copyMin.z + 1;
                    for (int x = copyMin.x; x <= copyMax.x; x++)
                    for (int y = copyMin.y; y <= copyMax.y; y++)
                    {
                        int sourceIndex = ((x - chunk.Origin.x) * chunk.SampleCount.y + y - chunk.Origin.y)
                            * chunk.SampleCount.z + copyMin.z - chunk.Origin.z;
                        int targetIndex = ((x - minIndex.x) * sampleCount.y + y - minIndex.y)
                            * sampleCount.z + copyMin.z - minIndex.z;
                        NativeArray<byte>.Copy(chunk.Densities, sourceIndex, snapshot, targetIndex, copyLength);
                    }
                }
                weightsHandle = new CalculateCylinderErosionWeightsJob
                {
                    Densities = snapshot,
                    Origin = minIndex,
                    SampleCount = sampleCount,
                    Resolution = Resolution,
                    DensityThreshold = densityThreshold,
                    Start = start,
                    Direction = localDirection,
                    Length = length,
                    Radius = radius,
                    CorrectionWidth = correctionWidth,
                    Weights = weights
                }.Schedule(sampleLength, 64);
            }

            for (int i = 0; i < jobCount; i++)
            {
                ChunkDensityData chunk = affectedChunks[i];
                Vector3Int chunkMin = Vector3Int.Max(writableMin, chunk.Origin);
                Vector3Int chunkMax = Vector3Int.Min(writableMax, chunk.Origin + chunk.SampleCount - Vector3Int.one);
                if (pass < 0)
                {
                    handles[i] = new ModifyDensityCylinderJob
                    {
                        Densities = chunk.Densities,
                        Origin = chunk.Origin,
                        SampleCount = chunk.SampleCount,
                        MinIndex = chunkMin,
                        MaxIndex = chunkMax,
                        Start = start,
                        Direction = localDirection,
                        Length = length,
                        Radius = radius,
                        Resolution = Resolution,
                        ChangedBounds = changedBounds[i]
                    }.Schedule();
                }
                else
                {
                    // 기존 구형 Job의 굴착 분기는 전달된 표면 가중치만 사용한다.
                    handles[i] = new ModifyDensitySphereJob
                    {
                        Densities = chunk.Densities,
                        ArtificialFlags = chunk.ArtificialFlags,
                        DensityThreshold = densityThreshold,
                        Origin = chunk.Origin,
                        SampleCount = chunk.SampleCount,
                        MinIndex = chunkMin,
                        MaxIndex = chunkMax,
                        MinMiningIndex = minMiningIndex,
                        MaxMiningIndex = maxMiningIndex,
                        Power = -mining.HitPower / mining.HitPassCount,
                        ErosionWeights = weights,
                        ErosionOrigin = minIndex,
                        ErosionSampleCount = sampleCount,
                        ChangedBounds = changedBounds[i]
                    }.Schedule(weightsHandle);
                }
            }
            JobHandle.CompleteAll(handles);
            bool passChanged = false;
            for (int i = 0; i < jobCount; i++)
            {
                Vector3Int min = changedBounds[i][0];
                Vector3Int max = changedBounds[i][1];
                if (min.x > max.x) continue;
                minChangedIndex = Vector3Int.Min(minChangedIndex, min);
                maxChangedIndex = Vector3Int.Max(maxChangedIndex, max);
                passChanged = true;
                changed = true;
            }
            if (pass >= 0 && !passChanged) break;
        }

        handles.Dispose();
        weights.Dispose();
        snapshot.Dispose();
        for (int i = 0; i < jobCount; i++) changedBounds[i].Dispose();
        return changed;
    }

    // 통로들을 청크별로 배정하고 국소 Carve가 모두 끝난 뒤 실제 변경 범위를 반환한다.
    // 입력은 지형 로컬 단위다. 0 < threshold < 1, transitionWidth > 0,
    // noiseAmplitude >= 0, 각 Radius > noiseAmplitude를 호출 측에서 보장한다.
    // 청크 소속과 인공 지형 표시는 유지하며 메시 갱신은 호출 측에서 처리한다.
    public bool CarvePassages(
        CaveCarveSegment[] segments,
        float densityThreshold,
        float transitionWidth,
        int noiseSeed,
        float noiseScale,
        float noiseAmplitude,
        out Vector3Int minChangedIndex,
        out Vector3Int maxChangedIndex)
    {
        Vector3Int terrainMax = new Vector3Int(Width, DensityFieldHeight, Width);
        minChangedIndex = terrainMax;
        maxChangedIndex = new Vector3Int(-1, -1, -1);
        var segmentsByChunk = new Dictionary<Vector3Int, List<CaveCarveSegment>>();
        float outerTransition = (1f - densityThreshold) * transitionWidth;

        foreach (CaveCarveSegment segment in segments)
        {
            float extent = segment.Radius + noiseAmplitude + outerTransition;
            Vector3 boundsMin = (Vector3.Min(segment.Start, segment.End) - Vector3.one * extent) / Resolution;
            Vector3 boundsMax = (Vector3.Max(segment.Start, segment.End) + Vector3.one * extent) / Resolution;
            Vector3Int minIndex = Vector3Int.Max(Vector3Int.zero, new Vector3Int(
                Mathf.FloorToInt(boundsMin.x), Mathf.FloorToInt(boundsMin.y), Mathf.FloorToInt(boundsMin.z)));
            Vector3Int maxIndex = Vector3Int.Min(terrainMax, new Vector3Int(
                Mathf.CeilToInt(boundsMax.x), Mathf.CeilToInt(boundsMax.y), Mathf.CeilToInt(boundsMax.z)));
            if (minIndex.x > maxIndex.x || minIndex.y > maxIndex.y || minIndex.z > maxIndex.z)
            {
                continue;
            }

            Vector3Int minChunk = new Vector3Int(
                Mathf.Min(minIndex.x / ChunkSize, ChunkCounts.x - 1),
                Mathf.Min(minIndex.y / ChunkSize, ChunkCounts.y - 1),
                Mathf.Min(minIndex.z / ChunkSize, ChunkCounts.z - 1));
            Vector3Int maxChunk = new Vector3Int(
                Mathf.Min(maxIndex.x / ChunkSize, ChunkCounts.x - 1),
                Mathf.Min(maxIndex.y / ChunkSize, ChunkCounts.y - 1),
                Mathf.Min(maxIndex.z / ChunkSize, ChunkCounts.z - 1));
            for (int x = minChunk.x; x <= maxChunk.x; x++)
            {
                for (int y = minChunk.y; y <= maxChunk.y; y++)
                {
                    for (int z = minChunk.z; z <= maxChunk.z; z++)
                    {
                        Vector3Int coordinate = new Vector3Int(x, y, z);
                        if (!segmentsByChunk.TryGetValue(coordinate, out List<CaveCarveSegment> chunkSegments))
                        {
                            chunkSegments = new List<CaveCarveSegment>();
                            segmentsByChunk.Add(coordinate, chunkSegments);
                        }
                        chunkSegments.Add(segment);
                    }
                }
            }
        }

        int jobCount = segmentsByChunk.Count;
        if (jobCount == 0)
        {
            return false;
        }

        NativeArray<JobHandle> handles = new NativeArray<JobHandle>(jobCount, Allocator.Temp);
        NativeArray<CaveCarveSegment>[] segmentBuffers = new NativeArray<CaveCarveSegment>[jobCount];
        NativeArray<Vector3Int>[] changedBounds = new NativeArray<Vector3Int>[jobCount];
        int jobIndex = 0;
        foreach (var entry in segmentsByChunk)
        {
            ChunkDensityData chunk = EnsureChunkWritable(entry.Key);
            segmentBuffers[jobIndex] = new NativeArray<CaveCarveSegment>(entry.Value.ToArray(), Allocator.TempJob);
            changedBounds[jobIndex] = new NativeArray<Vector3Int>(2, Allocator.TempJob);
            CarveJob job = new CarveJob
            {
                Densities = chunk.Densities,
                Origin = chunk.Origin,
                SampleCount = chunk.SampleCount,
                Resolution = Resolution,
                Segments = segmentBuffers[jobIndex],
                DensityThreshold = densityThreshold,
                TransitionWidth = transitionWidth,
                NoiseSeed = noiseSeed,
                NoiseScale = noiseScale,
                NoiseAmplitude = noiseAmplitude,
                ChangedBounds = changedBounds[jobIndex]
            };
            handles[jobIndex] = job.Schedule();
            jobIndex++;
        }

        JobHandle.CompleteAll(handles);
        handles.Dispose();
        bool changed = false;
        for (int i = 0; i < jobCount; i++)
        {
            Vector3Int min = changedBounds[i][0];
            Vector3Int max = changedBounds[i][1];
            if (min.x <= max.x)
            {
                minChangedIndex = Vector3Int.Min(minChangedIndex, min);
                maxChangedIndex = Vector3Int.Max(maxChangedIndex, max);
                changed = true;
            }
            segmentBuffers[i].Dispose();
            changedBounds[i].Dispose();
        }

        return changed;
    }

    // 좌표가 지형 끝점 샘플을 포함한 밀도 격자 범위 안인지 확인한다.
    public bool IsValidIndex(Vector3Int index)
    {
        return index.x >= 0 && index.x <= Width &&
               index.y >= 0 && index.y <= DensityFieldHeight &&
               index.z >= 0 && index.z <= Width;
    }
}
