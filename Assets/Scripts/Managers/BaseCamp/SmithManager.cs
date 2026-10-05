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
        slot.product = new ForgedGear(id, data, level);
        slot.remainingSeconds = ForgeDuration;
        GameManager.Instance.StockManager.TryConsumeOre(costs);
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
            slot.product = null;
            slot.remainingSeconds = 0f;
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
                product = slot.IsWorking ? new ForgedGear(slot.product.id, slot.product.data, slot.product.level) : null,
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
            slots[i].product = saved.IsWorking ? new ForgedGear(saved.product.id, saved.product.data, Mathf.Max(1, saved.product.level)) : null;
            slots[i].remainingSeconds = saved.remainingSeconds;
        }
        SlotsChanged?.Invoke();
    }

    public void ResetSmithSaveData()
    {
        foreach (SmithSlot slot in slots)
        {
            slot.product = null;
            slot.remainingSeconds = 0f;
        }
        SlotsChanged?.Invoke();
    }
}
