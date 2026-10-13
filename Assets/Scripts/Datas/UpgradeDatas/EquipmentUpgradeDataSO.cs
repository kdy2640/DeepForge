using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Upgrade/Equipment Upgrade Data")]
public sealed class EquipmentUpgradeDataSO : UpgradeDataSO
{
    [FormerlySerializedAs("targetEquipment")]
    [SerializeField] private int equipmentId;
    [SerializeField] private List<EquipmentStatModifier> statModifiers = new();

    public int EquipmentId => equipmentId;
    public IReadOnlyList<EquipmentStatModifier> StatModifiers => statModifiers;
}
