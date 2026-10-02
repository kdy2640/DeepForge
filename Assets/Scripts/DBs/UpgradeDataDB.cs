using System.Collections.Generic;
using UnityEngine;

public static class UpgradeDataDB
{
    private static readonly Dictionary<string, UpgradeDataSO> upgradeDataMap = new();
    private static readonly Dictionary<int, EquipmentUpgradeDataSO> equipmentUpgradeDataMap = new();
    private static readonly Dictionary<int, ForgedGearUpgradeDataSO> forgedGearUpgradeDataMap = new();

    static UpgradeDataDB()
    {
        foreach (UpgradeDataSO data in Resources.LoadAll<UpgradeDataSO>("SOs/UpgradeDatas"))
        {
            upgradeDataMap.Add(data.Id, data);
            if (data is EquipmentUpgradeDataSO equipmentUpgrade)
                equipmentUpgradeDataMap.Add(equipmentUpgrade.EquipmentId, equipmentUpgrade);
            else if (data is ForgedGearUpgradeDataSO forgedGearUpgrade)
                forgedGearUpgradeDataMap.Add(forgedGearUpgrade.ForgedGearId, forgedGearUpgrade);
        }
    }

    // 저장된 ID로 Resources/SOs/UpgradeDatas 안의 업그레이드 데이터를 찾는다.
    public static UpgradeDataSO GetData(string id)
    {
        if (upgradeDataMap.TryGetValue(id, out UpgradeDataSO result))
            return result;

        Debug.LogWarning($"There is no UpgradeDataSO. id : {id}");
        return null;
    }

    public static EquipmentUpgradeDataSO GetEquipmentUpgrade(int equipmentId)
    {
        return equipmentUpgradeDataMap[equipmentId];
    }

    public static ForgedGearUpgradeDataSO GetForgedGearUpgrade(int forgedGearId)
    {
        return forgedGearUpgradeDataMap[forgedGearId];
    }
}
