using System.Collections.Generic;
using UnityEngine;

public static class EquipmentDataDB
{
    private static readonly Dictionary<int, EquipmentDataSO> equipmentDataMap = new();

    static EquipmentDataDB()
    {
        foreach (EquipmentDataSO data in Resources.LoadAll<EquipmentDataSO>("SOs/EquipmentDatas"))
            equipmentDataMap.Add(data.Id, data);
    }

    public static EquipmentDataSO GetData(int id)
    {
        return equipmentDataMap[id];
    }
}
