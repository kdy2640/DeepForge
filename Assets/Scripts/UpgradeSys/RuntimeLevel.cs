using System;
using UnityEngine;

[Serializable]
public sealed class RuntimeLevel
{
    [SerializeField] private int[] equipmentLevels = Array.Empty<int>();
    [SerializeField] private int[] forgedGearLevels = Array.Empty<int>();

    public int GetEquipment(int id)
    {
        return equipmentLevels[id];
    }

    internal void SetEquipment(int id, int level)
    {
        equipmentLevels[id] = level;
    }

    public int GetForgedGear(int id)
    {
        return forgedGearLevels[id];
    }

    internal void SetForgedGear(int id, int level)
    {
        forgedGearLevels[id] = level;
    }

    internal void Clear()
    {
        equipmentLevels = new int[EquipmentDataDB.MaxId + 1];
        forgedGearLevels = new int[ForgedGearDB.MaxId + 1];
    }
}
