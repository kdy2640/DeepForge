using System;
using System.Collections.Generic;
using UnityEngine;

// 입구부터의 시작 심도, 표시색과 생성할 돌 목록을 정의한다.
[Serializable]
public struct TerrainLayer
{
    [Min(0f)] public float StartDepth;
    [Tooltip("0은 인공 지형 표시용이며 자연 청크 배정에 사용하지 않습니다.")]
    public byte TypeId;
    public Color Color;
    public List<int> stondataIDs;
}
