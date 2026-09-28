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

    [Header("굴착")]
    [Min(0.01f)] public float ErosionDirectionBlendTime = 0.5f;
    [Range(0f, 1f)] public float ErosionSideStrength = 0.2f;
    [Tooltip("굴착 시 브러시 중심에서 멀어질수록 강도를 줄입니다. 끄면 반경 안의 거리 가중치를 1로 사용합니다.")]
    public bool UseErosionDistanceFalloff = true;
}
