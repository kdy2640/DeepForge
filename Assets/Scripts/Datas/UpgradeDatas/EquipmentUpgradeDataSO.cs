using UnityEngine;

[CreateAssetMenu(menuName = "Upgrade/Equipment Upgrade Data")]
public sealed class EquipmentUpgradeDataSO : UpgradeDataSO
{
    [SerializeField] private EquipmentType targetEquipment = EquipmentType.Count;

    public EquipmentType TargetEquipment => targetEquipment;
}
