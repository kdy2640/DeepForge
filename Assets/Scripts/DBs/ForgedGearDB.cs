using System.Collections.Generic;
using UnityEngine;

public static class ForgedGearDB
{
    private static readonly Dictionary<int, ForgedGearSO> gearDataMap = new();

    public static int MaxId { get; } = -1;

    static ForgedGearDB()
    {
        foreach (ForgedGearSO data in Resources.LoadAll<ForgedGearSO>("SOs/ForgedGearDatas"))
        {
            gearDataMap.Add(data.Id, data);
            MaxId = Mathf.Max(MaxId, data.Id);
        }
    }

    public static ForgedGearSO GetData(int id)
    {
        return gearDataMap[id];
    }
}
