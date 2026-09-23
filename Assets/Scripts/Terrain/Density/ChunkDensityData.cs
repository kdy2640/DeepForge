using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

// 청크의 격자 범위와 밀도 배열을 보관한다. 배열의 수명은 TerrainData가 관리한다.
public struct ChunkDensityData
{
    // 전체 격자 기준 시작점과 큐브·샘플 개수
    public Vector3Int Origin;
    public Vector3Int CubeCount;
    public Vector3Int SampleCount;
    // 자연 지층 소속은 청크마다 하나이며 밀도 수정으로 바뀌지 않는다.
    public byte LayerId { get; }
    // 청크 내부 좌표를 일차원으로 펼친 밀도 배열
    public NativeArray<float> Densities;
    // 0은 자연 지형, 1은 빈 공간에 쌓은 인공 지형이다.
    public NativeArray<byte> ArtificialFlags;

    // 청크의 시작 좌표와 크기를 저장하고 샘플 수만큼 밀도 배열을 할당한다.
    public ChunkDensityData(Vector3Int origin, Vector3Int cubeCount, Vector3Int sampleCount, byte layerId)
    {
        Origin = origin;
        CubeCount = cubeCount;
        SampleCount = sampleCount;
        LayerId = layerId;
        Densities = new NativeArray<float>(
            sampleCount.x * sampleCount.y * sampleCount.z, Allocator.Persistent);
        ArtificialFlags = new NativeArray<byte>(Densities.Length, Allocator.Persistent);
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
        int index = (localIndex.x * SampleCount.y + localIndex.y) * SampleCount.z + localIndex.z;
        return Densities[index];
    }

    // 청크 내부 좌표에 해당하는 배열 위치에 밀도를 저장한다.
    public void SetDensity(Vector3Int localIndex, float density)
    {
        int index = (localIndex.x * SampleCount.y + localIndex.y) * SampleCount.z + localIndex.z;
        Densities[index] = density;
    }
}
