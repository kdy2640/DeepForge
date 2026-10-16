using System;
using UnityEngine;

[Serializable]
public sealed class MetalOreOrderCondition : OrderCondition
{
    [SerializeField] private int oreId;

    public int OreId => oreId;

    public MetalOreOrderCondition(int oreId)
    {
        this.oreId = oreId;
    }

    public override bool IsSatisfied(ForgedGear forgedGear)
    {
        return forgedGear.data.metalOreId == oreId;
    }
}
