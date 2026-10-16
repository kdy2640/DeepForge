using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class UpgradeLevelCost
{
    [SerializeField, Min(1)] private int blueprintPrice;
    [SerializeField] private List<OreAmount> requiredOres = new();

    public int BlueprintPrice => blueprintPrice;
    public List<OreAmount> RequiredOres => requiredOres;
}

/// <summary>
/// 업그레이드 노드가 공통으로 사용하는 데이터입니다.
/// </summary>
public abstract class UpgradeDataSO : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite displayIcon;

    [SerializeField] private List<UpgradeLevelCost> levelCosts = new();

    public string Id => id;
    public string DisplayName => displayName;
    public Sprite DisplayIcon => displayIcon;
    public int MaxLevel => levelCosts.Count;

    public UpgradeLevelCost GetLevelCost(int targetLevel) => levelCosts[targetLevel - 1];
}
