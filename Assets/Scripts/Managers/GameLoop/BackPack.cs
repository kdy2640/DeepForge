using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class BackPack
{
    [SerializeField] private List<OreAmount> ores = new();
    [SerializeField] private int capacity = 50;

    public IReadOnlyList<OreAmount> Ores => ores;
    public int Capacity => capacity;
    public int Count
    {
        get
        {
            int count = 0;
            foreach (OreAmount ore in ores)
                count += ore.amount;
            return count;
        }
    }

    public void SetCapacity(int value)
    {
        capacity = value;
    }

    // 보상 목록 순서대로 남은 공간만큼 담고, 실제로 담은 총수량을 반환한다.
    // 초과분은 보관하지 않는다.
    public int Add(IReadOnlyList<OreAmount> rewards)
    {
        int remaining = Mathf.Max(0, capacity - Count);
        int added = 0;
        foreach (OreAmount reward in rewards)
        {
            if (remaining == 0)
                break;

            int amount = Mathf.Min(reward.amount, remaining);
            if (amount == 0)
                continue;

            OreAmount existing = ores.Find(ore => ore.oreId == reward.oreId);
            if (existing == null)
                ores.Add(new OreAmount(reward.oreId, amount));
            else
                existing.amount += amount;

            remaining -= amount;
            added += amount;
        }
        return added;
    }

    public int GetOreAmount(int oreId)
    {
        foreach (OreAmount ore in ores)
            if (ore.oreId == oreId)
                return ore.amount;
        return 0;
    }

    public void Clear()
    {
        ores.Clear();
    }

    public List<OreAmount> CreateSaveData()
    {
        List<OreAmount> savedOres = new();
        foreach (OreAmount ore in ores)
            savedOres.Add(new OreAmount(ore.oreId, ore.amount));
        return savedOres;
    }

    public void LoadSaveData(List<OreAmount> savedOres)
    {
        ores.Clear();
        foreach (OreAmount ore in savedOres)
            ores.Add(new OreAmount(ore.oreId, ore.amount));
    }
}
