using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class Order
{
    [SerializeField] private int index;
    [SerializeField] private int requiredCount;
    [SerializeField] private int rewardCurrency;
    [SerializeReference] private List<OrderCondition> conditions;

    public int Index => index;
    public int RequiredCount => requiredCount;
    public int RewardCurrency => rewardCurrency;
    public IReadOnlyList<OrderCondition> Conditions => conditions;

    public Order(int index, int requiredCount, int rewardCurrency, List<OrderCondition> conditions)
    {
        this.index = index;
        this.requiredCount = requiredCount;
        this.rewardCurrency = rewardCurrency;
        this.conditions = conditions;
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
