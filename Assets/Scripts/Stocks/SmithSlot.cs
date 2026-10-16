using System;

public enum SmithWorkType { Product, EquipmentUpgrade }

[Serializable]
public sealed class SmithSlot
{
    public SmithWorkType workType;
    public ForgedGear product;
    public int equipmentId;
    public int equipmentLevel;
    public float remainingSeconds;
    public bool isReady;

    public bool IsWorking => remainingSeconds > 0f;
    public bool IsOccupied => IsWorking || isReady;
}
