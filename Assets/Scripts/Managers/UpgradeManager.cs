using System;
using System.Collections.Generic;
using UnityEngine;

public enum UpgradeAvailability
{
    Available,
    InvalidData,
    MaxLevel,
    InsufficientCurrency,
    MissingBlueprint,
    BlueprintAlreadyOwned,
    InsufficientOre
}

public class UpgradeManager : MonoBehaviour
{
    [SerializeField] private List<UpgradeState> upgradeStates = new();
    [SerializeField] private RuntimeLevel runtimeLevel = new();

    public RuntimeLevel RuntimeLevel => runtimeLevel;

    private readonly Dictionary<string, UpgradeState> upgradeStateMap = new();
    private Action onUpgradeChanged;

    private void Awake()
    {
        foreach (UpgradeState state in upgradeStates)
            upgradeStateMap.Add(state.data.Id, state);
        RefreshRuntimeData();
    }

    public UpgradeState GetState(UpgradeDataSO data)
    {
        if (data == null || string.IsNullOrEmpty(data.Id))
            return null;

        if (upgradeStateMap.TryGetValue(data.Id, out UpgradeState state))
        {
            if (state.data != data)
            {
                Debug.LogError($"중복된 UpgradeData id가 있습니다: {data.Id}");
                return null;
            }

            return state;
        }

        state = new UpgradeState { data = data, level = 0 };
        upgradeStates.Add(state);
        upgradeStateMap.Add(data.Id, state);
        return state;
    }

    public bool HasState(UpgradeDataSO data)
    {
        return data != null && !string.IsNullOrEmpty(data.Id)
            && upgradeStateMap.ContainsKey(data.Id);
    }

    public bool IsMaxLevel(UpgradeState state)
    {
        return state != null && state.data != null && state.level >= state.data.MaxLevel;
    }

    public UpgradeAvailability GetUpgradeAvailability(UpgradeDataSO data)
    {
        if (data == null || string.IsNullOrEmpty(data.Id))
            return UpgradeAvailability.InvalidData;

        int level = 0;
        if (upgradeStateMap.TryGetValue(data.Id, out UpgradeState state))
        {
            if (state.data != data)
                return UpgradeAvailability.InvalidData;

            level = state.level;
        }

        if (level >= data.MaxLevel)
            return UpgradeAvailability.MaxLevel;

        if (state == null || state.level >= state.unlockedLevel)
            return UpgradeAvailability.MissingBlueprint;

        return GameManager.Instance.StockManager.CanConsumeOre(data.RequiredOres)
            ? UpgradeAvailability.Available
            : UpgradeAvailability.InsufficientOre;
    }

    public UpgradeAvailability GetBlueprintAvailability(UpgradeDataSO data)
    {
        if (data == null || string.IsNullOrEmpty(data.Id))
            return UpgradeAvailability.InvalidData;

        if (upgradeStateMap.TryGetValue(data.Id, out UpgradeState state))
        {
            if (state.data != data)
                return UpgradeAvailability.InvalidData;
            if (IsMaxLevel(state))
                return UpgradeAvailability.MaxLevel;
            if (state.unlockedLevel >= data.MaxLevel)
                return UpgradeAvailability.BlueprintAlreadyOwned;
        }

        return GameManager.Instance.StockManager.CanConsumeCurrency(data.BlueprintPrice)
            ? UpgradeAvailability.Available
            : UpgradeAvailability.InsufficientCurrency;
    }

    public bool TryBuyBlueprint(UpgradeDataSO data)
    {
        if (GetBlueprintAvailability(data) != UpgradeAvailability.Available)
            return false;

        // 비용 검사 이후, 재고 변경 알림 전에 보유 상태를 반영한다.
        GetState(data).unlockedLevel++;
        GameManager.Instance.StockManager.TryConsumeCurrency(data.BlueprintPrice);
        onUpgradeChanged?.Invoke();
        return true;
    }

    public bool CanUpgrade(UpgradeDataSO data)
    {
        return GetUpgradeAvailability(data) == UpgradeAvailability.Available;
    }

    public bool TryUpgrade(UpgradeDataSO data)
    {
        if (!CanUpgrade(data))
            return false;

        UpgradeState state = GetState(data);
        state.level++;
        RefreshRuntimeData();
        GameManager.Instance.StockManager.TryConsumeOre(data.RequiredOres);
        GameManager.Instance.Utility.Audio.PlaySFX(SFXType.Hub_Upgrade);
        onUpgradeChanged?.Invoke();
        return true;
    }

    private void RefreshRuntimeData()
    {
        runtimeLevel.Clear();
        foreach (UpgradeState state in upgradeStates)
        {
            if (state.data is EquipmentUpgradeDataSO equipmentUpgrade)
                runtimeLevel.SetEquipment(equipmentUpgrade.EquipmentId, state.level);
            else if (state.data is ForgedGearUpgradeDataSO forgedGearUpgrade)
                runtimeLevel.SetForgedGear(forgedGearUpgrade.ForgedGearId, state.level);
        }
    }

    public void SubscribeUpgradeChanged(Action callback)
    {
        onUpgradeChanged += callback;
    }

    public void UnsubscribeUpgradeChanged(Action callback)
    {
        onUpgradeChanged -= callback;
    }

    public List<UpgradeSaveData> CreateUpgradeSaveData()
    {
        List<UpgradeSaveData> saveData = new();
        foreach (UpgradeState state in upgradeStates)
            saveData.Add(new UpgradeSaveData(state.data.Id, state.level, state.unlockedLevel));

        return saveData;
    }

    public void LoadUpgradeSaveData(List<UpgradeSaveData> saveData)
    {
        upgradeStates.Clear();
        upgradeStateMap.Clear();

        if (saveData != null)
        {
            foreach (UpgradeSaveData savedState in saveData)
            {
                if (savedState == null || string.IsNullOrEmpty(savedState.id))
                    continue;

                UpgradeDataSO data = UpgradeDataDB.GetData(savedState.id);
                if (data == null)
                    continue;

                UpgradeState state = GetState(data);
                state.level = Mathf.Clamp(savedState.level, 0, data.MaxLevel);
                state.unlockedLevel = Mathf.Clamp(savedState.unlockedLevel, state.level, data.MaxLevel);
            }
        }

        RefreshRuntimeData();
        onUpgradeChanged?.Invoke();
    }

    public void ResetUpgradeSaveData()
    {
        upgradeStates.Clear();
        upgradeStateMap.Clear();
        RefreshRuntimeData();
        onUpgradeChanged?.Invoke();
    }
}
