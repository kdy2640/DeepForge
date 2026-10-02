using System;
using UnityEngine;

[Serializable]
public sealed class RuntimeLevel
{
    [SerializeField] private int[] equipmentLevels = new int[(int)EquipmentType.Count];

    public int Get(EquipmentType type)
    {
        return equipmentLevels[(int)type];
    }

    internal void Set(EquipmentType type, int level)
    {
        equipmentLevels[(int)type] = level;
    }

    internal void Clear()
    {
        equipmentLevels = new int[(int)EquipmentType.Count];
    }
}
