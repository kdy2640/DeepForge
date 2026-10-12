using System;
using System.Collections.Generic;
using UnityEngine;

// 생성 규칙·지층·돌 후보 목록을 변경하면 Version을 올려 기존 월드를 보존하고 새로 생성한다.
[Serializable]
public sealed class WorldSaveData
{
    public int Version;
    public string WorldId;
    public int RegionSize;
    public TerrainGridSettings Grid;
    public TerrainSurfaceSettings Surface;
    public TerrainDensitySettings Density;
    public CaveSettings Cave;
    public Bounds MiningBounds;
    public int StoneSeed;
    public int StonesPerChunk;
}

public enum WorldDensityEncoding : byte
{
    Original,
    Blank,
    Fill,
    Raw,
    Rle
}

// Region 파일과 저장 작업 사이에서만 사용하는 독립된 복사본이다.
public sealed class RegionSaveData
{
    public Vector3Int Coordinate;
    public int Revision;
    public List<ChunkSaveData> Chunks = new List<ChunkSaveData>();
}

public sealed class ChunkSaveData
{
    public Vector3Int Coordinate;
    public byte StoneMask;
    public WorldDensityEncoding Encoding;
    public byte[] Densities;
}
