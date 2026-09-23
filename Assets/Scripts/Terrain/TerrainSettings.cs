using System;
using UnityEngine;

// 인스펙터에서 편집하는 지형 설정. 실행 중인 데이터와 작업 상태는 각 관리자가 소유한다.
[Serializable]
public sealed class TerrainSettings
{
    [Header("표면 높이와 변화 폭")]
    public TerrainSurfaceSettings Surface = new TerrainSurfaceSettings();
    [Header("노이즈와 표면 판정")]
    public TerrainDensitySettings Density = new TerrainDensitySettings();
    [Header("동굴")]
    public CaveSettings Cave = new CaveSettings();
    [Header("지형 재질")]
    public TerrainMaterialSettings Material = new TerrainMaterialSettings();
    [Header("메시 셰이딩")]
    public TerrainShadingSettings Shading = new TerrainShadingSettings();
    [Header("자원 배치")]
    public StonePlacementSettings Stones = new StonePlacementSettings();
    [Header("지형 격자")]
    public TerrainGridSettings Grid = new TerrainGridSettings();
    [Header("청크 스트리밍")]
    public TerrainStreamingSettings Streaming = new TerrainStreamingSettings();
}

[Serializable]
public sealed class TerrainSurfaceSettings
{
    [Min(0f)] public float BaseSurfaceHeight = 5f;
    [Min(0f)] public float Amplitude = 5f;
}

[Serializable]
public sealed class TerrainDensitySettings
{
    public float NoiseScale = 1f;
    public float DensityThreshold = 0.5f;
    public bool Use3DNoise;
}

[Serializable]
public sealed class TerrainMaterialSettings
{
    public Material Material;
}

[Serializable]
public sealed class TerrainShadingSettings
{
    public bool IsSmoothShading;
}

[Serializable]
public sealed class StonePlacementSettings
{
    public int Seed = 12345;
    [Min(0)] public int CountPerChunk = 1;
    public StoneActor Prefab;
    [Min(0)] public int PrewarmCount = 256;
    [Min(1)] public int PrewarmPerFrame = 16;
}

[Serializable]
public sealed class TerrainGridSettings
{
    [Header("밀도 격자 크기")]
    [Min(1)] public int Width = 30;
    [Min(1)] public int DensityFieldHeight = 20;
    [Header("밀도 샘플 간격")]
    [Min(0.001f)] public float Resolution = 1f;
    [Header("청크 크기")]
    [Min(1)] public int ChunkSize = 16;
}

[Serializable]
public sealed class TerrainStreamingSettings
{
    public Transform Target;
    [Header("청크 활성화·해제 거리")]
    [Min(0f)] public float LoadDistance = 110f;
    [Min(0f)] public float UnloadDistance = 120f;
    [Header("프레임당 활성화 제한")]
    [Min(1)] public int MaxChunkActivationsPerFrame = 64;
}
