using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Game/ForgedGearSO")]
public sealed class ForgedGearSO : ScriptableObject
{
    [SerializeField] private int id;
    [SerializeField] private ForgedGearType type;
    [FormerlySerializedAs("sellPrice")]
    [SerializeField, Min(1)] private int baseSellPrice;
    [SerializeField, Min(0f)] private float materialValueMultiplier;
    [SerializeField, Min(0f)] private float levelPriceBonus;
    [SerializeField, Min(1)] private int handleOreCost;
    [SerializeField, Min(1)] private int metalOreCost;
    [SerializeField, Min(1)] private int gemOreCost;
    [SerializeField] private GameObject iconModel;
    [SerializeField] private Vector3 iconRotation = new Vector3(0f, 25f, -35f);

    public int Id => id;
    public int BaseSellPrice => baseSellPrice;
    public int HandleOreCost => handleOreCost;
    public int MetalOreCost => metalOreCost;
    public int GemOreCost => gemOreCost;
    public GameObject IconModel => iconModel;
    public Vector3 IconRotation => iconRotation;

    public int GetSellPrice(ForgedGearData materials, int level)
    {
        double materialValue = (double)handleOreCost * OreDataDB.GetData(materials.handleOreId).SellPrice
            + (double)metalOreCost * OreDataDB.GetData(materials.metalOreId).SellPrice;
        if (materials.hasGem)
            materialValue += (double)gemOreCost * OreDataDB.GetData(materials.gemOreId).SellPrice;

        double price = (baseSellPrice + materialValue * materialValueMultiplier)
            * (1d + levelPriceBonus * (level - 1));
        return (int)Math.Min(Math.Floor(price), int.MaxValue);
    }
}
