using UnityEngine;

public enum EquipmentType
{
    Pickaxe = 0,
    Backpack = 1,
    Boots = 2,
    Lantern = 3,
    Count
}

[CreateAssetMenu(menuName = "Game/EquipmentDataSO")]
public sealed class EquipmentDataSO : ScriptableObject
{
    [SerializeField] private int id;
    [SerializeField] private EquipmentType type;

    public int Id => id;
}
