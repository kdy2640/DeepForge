using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Upgrade/Equipment Upgrade Data")]
public sealed class EquipmentUpgradeDataSO : UpgradeDataSO
{
    [FormerlySerializedAs("targetEquipment")]
    [SerializeField] private int equipmentId;
    [SerializeField] private List<EquipmentStatModifier> statModifiers = new();
    [Tooltip("내부 강화 레벨 순서. 0은 기본 상태이며 MaxLevel + 1개를 지정합니다.")]
    [SerializeField] private List<Sprite> levelIcons = new();

    public int EquipmentId => equipmentId;
    public IReadOnlyList<EquipmentStatModifier> StatModifiers => statModifiers;
    public Sprite GetLevelIcon(int level) => levelIcons[level];
}
