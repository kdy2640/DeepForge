using System;
using System.Collections.Generic;
using UnityEngine;

public enum EquipmentStatType
{
    MiningRadius,
    MiningPower,
    BackpackCapacity,
    MoveSpeed,
    LightRange,
    ReturnCableLength,
    Count
}

public enum ModifierType
{
    Add,
    Multiply
}

[Serializable]
public struct EquipmentStatModifier
{
    public EquipmentStatType statType;
    public ModifierType modifierType;
    public float value;
}

[Serializable]
public sealed class EquipmentStat
{
    [SerializeField] private float[] values = new float[(int)EquipmentStatType.Count];

    internal void Initialize()
    {
        values[(int)EquipmentStatType.MiningRadius] = 1f;
        values[(int)EquipmentStatType.MiningPower] = 0.4f;
        values[(int)EquipmentStatType.BackpackCapacity] = 50f;
        values[(int)EquipmentStatType.MoveSpeed] = 8f;
        values[(int)EquipmentStatType.LightRange] = 18f;
        values[(int)EquipmentStatType.ReturnCableLength] = 50f;
    }

    public float Get(EquipmentStatType statType)
    {
        return values[(int)statType];
    }

    internal void Apply(IReadOnlyList<EquipmentStatModifier> modifiers, int level)
    {
        foreach (EquipmentStatModifier modifier in modifiers)
        {
            int index = (int)modifier.statType;
            float amount = modifier.value * level;
            switch (modifier.modifierType)
            {
                case ModifierType.Add:
                    values[index] += amount;
                    break;
                case ModifierType.Multiply:
                    values[index] *= 1f + amount;
                    break;
            }
        }
    }
}
