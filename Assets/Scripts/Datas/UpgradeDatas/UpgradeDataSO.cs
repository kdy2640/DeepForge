using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 업그레이드 노드가 공통으로 사용하는 데이터입니다.
/// </summary>
[CreateAssetMenu(menuName = "Upgrade/Upgrade Data")]
public class UpgradeDataSO : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite displayIcon;

    [SerializeField] private EquipmentUpgradeType targetEquipment;
    [SerializeField, Min(1)] private int blueprintPrice = 30;
    [SerializeField] private List<OreAmount> requiredOres = new()
    {
        new OreAmount(1, 10),
        new OreAmount(2, 1)
    };
    [SerializeField] private int maxLevel = 1;

    public string Id => id;
    public string DisplayName => displayName;
    public Sprite DisplayIcon => displayIcon;
    public EquipmentUpgradeType TargetEquipment => targetEquipment;
    public int BlueprintPrice => blueprintPrice;
    public List<OreAmount> RequiredOres => requiredOres;
    public int MaxLevel => maxLevel;

}
