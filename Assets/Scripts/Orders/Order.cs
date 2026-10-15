using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class Order
{
    [SerializeField] private int index;
    [SerializeField] private int requiredCount;
    [SerializeField, Min(0f)] private float rewardMultiplier;
    [SerializeReference] private List<OrderCondition> conditions;

    public int Index => index;
    public int RequiredCount => requiredCount;
    public float RewardMultiplier => rewardMultiplier;
    public IReadOnlyList<OrderCondition> Conditions => conditions;

    public Order(int index, int requiredCount, float rewardMultiplier, List<OrderCondition> conditions)
    {
        this.index = index;
        this.requiredCount = requiredCount;
        this.rewardMultiplier = rewardMultiplier;
        this.conditions = conditions;
    }

    public int GetRewardCurrency(IReadOnlyList<ForgedGear> selectedGears)
    {
        long total = 0;
        foreach (ForgedGear gear in selectedGears)
            total += ForgedGearDB.GetData(gear.id).GetSellPrice(gear.data, gear.level);
        return (int)Math.Min(Math.Floor(total * (double)rewardMultiplier), int.MaxValue);
    }

    public bool IsSatisfied(IReadOnlyList<ForgedGear> forgedGears)
    {
        if (forgedGears.Count != requiredCount)
            return false;

        foreach (ForgedGear forgedGear in forgedGears)
        {
            foreach (OrderCondition condition in conditions)
            {
                if (!condition.IsSatisfied(forgedGear))
                    return false;
            }
        }

        return true;
    }
}
