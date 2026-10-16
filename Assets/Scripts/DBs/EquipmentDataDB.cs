using System.Collections.Generic;
using UnityEngine;

public static class EquipmentDataDB
{
    private static readonly Dictionary<int, EquipmentDataSO> equipmentDataMap = new();

    public static int MaxId { get; } = -1;

    static EquipmentDataDB()
    {
        foreach (EquipmentDataSO data in Resources.LoadAll<EquipmentDataSO>("SOs/EquipmentDatas"))
        {
            equipmentDataMap.Add(data.Id, data);
            MaxId = Mathf.Max(MaxId, data.Id);
        }
    }

    public static EquipmentDataSO GetData(int id)
    {
        return equipmentDataMap[id];
    }
}
