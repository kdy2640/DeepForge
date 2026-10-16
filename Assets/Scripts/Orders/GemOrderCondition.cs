using System;
using UnityEngine;

[Serializable]
public sealed class GemOrderCondition : OrderCondition
{
    [SerializeField] private bool hasGem;
    [SerializeField] private int oreId;

    public bool HasGem => hasGem;
    public int OreId => oreId;

    public GemOrderCondition(bool hasGem, int oreId)
    {
        this.hasGem = hasGem;
        this.oreId = oreId;
    }

    public override bool IsSatisfied(ForgedGear forgedGear)
    {
        return forgedGear.data.hasGem == hasGem
            && (!hasGem || forgedGear.data.gemOreId == oreId);
    }
}
