using System;
using Unity.Collections;
using UnityEngine;

// 메시 생성 Job이 읽을 청크 정보와 이웃 밀도 배열의 참조를 보관한다.
// TerrainData 접근은 메인 스레드의 생성자에서만 수행하며 배열을 직접 해제하지 않는다.
internal struct ChunkMeshInput
{
    // 현재 청크 범위와 전체 지형 격자 정보
    public Vector3Int Origin;
    public Vector3Int CubeCount;
    public int Width;
    public int Height;
    public float Resolution;

    // 밀도 샘플을 소유한 청크를 찾기 위한 분할 정보
    private Vector3Int chunkCoord;
    private Vector3Int chunkCounts;
    private int chunkSize;
    // Neighbor states travel with the Job value without another native allocation.
    private FixedList128Bytes<ChunkDensityState> densityStates;

    // 인공 지형 표시는 표면을 만드는 큐브의 여덟 꼭짓점 소유 청크만 참조한다.
    [ReadOnly] private NativeArray<byte> artificial000;
    [ReadOnly] private NativeArray<byte> artificial001;
    [ReadOnly] private NativeArray<byte> artificial010;
    [ReadOnly] private NativeArray<byte> artificial011;
    [ReadOnly] private NativeArray<byte> artificial100;
    [ReadOnly] private NativeArray<byte> artificial101;
    [ReadOnly] private NativeArray<byte> artificial110;
    [ReadOnly] private NativeArray<byte> artificial111;

    // Eight corner-owner combinations and single-axis gradient neighbors.
    // A one-cell chunk can read two chunks ahead for the gradient at its upper corner.
    [ReadOnly] private NativeArray<float> density000;
    [ReadOnly] private NativeArray<float> density001;
    [ReadOnly] private NativeArray<float> density010;
    [ReadOnly] private NativeArray<float> density011;
    [ReadOnly] private NativeArray<float> density100;
    [ReadOnly] private NativeArray<float> density101;
    [ReadOnly] private NativeArray<float> density110;
    [ReadOnly] private NativeArray<float> density111;
    [ReadOnly] private NativeArray<float> densityN00;
    [ReadOnly] private NativeArray<float> densityN01;
    [ReadOnly] private NativeArray<float> densityN10;
    [ReadOnly] private NativeArray<float> densityN11;
    [ReadOnly] private NativeArray<float> density200;
    [ReadOnly] private NativeArray<float> density201;
    [ReadOnly] private NativeArray<float> density210;
    [ReadOnly] private NativeArray<float> density211;
    [ReadOnly] private NativeArray<float> density0N0;
    [ReadOnly] private NativeArray<float> density1N0;
    [ReadOnly] private NativeArray<float> density0N1;
    [ReadOnly] private NativeArray<float> density1N1;
    [ReadOnly] private NativeArray<float> density020;
    [ReadOnly] private NativeArray<float> density120;
    [ReadOnly] private NativeArray<float> density021;
    [ReadOnly] private NativeArray<float> density121;
    [ReadOnly] private NativeArray<float> density00N;
    [ReadOnly] private NativeArray<float> density01N;
    [ReadOnly] private NativeArray<float> density10N;
    [ReadOnly] private NativeArray<float> density11N;
    [ReadOnly] private NativeArray<float> density002;
    [ReadOnly] private NativeArray<float> density012;
    [ReadOnly] private NativeArray<float> density102;
    [ReadOnly] private NativeArray<float> density112;

