using System;
using UnityEngine;

[Serializable]
public sealed class RuntimeLevel
{
    [SerializeField] private int pickaxeLevel;

    public int Get(EquipmentUpgradeType type)
    {
        return type switch
        {
            EquipmentUpgradeType.Pickaxe => pickaxeLevel,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    internal void Set(EquipmentUpgradeType type, int level)
    {
        switch (type)
        {
            case EquipmentUpgradeType.Pickaxe:
                pickaxeLevel = level;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }
}
