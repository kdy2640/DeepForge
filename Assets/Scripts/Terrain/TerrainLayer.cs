using System;
using System.Collections.Generic;
using UnityEngine;

// 청크 소속, 표시색과 생성할 돌 목록을 정의한다. YStart는 청크 중심을 배정하는 지형 로컬 높이 기준이다.
[Serializable]
public struct TerrainLayer
{
    [Min(0f)] public float YStart;
    [Tooltip("0은 인공 지형 표시용이며 자연 청크 배정에 사용하지 않습니다.")]
    public byte TypeId;
    public Color Color;
    public List<int> stondataIDs;
}
