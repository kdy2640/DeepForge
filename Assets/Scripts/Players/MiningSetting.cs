using System;
using UnityEngine;

// PlayerMiner가 소유하는 채굴 설정. 입력과 굴착 진행 상태는 PlayerMiner에서 관리한다.
[Serializable]
public sealed class MiningSetting
{
    [Header("브러시")]
    public float AnchorRadius = 1f;
    public float EditPower = 0.3f;
    [Min(0.01f)] public float EditInterval = 0.1f;

    [Header("채굴 타격")]
    [Min(0.01f)] public float HitInterval = 0.25f;
    [Tooltip("한 타의 총 밀도 감소량. 아래 횟수로 나누어 표면부터 깎습니다.")]
    [Min(0.01f)] public float HitPower = 0.4f;
    [Range(1, 8)] public int HitPassCount = 5;
    [Tooltip("반경 중 가장자리 감쇠 구간의 비율. 1이면 기존 전체 반경 감쇠입니다.")]
    [Range(0.05f, 1f)] public float HitEdgeWidth = 0.35f;

    [Header("굴착")]
    [Min(0.01f)] public float ErosionDirectionBlendTime = 0.5f;
    [Range(0f, 1f)] public float ErosionSideStrength = 0.2f;
    [Tooltip("굴착 시 브러시 중심에서 멀어질수록 강도를 줄입니다. 끄면 반경 안의 거리 가중치를 1로 사용합니다.")]
    public bool UseErosionDistanceFalloff = true;
}
