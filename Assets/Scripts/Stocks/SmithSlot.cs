using System;

[Serializable]
public sealed class SmithSlot
{
    public ForgedGear product;
    public float remainingSeconds;

    public bool IsWorking => remainingSeconds > 0f;
}
