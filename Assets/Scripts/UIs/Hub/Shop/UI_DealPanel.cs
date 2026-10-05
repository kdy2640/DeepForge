using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_DealPanel : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    [SerializeField] private UI_DealVisualCard cardPrefab;
    [SerializeField] private Button dealButton;
    [SerializeField] private Text dealPriceText;
    [SerializeField] private Text dealButtonText;
    [SerializeField] private GameObject emptyMessage;

    private readonly Dictionary<UpgradeDataSO, UI_DealVisualCard> blueprints = new();
    private readonly Dictionary<OreAmount, UI_DealVisualCard> ores = new();
    private readonly Dictionary<ForgedGear, UI_DealVisualCard> gears = new();
    private UI_Shop_Deal shop;

    public event Action SelectionChanged;
    public event Action<int> OreReturnRequested;
    public event Action<string> TradeCompleted;
    public int Count => blueprints.Count + ores.Count + gears.Count;
    public long TotalPrice =>
        blueprints.Keys.Sum(data => (long)data.BlueprintPrice) +
        ores.Keys.Sum(amount => (long)amount.amount * OreDataDB.GetData(amount.oreId).SellPrice) +
        gears.Keys.Sum(gear => (long)ForgedGearDB.GetData(gear.id).SellPrice);

    public void Init(UI_Shop_Deal owner)
    {
        shop = owner;
        dealButton.onClick.AddListener(ConfirmDeal);
        shop.DealModeChanged += OnDealModeChanged;
        GameManager.Instance.StockManager.SubscribeStockDataChange(RefreshSummary);
        GameManager.Instance.Upgrade.SubscribeUpgradeChanged(RefreshSummary);
        RefreshSummary();
    }

    public bool IsSelected(UpgradeDataSO data) => blueprints.ContainsKey(data);
    public bool IsSelected(int oreId) => ores.Keys.Any(amount => amount.oreId == oreId);
    public bool IsSelected(ForgedGear gear) => gears.ContainsKey(gear);

    public int GetOreAmount(int oreId)
    {
        OreAmount selected = ores.Keys.FirstOrDefault(amount => amount.oreId == oreId);
        return selected == null ? 0 : selected.amount;
    }

    public void AddBlueprint(UpgradeDataSO data)
    {
        UpgradeAvailability availability = GameManager.Instance.Upgrade.GetBlueprintAvailability(data);
        if (availability != UpgradeAvailability.Available && availability != UpgradeAvailability.InsufficientCurrency) return;
        if (IsSelected(data)) return;
        shop.SetDealMode(UI_Shop_Deal.DealMode.Buy);
        UI_DealVisualCard card = Instantiate(cardPrefab, content, false);
        card.SetData(data.DisplayIcon, new Color32(185, 179, 169, 255),
            1, data.BlueprintPrice, () => RemoveBlueprint(data));
        blueprints.Add(data, card);
        card.Animate(true);
        RefreshSelection();
    }

    public void AddOre(int oreId, int amount)
    {
        int remaining = GameManager.Instance.StockManager.GetOreAmount(oreId) - GetOreAmount(oreId);
        int moved = Mathf.Min(amount, remaining);
        if (moved <= 0) return;
        shop.SetDealMode(UI_Shop_Deal.DealMode.Sell);
        OreAmount selected = ores.Keys.FirstOrDefault(entry => entry.oreId == oreId);
        if (selected == null)
        {
            selected = new OreAmount(oreId, moved);
            UI_DealVisualCard card = Instantiate(cardPrefab, content, false);
            ores.Add(selected, card);
            UpdateOreCard(selected);
            card.Animate(false);
        }
        else
        {
            selected.amount += moved;
            UpdateOreCard(selected);
        }
        RefreshSelection();
    }

    public void ReturnOre(int oreId, int amount)
    {
        OreAmount selected = ores.Keys.FirstOrDefault(entry => entry.oreId == oreId);
        if (selected == null || amount <= 0) return;
        selected.amount -= Mathf.Min(amount, selected.amount);
        if (selected.amount == 0)
        {
            RemoveOre(selected);
            return;
        }
        UpdateOreCard(selected);
        RefreshSelection();
    }

    private void UpdateOreCard(OreAmount selected)
    {
        OreDataSO data = OreDataDB.GetData(selected.oreId);
        UI_DealVisualCard card = ores[selected];
        card.SetData(data.Icon, data.Color, selected.amount,
            (long)selected.amount * data.SellPrice, () => RemoveOre(selected));
        card.SetReturnAction(() => OreReturnRequested.Invoke(selected.oreId));
    }

    public void AddGear(ForgedGear gear)
    {
        if (!GameManager.Instance.StockManager.StockData.ForgedGears.Contains(gear)) return;
        if (IsSelected(gear)) return;
        shop.SetDealMode(UI_Shop_Deal.DealMode.Sell);
        ForgedGearSO data = ForgedGearDB.GetData(gear.id);
        UI_DealVisualCard card = Instantiate(cardPrefab, content, false);
        card.SetData(null, new Color32(185, 179, 169, 255),
            1, data.SellPrice, () => RemoveGear(gear));
        gears.Add(gear, card);
        card.Animate(false);
        RefreshSelection();
    }

    private void RemoveBlueprint(UpgradeDataSO data)
    {
        blueprints[data].gameObject.SetActive(false);
        Destroy(blueprints[data].gameObject);
        blueprints.Remove(data);
        RefreshSelection();
    }

    private void RemoveOre(OreAmount amount)
    {
        ores[amount].gameObject.SetActive(false);
        Destroy(ores[amount].gameObject);
        ores.Remove(amount);
        RefreshSelection();
    }

    private void RemoveGear(ForgedGear gear)
    {
        gears[gear].gameObject.SetActive(false);
        Destroy(gears[gear].gameObject);
        gears.Remove(gear);
        RefreshSelection();
    }

    private void OnDealModeChanged(UI_Shop_Deal.DealMode mode)
    {
        foreach (UI_DealVisualCard card in blueprints.Values.Concat(ores.Values).Concat(gears.Values))
        {
            card.gameObject.SetActive(false);
            Destroy(card.gameObject);
        }
        blueprints.Clear();
        ores.Clear();
        gears.Clear();
        RefreshSummary();
        SelectionChanged.Invoke();
    }

    private void RefreshSelection()
    {
        if (Count == 0 && shop.CurrentDealMode != UI_Shop_Deal.DealMode.None)
        {
            shop.SetDealMode(UI_Shop_Deal.DealMode.None);
            return;
        }
        RefreshSummary();
        SelectionChanged.Invoke();
    }

    public void Clear()
    {
        if (shop.CurrentDealMode == UI_Shop_Deal.DealMode.None)
            RefreshSelection();
        else
            shop.SetDealMode(UI_Shop_Deal.DealMode.None);
    }

    private bool CanTrade()
    {
        if (Count == 0) return false;
        if (blueprints.Count > 0)
            return TotalPrice <= GameManager.Instance.StockManager.StockData.Currency &&
                blueprints.Keys.All(data => GameManager.Instance.Upgrade.GetBlueprintAvailability(data) == UpgradeAvailability.Available);
        return ores.Keys.All(amount => GameManager.Instance.StockManager.CanConsumeOre(amount)) &&
            gears.Keys.All(gear => GameManager.Instance.StockManager.StockData.ForgedGears.Contains(gear));
    }

    private void RefreshSummary()
    {
        bool buying = shop.CurrentDealMode == UI_Shop_Deal.DealMode.Buy;
        dealPriceText.text = Count == 0 ? "— G" : $"{TotalPrice:N0} G";
        dealButtonText.text = Count == 0 ? "거래" : buying ? "전체 구매" : "전체 판매";
        emptyMessage.SetActive(Count == 0);
        dealButton.interactable = CanTrade();
    }

    public void ConfirmDeal()
    {
        if (!CanTrade()) return;
        int count = Count;
        bool buying = shop.CurrentDealMode == UI_Shop_Deal.DealMode.Buy;
        // 전체 목록 검사를 통과한 뒤 기존 매니저의 거래 규칙을 적용한다.
        foreach (UpgradeDataSO data in blueprints.Keys)
            GameManager.Instance.Upgrade.TryBuyBlueprint(data);
        foreach (OreAmount amount in ores.Keys)
            GameManager.Instance.StockManager.TrySellOre(amount);
        foreach (ForgedGear gear in gears.Keys)
            GameManager.Instance.StockManager.TrySellForgedGear(gear);
        Clear();
        TradeCompleted.Invoke(buying ? $"설계도 {count}개를\n구매했네." : $"판매 {count}건을\n마쳤네.");
    }

    public void Dispose()
    {
        shop.DealModeChanged -= OnDealModeChanged;
        GameManager.Instance.StockManager.UnsubscribeStockDataChange(RefreshSummary);
        GameManager.Instance.Upgrade.UnsubscribeUpgradeChanged(RefreshSummary);
    }
}
