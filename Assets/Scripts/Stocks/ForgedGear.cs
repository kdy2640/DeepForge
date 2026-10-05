using System;
using UnityEngine.Serialization;

public enum ForgedGearType
{
    Axe = 0,
    BattleAxe = 1,
    Dagger = 2,
    GreatAxe = 3,
    GreatHammer = 4,
    GreatMace = 5,
    GreatSword = 6,
    Halberd = 7,
    Hammer = 8,
    KiteShield = 9,
    Knife = 10,
    LongSword = 11,
    Mace = 12,
    PickAxe = 13,
    Shield = 14,
    Shovel = 15,
    Spear = 16,
    Sword = 17,
    WarHammer = 18
}

[Serializable]
public struct ForgedGearData
{
    public int handleOreId;
    public int metalOreId;
    public bool hasGem;
    public int gemOreId;
}

[Serializable]
public class ForgedGear
{
    [FormerlySerializedAs("type")]
    public int id;
    public int level = 1;
    public ForgedGearData data;

    public ForgedGear(int id, ForgedGearData data, int level = 1)
    {
        this.id = id;
        this.level = level;
        this.data = data;
    }
}
