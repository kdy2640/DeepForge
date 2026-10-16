using System.Collections.Generic;
using UnityEngine;

public sealed class GameLoopManager : MonoBehaviour
{
    [SerializeField] private UpgradeManager upgradeManager;
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
        backPack.SetCapacity(Mathf.RoundToInt(
            upgradeManager.RuntimeStat.Equipment.Get(EquipmentStatType.BackpackCapacity)));
    }

    public bool DepositBackPack()
    {
        if (backPack.Count == 0)
            return false;

        List<OreAmount> ores = backPack.CreateSaveData();
        backPack.Clear();
        GameManager.Instance.StockManager.AddOre(ores);
        GameManager.Instance.Utility.Audio.PlaySFX(SFXType.Camp_Deposit);
        return true;
    }
}
