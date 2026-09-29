using System.Collections.Generic;
using UnityEngine;

public static class ForgedGearDB
{
    private static readonly Dictionary<ForgedGearType, ForgedGearSO> gearDataMap = new();

    static ForgedGearDB()
    {
        foreach (ForgedGearSO data in Resources.LoadAll<ForgedGearSO>("SOs/ForgedGearDatas"))
            gearDataMap.Add(data.Type, data);
    }

    public static ForgedGearSO GetData(ForgedGearType type)
    {
        return gearDataMap[type];
    }
}
