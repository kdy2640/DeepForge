using System;
using UnityEngine;

[Serializable]
public sealed class GearOrderCondition : OrderCondition
{
    [SerializeField] private int gearId;

    public int GearId => gearId;

    public GearOrderCondition(int gearId)
    {
        this.gearId = gearId;
    }

    public override bool IsSatisfied(ForgedGear forgedGear)
    {
        return forgedGear.id == gearId;
    }
}
