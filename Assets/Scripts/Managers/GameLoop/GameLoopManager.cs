using System.Collections.Generic;
using UnityEngine;

public sealed class GameLoopManager : MonoBehaviour
{
    [SerializeField] private UpgradeManager upgradeManager;
    [SerializeField] private int backpackEquipmentId = 1;
    [SerializeField] private int[] backpackCapacities = { 50, 100, 150 };
    [SerializeField] private BackPack backPack = new();

    public BackPack BackPack => backPack;

    private void Start()
    {
        upgradeManager.SubscribeUpgradeChanged(RefreshCapacity);
        RefreshCapacity();
    }

    private void OnDestroy()
    {
        upgradeManager.UnsubscribeUpgradeChanged(RefreshCapacity);
    }

    private void RefreshCapacity()
    {
        int level = upgradeManager.RuntimeLevel.GetEquipment(backpackEquipmentId);
        backPack.SetCapacity(backpackCapacities[Mathf.Min(level, backpackCapacities.Length - 1)]);
    }

    public bool DepositBackPack()
    {
        if (backPack.Count == 0)
            return false;

        List<OreAmount> ores = backPack.CreateSaveData();
        backPack.Clear();
        GameManager.Instance.StockManager.AddOre(ores);
        return true;
    }
}
