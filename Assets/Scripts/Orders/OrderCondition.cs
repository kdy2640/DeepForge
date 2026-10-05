using System;

[Serializable]
public abstract class OrderCondition
{
    public abstract bool IsSatisfied(ForgedGear forgedGear);
}
