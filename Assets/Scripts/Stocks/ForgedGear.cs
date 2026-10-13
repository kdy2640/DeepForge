using System;
using UnityEngine;
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

    [NonSerialized] private Sprite icon;
    [NonSerialized] private bool iconReleased;

    public Sprite Icon => icon;
    internal bool IconReleased => iconReleased;
    [field: NonSerialized] public event Action IconChanged;

    public ForgedGear(int id, ForgedGearData data, int level = 1)
    {
        this.id = id;
        this.level = level;
        this.data = data;
    }

    internal void SetIcon(Sprite value)
    {
        icon = value;
        IconChanged?.Invoke();
    }

    public void ReleaseIcon()
    {
        iconReleased = true;
        if (icon == null) return;

        Sprite releasedIcon = icon;
        icon = null;
        IconChanged?.Invoke();
        UnityEngine.Object.Destroy(releasedIcon.texture);
        UnityEngine.Object.Destroy(releasedIcon);
    }
}
