using UnityEngine;

[CreateAssetMenu(menuName = "Game/ForgedGearSO")]
public sealed class ForgedGearSO : ScriptableObject
{
    [SerializeField] private int id;
    [SerializeField] private ForgedGearType type;
    [SerializeField, Min(1)] private int sellPrice;
    [SerializeField, Min(1)] private int handleOreCost;
    [SerializeField, Min(1)] private int metalOreCost;
    [SerializeField, Min(1)] private int gemOreCost;

    public int Id => id;
    public int SellPrice => sellPrice;
    public int HandleOreCost => handleOreCost;
    public int MetalOreCost => metalOreCost;
    public int GemOreCost => gemOreCost;
}
