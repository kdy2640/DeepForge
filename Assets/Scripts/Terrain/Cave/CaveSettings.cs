using System;
using UnityEngine;

[Serializable]
public sealed class CaveSettings
{
    [Header("동굴 생성 영역과 구조")]
    public bool Enabled;
    [Tooltip("지형 로컬 좌표. 통로 반지름과 벽면 보정까지 포함할 지하 허용 영역입니다.")]
    public Bounds Bounds = new Bounds(new Vector3(192f, 23f, 192f), new Vector3(368f, 30f, 368f));
    public int Seed = 15;
    public Vector3Int RegionCounts = new Vector3Int(4, 1, 4);
    [Min(2)] public int NodesPerRegion = 6;

    [Header("동굴 통로와 벽면")]
    [Tooltip("지형 로컬 거리 기준 최소·최대 반지름입니다. 최소 반지름은 두 노이즈의 변화량 합보다 커야 합니다.")]
    public Vector2 RadiusRange = new Vector2(1.75f, 2.25f);
    [Min(0f)] public float BendDistance = 4f;
    [Min(0.001f)] public float SegmentLength = 1f;
    [Min(0.001f)] public float TransitionWidth = 0.75f;
    [Min(0f)] public float NoiseScale = 0.12f;
    [Min(0f)] public float NoiseAmplitude = 1f;
    [Min(0f)] public float DetailNoiseScale = 0.5f;
    [Min(0f)] public float DetailNoiseAmplitude = 0.5f;

    [Header("내부 노드 공동")]
    [Tooltip("내부 노드마다 만드는 공동의 반지름 범위입니다. 통로의 최대 반지름 이상으로 설정합니다.")]
    public Vector2 ChamberRadiusRange = new Vector2(3f, 4.5f);

    [Header("동굴 지표 출입구")]
    [Tooltip("캠프 입구에서 정면으로 내려가는 구간의 수평 길이입니다.")]
    [Min(1f)] public float EntranceLength = 12f;
    [Range(1f, 45f)] public float EntranceMaxSlope = 30f;
}
