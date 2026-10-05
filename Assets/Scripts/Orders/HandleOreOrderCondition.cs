using System;
using UnityEngine;

[Serializable]
public sealed class HandleOreOrderCondition : OrderCondition
{
    [SerializeField] private int oreId;

    public int OreId => oreId;

    public HandleOreOrderCondition(int oreId)
    {
        this.oreId = oreId;
    }

    public override bool IsSatisfied(ForgedGear forgedGear)
    {
        return forgedGear.data.handleOreId == oreId;
    }
}