    // 현재 청크와 표면·노멀 계산에 필요한 이웃 청크의 밀도 배열을 연결한다.
    public ChunkMeshInput(TerrainData data, Vector3Int chunkCoord,
        NativeArray<float> constantDensities, NativeArray<byte> naturalFlags)
    {
        ChunkDensityData chunk = data.GetChunkData(chunkCoord);
        Origin = chunk.Origin;
        CubeCount = chunk.CubeCount;
        Width = data.Width;
        Height = data.DensityFieldHeight;
        Resolution = data.Resolution;
        this.chunkCoord = chunkCoord;
        chunkCounts = data.ChunkCounts;
        chunkSize = data.ChunkSize;
        densityStates = default;
        densityStates.Length = 64;

        int xN = Mathf.Max(chunkCoord.x - 1, 0);
        int x0 = chunkCoord.x;
        int x1 = Mathf.Min(chunkCoord.x + 1, chunkCounts.x - 1);
        int x2 = Mathf.Min(chunkCoord.x + (chunkSize == 1 ? 2 : 1), chunkCounts.x - 1);
        int yN = Mathf.Max(chunkCoord.y - 1, 0);
        int y0 = chunkCoord.y;
        int y1 = Mathf.Min(chunkCoord.y + 1, chunkCounts.y - 1);
        int y2 = Mathf.Min(chunkCoord.y + (chunkSize == 1 ? 2 : 1), chunkCounts.y - 1);
        int zN = Mathf.Max(chunkCoord.z - 1, 0);
        int z0 = chunkCoord.z;
        int z1 = Mathf.Min(chunkCoord.z + 1, chunkCounts.z - 1);
        int z2 = Mathf.Min(chunkCoord.z + (chunkSize == 1 ? 2 : 1), chunkCounts.z - 1);
        chunk = data.GetChunkData(new Vector3Int(x0, y0, z0));
        artificial000 = chunk.State == ChunkDensityState.Complicate ? chunk.ArtificialFlags : naturalFlags;
        chunk = data.GetChunkData(new Vector3Int(x0, y0, z1));
        artificial001 = chunk.State == ChunkDensityState.Complicate ? chunk.ArtificialFlags : naturalFlags;
        chunk = data.GetChunkData(new Vector3Int(x0, y1, z0));
        artificial010 = chunk.State == ChunkDensityState.Complicate ? chunk.ArtificialFlags : naturalFlags;
        chunk = data.GetChunkData(new Vector3Int(x0, y1, z1));
        artificial011 = chunk.State == ChunkDensityState.Complicate ? chunk.ArtificialFlags : naturalFlags;
        chunk = data.GetChunkData(new Vector3Int(x1, y0, z0));
        artificial100 = chunk.State == ChunkDensityState.Complicate ? chunk.ArtificialFlags : naturalFlags;
        chunk = data.GetChunkData(new Vector3Int(x1, y0, z1));
        artificial101 = chunk.State == ChunkDensityState.Complicate ? chunk.ArtificialFlags : naturalFlags;
        chunk = data.GetChunkData(new Vector3Int(x1, y1, z0));
        artificial110 = chunk.State == ChunkDensityState.Complicate ? chunk.ArtificialFlags : naturalFlags;
        chunk = data.GetChunkData(new Vector3Int(x1, y1, z1));
        artificial111 = chunk.State == ChunkDensityState.Complicate ? chunk.ArtificialFlags : naturalFlags;
        chunk = data.GetChunkData(new Vector3Int(x0, y0, z0));
        densityStates[21] = chunk.State;
        density000 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x0, y0, z1));
        densityStates[22] = chunk.State;
        density001 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x0, y1, z0));
        densityStates[25] = chunk.State;
        density010 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x0, y1, z1));
        densityStates[26] = chunk.State;
        density011 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x1, y0, z0));
        densityStates[37] = chunk.State;
        density100 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x1, y0, z1));
        densityStates[38] = chunk.State;
        density101 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x1, y1, z0));
        densityStates[41] = chunk.State;
        density110 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x1, y1, z1));
        densityStates[42] = chunk.State;
        density111 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(xN, y0, z0));
        densityStates[5] = chunk.State;
        densityN00 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(xN, y0, z1));
        densityStates[6] = chunk.State;
        densityN01 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(xN, y1, z0));
        densityStates[9] = chunk.State;
        densityN10 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(xN, y1, z1));
        densityStates[10] = chunk.State;
        densityN11 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x2, y0, z0));
        densityStates[53] = chunk.State;
        density200 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x2, y0, z1));
        densityStates[54] = chunk.State;
        density201 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x2, y1, z0));
        densityStates[57] = chunk.State;
        density210 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x2, y1, z1));
        densityStates[58] = chunk.State;
        density211 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x0, yN, z0));
        densityStates[17] = chunk.State;
        density0N0 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x1, yN, z0));
        densityStates[33] = chunk.State;
        density1N0 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x0, yN, z1));
        densityStates[18] = chunk.State;
        density0N1 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x1, yN, z1));
        densityStates[34] = chunk.State;
        density1N1 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x0, y2, z0));
        densityStates[29] = chunk.State;
        density020 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x1, y2, z0));
        densityStates[45] = chunk.State;
        density120 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x0, y2, z1));
        densityStates[30] = chunk.State;
        density021 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x1, y2, z1));
        densityStates[46] = chunk.State;
        density121 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x0, y0, zN));
        densityStates[20] = chunk.State;
        density00N = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x0, y1, zN));
        densityStates[24] = chunk.State;
        density01N = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x1, y0, zN));
        densityStates[36] = chunk.State;
        density10N = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x1, y1, zN));
        densityStates[40] = chunk.State;
        density11N = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x0, y0, z2));
        densityStates[23] = chunk.State;
        density002 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x0, y1, z2));
        densityStates[27] = chunk.State;
        density012 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x1, y0, z2));
        densityStates[39] = chunk.State;
        density102 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
        chunk = data.GetChunkData(new Vector3Int(x1, y1, z2));
        densityStates[43] = chunk.State;
        density112 = chunk.State == ChunkDensityState.Complicate ? chunk.Densities : constantDensities;
    }

    // 표면 꼭짓점의 인공 여부를 밀도와 동일한 소유 청크에서 읽는다.
    public readonly bool IsArtificial(Vector3Int index)
    {
        Vector3Int owner = new Vector3Int(
            Mathf.Min(index.x / chunkSize, chunkCounts.x - 1),
            Mathf.Min(index.y / chunkSize, chunkCounts.y - 1),
            Mathf.Min(index.z / chunkSize, chunkCounts.z - 1));
        Vector3Int origin = owner * chunkSize;
        Vector3Int localIndex = index - origin;
        int sampleCountY = Mathf.Min(chunkSize, Height - origin.y) +
            (owner.y == chunkCounts.y - 1 ? 1 : 0);
        int sampleCountZ = Mathf.Min(chunkSize, Width - origin.z) +
            (owner.z == chunkCounts.z - 1 ? 1 : 0);
        int flatIndex = (localIndex.x * sampleCountY + localIndex.y) * sampleCountZ + localIndex.z;
        Vector3Int offset = owner - chunkCoord;
        int sourceIndex = offset.x * 4 + offset.y * 2 + offset.z;
        int stateIndex = (offset.x + 1) * 16 + (offset.y + 1) * 4 + offset.z + 1;
        if (densityStates[stateIndex] != ChunkDensityState.Complicate) flatIndex = 0;
        switch (sourceIndex)
        {
            case 0: return artificial000[flatIndex] != 0;
            case 1: return artificial001[flatIndex] != 0;
            case 2: return artificial010[flatIndex] != 0;
            case 3: return artificial011[flatIndex] != 0;
            case 4: return artificial100[flatIndex] != 0;
            case 5: return artificial101[flatIndex] != 0;
            case 6: return artificial110[flatIndex] != 0;
            case 7: return artificial111[flatIndex] != 0;
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    // 전체 격자 좌표를 소유한 청크의 배열을 골라 해당 샘플의 밀도를 읽는다.
    public readonly float GetDensity(Vector3Int index)
    {
        Vector3Int owner = new Vector3Int(
            Mathf.Min(index.x / chunkSize, chunkCounts.x - 1),
            Mathf.Min(index.y / chunkSize, chunkCounts.y - 1),
            Mathf.Min(index.z / chunkSize, chunkCounts.z - 1));
        Vector3Int origin = owner * chunkSize;
        Vector3Int localIndex = index - origin;
        int sampleCountY = Mathf.Min(chunkSize, Height - origin.y) +
            (owner.y == chunkCounts.y - 1 ? 1 : 0);
        int sampleCountZ = Mathf.Min(chunkSize, Width - origin.z) +
            (owner.z == chunkCounts.z - 1 ? 1 : 0);
        int flatIndex = (localIndex.x * sampleCountY + localIndex.y) * sampleCountZ + localIndex.z;
        Vector3Int offset = owner - chunkCoord;
        int sourceIndex = (offset.x + 1) * 16 + (offset.y + 1) * 4 + offset.z + 1;
        ChunkDensityState state = densityStates[sourceIndex];
        if (state != ChunkDensityState.Complicate) flatIndex = (int)state;

        switch (sourceIndex)
        {
            case 21: return density000[flatIndex];
            case 22: return density001[flatIndex];
            case 25: return density010[flatIndex];
            case 26: return density011[flatIndex];
            case 37: return density100[flatIndex];
            case 38: return density101[flatIndex];
            case 41: return density110[flatIndex];
            case 42: return density111[flatIndex];
            case 5: return densityN00[flatIndex];
            case 6: return densityN01[flatIndex];
            case 9: return densityN10[flatIndex];
            case 10: return densityN11[flatIndex];
            case 53: return density200[flatIndex];
            case 54: return density201[flatIndex];
            case 57: return density210[flatIndex];
            case 58: return density211[flatIndex];
            case 17: return density0N0[flatIndex];
            case 33: return density1N0[flatIndex];
            case 18: return density0N1[flatIndex];
            case 34: return density1N1[flatIndex];
            case 29: return density020[flatIndex];
            case 45: return density120[flatIndex];
            case 30: return density021[flatIndex];
            case 46: return density121[flatIndex];
            case 20: return density00N[flatIndex];
            case 24: return density01N[flatIndex];
            case 36: return density10N[flatIndex];
            case 40: return density11N[flatIndex];
            case 23: return density002[flatIndex];
            case 27: return density012[flatIndex];
            case 39: return density102[flatIndex];
            case 43: return density112[flatIndex];
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
