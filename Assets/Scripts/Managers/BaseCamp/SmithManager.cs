using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SmithManager : MonoBehaviour
{
    private const float ForgeDuration = 3f;
    private readonly SmithSlot[] slots = { new(), new(), new(), new() };

    public IReadOnlyList<SmithSlot> Slots => slots;
    public bool HasEmptySlot => Array.Exists(slots, slot => !slot.IsWorking);
    public event Action SlotsChanged;

    public bool TryStartForge(int id, ForgedGearData data, int level)
    {
        SmithSlot slot = Array.Find(slots, entry => !entry.IsWorking);
        if (slot == null) return false;

        ForgedGearUpgradeDataSO blueprint = UpgradeDataDB.GetForgedGearUpgrade(id);
        if (level < 1 || level > blueprint.MaxLevel
            || !GameManager.Instance.Upgrade.HasState(blueprint)
            || level > GameManager.Instance.Upgrade.GetState(blueprint).unlockedLevel)
            return false;

        ForgedGearSO gearData = ForgedGearDB.GetData(id);
        List<OreAmount> costs = new()
        {
            new OreAmount(data.handleOreId, gearData.HandleOreCost),
            new OreAmount(data.metalOreId, gearData.MetalOreCost)
        };
        if (data.hasGem) costs.Add(new OreAmount(data.gemOreId, gearData.GemOreCost));
        if (!GameManager.Instance.StockManager.CanConsumeOre(costs)) return false;

        // 재고 알림 전에 슬롯을 점유해 등록 상태와 재료 차감을 함께 확인할 수 있게 한다.
        slot.workType = SmithWorkType.Product;
        slot.product = new ForgedGear(id, data, level);
        slot.remainingSeconds = ForgeDuration;
        GameManager.Instance.StockManager.TryConsumeOre(costs);
        SlotsChanged?.Invoke();
        return true;
    }

    public bool IsEquipmentUpgradeInProgress(int equipmentId)
    {
        return Array.Exists(slots, slot => slot.IsWorking
            && slot.workType == SmithWorkType.EquipmentUpgrade && slot.equipmentId == equipmentId);
    }

    public bool TryStartEquipmentUpgrade(int equipmentId, int level)
    {
        SmithSlot slot = Array.Find(slots, entry => !entry.IsWorking);
        if (slot == null || IsEquipmentUpgradeInProgress(equipmentId)) return false;

        EquipmentUpgradeDataSO data = UpgradeDataDB.GetEquipmentUpgrade(equipmentId);
        if (!GameManager.Instance.Upgrade.HasState(data)) return false;
        UpgradeState state = GameManager.Instance.Upgrade.GetState(data);
        if (level <= state.level || level > state.unlockedLevel || level > data.MaxLevel) return false;
        if (!GameManager.Instance.StockManager.CanConsumeOre(data.RequiredOres)) return false;

        slot.workType = SmithWorkType.EquipmentUpgrade;
        slot.equipmentId = equipmentId;
        slot.equipmentLevel = level;
        slot.remainingSeconds = ForgeDuration;
        GameManager.Instance.StockManager.TryConsumeOre(data.RequiredOres);
        SlotsChanged?.Invoke();
        return true;
    }

    private void Update()
    {
        foreach (SmithSlot slot in slots)
        {
            if (!slot.IsWorking) continue;
            slot.remainingSeconds -= Time.deltaTime;
            if (slot.remainingSeconds > 0f) continue;

            ForgedGear product = slot.product;
            SmithWorkType workType = slot.workType;
            int equipmentId = slot.equipmentId;
            int equipmentLevel = slot.equipmentLevel;
            slot.product = null;
            slot.workType = SmithWorkType.Product;
            slot.equipmentId = 0;
            slot.equipmentLevel = 0;
            slot.remainingSeconds = 0f;
            if (workType == SmithWorkType.EquipmentUpgrade)
                GameManager.Instance.Upgrade.CompleteEquipmentUpgrade(UpgradeDataDB.GetEquipmentUpgrade(equipmentId), equipmentLevel);
            else
                GameManager.Instance.StockManager.AddForgedGear(product);
            SlotsChanged?.Invoke();
        }
    }

    public SmithSaveData CreateSmithSaveData()
    {
        SmithSaveData saveData = new();
        for (int i = 0; i < slots.Length; i++)
        {
            SmithSlot slot = slots[i];
            saveData.slots[i] = new SmithSlot
            {
                workType = slot.workType,
                equipmentId = slot.equipmentId,
                equipmentLevel = slot.equipmentLevel,
                product = slot.IsWorking && slot.workType == SmithWorkType.Product
                    ? new ForgedGear(slot.product.id, slot.product.data, slot.product.level) : null,
                remainingSeconds = slot.remainingSeconds
            };
        }
        return saveData;
    }

    public void LoadSmithSaveData(SmithSaveData saveData)
    {
        // 슬롯 데이터가 없는 기존 세이브는 빈 슬롯 4개로 시작한다.
        if (saveData == null)
        {
            ResetSmithSaveData();
            return;
        }
        for (int i = 0; i < slots.Length; i++)
        {
            SmithSlot saved = saveData.slots[i];
            // 레벨 필드가 없던 저장 데이터의 제품은 1레벨로 불러온다.
            slots[i].workType = saved.workType;
            slots[i].equipmentId = saved.equipmentId;
            slots[i].equipmentLevel = saved.equipmentLevel;
            slots[i].product = saved.IsWorking && saved.workType == SmithWorkType.Product
                ? new ForgedGear(saved.product.id, saved.product.data, Mathf.Max(1, saved.product.level)) : null;
            slots[i].remainingSeconds = saved.remainingSeconds;
        }
        SlotsChanged?.Invoke();
    }

    public void ResetSmithSaveData()
    {
        foreach (SmithSlot slot in slots)
        {
            slot.product = null;
            slot.workType = SmithWorkType.Product;
            slot.equipmentId = 0;
            slot.equipmentLevel = 0;
            slot.remainingSeconds = 0f;
        }
        SlotsChanged?.Invoke();
    }
}
