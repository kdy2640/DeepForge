using UnityEngine;

[CreateAssetMenu(menuName = "Upgrade/Forged Gear Upgrade Data")]
public sealed class ForgedGearUpgradeDataSO : UpgradeDataSO
{
    [SerializeField] private int forgedGearId;

    public int ForgedGearId => forgedGearId;
}
