using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public enum ChunkDensityState : byte
{
    Blank,
    Fill,
    Complicate
}

// 청크의 격자 범위와 밀도 상태를 보관하고 자신의 배열을 생성·초기화·해제한다.
public struct ChunkDensityData
{
    // 전체 격자 기준 시작점과 큐브·샘플 개수
    public Vector3Int Origin;
    public Vector3Int CubeCount;
    public Vector3Int SampleCount;
    // 자연 지층 소속은 청크마다 하나이며 밀도 수정으로 바뀌지 않는다.
    public byte LayerId { get; }
    // 균일 청크는 상수로 읽고, Complicate만 실제 배열을 소유한다.
    public ChunkDensityState State { get; private set; }
    // 청크 내부 좌표를 일차원으로 펼친 밀도 배열. 0~255는 밀도 0~1에 대응한다.
    public NativeArray<byte> Densities;
    // 0은 자연 지형, 1은 빈 공간에 쌓은 인공 지형이다.
    public NativeArray<byte> ArtificialFlags;

    // 청크 정보만 준비한다. 초기 생성이나 첫 쓰기 때 필요한 배열을 할당한다.
    public ChunkDensityData(Vector3Int origin, Vector3Int cubeCount, Vector3Int sampleCount, byte layerId)
    {
        Origin = origin;
        CubeCount = cubeCount;
        SampleCount = sampleCount;
        LayerId = layerId;
        State = ChunkDensityState.Blank;
        Densities = default;
        ArtificialFlags = default;
    }

    // 초기 생성 Job이 모든 샘플을 덮어쓸 배열 또는 균일 상태를 준비한다.
    public void Initialize(ChunkDensityState state)
    {
        Dispose();
        State = state;
        if (state == ChunkDensityState.Complicate)
        {
            int length = SampleCount.x * SampleCount.y * SampleCount.z;
            Densities = new NativeArray<byte>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            ArtificialFlags = new NativeArray<byte>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }
    }

    // 첫 쓰기 전에 균일 상태를 배열로 펼치고, 이후 쓰기는 같은 배열을 사용한다.
    public void EnsureWritable()
    {
        if (State == ChunkDensityState.Complicate) return;

        int length = SampleCount.x * SampleCount.y * SampleCount.z;
        bool filled = State == ChunkDensityState.Fill;
        Densities = new NativeArray<byte>(length, Allocator.Persistent,
            filled ? NativeArrayOptions.UninitializedMemory : NativeArrayOptions.ClearMemory);
        ArtificialFlags = new NativeArray<byte>(length, Allocator.Persistent);
        if (filled)
        {
            for (int i = 0; i < length; i++) Densities[i] = 255;
        }
        State = ChunkDensityState.Complicate;
    }

    // 밀도만 0으로 초기화하고 기존 인공 지형 표시는 유지한다.
    public void ResetDensities()
    {
        if (State != ChunkDensityState.Complicate)
        {
            State = ChunkDensityState.Blank;
            return;
        }
        for (int i = 0; i < Densities.Length; i++) Densities[i] = 0;
    }

    // 자신이 소유한 배열을 해제하고 미할당 상태로 돌린다.
    public void Dispose()
    {
        if (State == ChunkDensityState.Complicate)
        {
            Densities.Dispose();
            ArtificialFlags.Dispose();
        }
        Densities = default;
        ArtificialFlags = default;
        State = ChunkDensityState.Blank;
    }

    // 청크의 LayerId로 돌 목록을 조회하고 고정 시드로 위치와 종류를 결정한다.
    public StoneSpawnData[] CreateStoneSpawns(int seed, int count, float resolution)
    {
        List<int> stoneIDs = TerrainTypeDB.GetData(LayerId).Layer.stondataIDs;
        if (stoneIDs.Count == 0) return System.Array.Empty<StoneSpawnData>();

        uint chunkSeed = math.hash(new int4(seed, Origin.x, Origin.y, Origin.z));
        // Unity.Mathematics.Random의 시드는 0이 될 수 없다.
        var random = new Unity.Mathematics.Random(chunkSeed | 1u);
        StoneSpawnData[] spawns = new StoneSpawnData[count];

        for (int i = 0; i < spawns.Length; i++)
        {
            float3 offset = random.NextFloat3();
            Vector3 position = new Vector3(
                Origin.x + offset.x * CubeCount.x,
                Origin.y + offset.y * CubeCount.y,
                Origin.z + offset.z * CubeCount.z);
            spawns[i] = new StoneSpawnData
            {
                TerrainLocalPosition = position * resolution
            };
        }

        // 종류 선택이 기존 위치 난수 순서에 영향을 주지 않도록 위치 계산 후 선택한다.
        for (int i = 0; i < spawns.Length; i++)
        {
            spawns[i].StoneID = stoneIDs[random.NextInt(stoneIDs.Count)];
        }

        return spawns;
    }

    // 청크 내부 좌표를 일차원 인덱스로 바꿔 밀도를 읽는다.
    public float GetDensity(Vector3Int localIndex)
    {
        if (State == ChunkDensityState.Blank) return 0f;
        if (State == ChunkDensityState.Fill) return 1f;
        int index = (localIndex.x * SampleCount.y + localIndex.y) * SampleCount.z + localIndex.z;
        return Densities[index] * (1f / 255f);
    }

    // 청크 내부 좌표에 해당하는 배열 위치에 밀도를 저장한다.
    public void SetDensity(Vector3Int localIndex, float density)
    {
        int index = (localIndex.x * SampleCount.y + localIndex.y) * SampleCount.z + localIndex.z;
        Densities[index] = (byte)Mathf.RoundToInt(Mathf.Clamp01(density) * 255f);
    }
}
