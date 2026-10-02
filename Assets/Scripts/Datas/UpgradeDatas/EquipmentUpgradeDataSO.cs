using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Upgrade/Equipment Upgrade Data")]
public sealed class EquipmentUpgradeDataSO : UpgradeDataSO
{
    [FormerlySerializedAs("targetEquipment")]
    [SerializeField] private int equipmentId;

    public int EquipmentId => equipmentId;
}
