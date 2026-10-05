using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_InvenVisualPanel : MonoBehaviour
{
    [SerializeField] private RectTransform resourceContent;
    [SerializeField] private RectTransform weaponContent;
    [SerializeField] private UI_InvenVisualCard resourceCardPrefab;
    [SerializeField] private UI_InvenVisualCard gearCardPrefab;
    [SerializeField] private int[] oreIds = { 1, 2, 3, 4, 5, 6, 7, 8 };
    [SerializeField] private Button quantityOneButton;
    [SerializeField] private Button quantityTenButton;
    [SerializeField] private Button quantityAllButton;

    private readonly Dictionary<int, UI_InvenVisualCard> oreCards = new();
    private readonly Dictionary<ForgedGear, UI_InvenVisualCard> gearCards = new();
    private UI_DealPanel deal;
    private int transferAmount = 1;

    public void Init(UI_DealPanel dealPanel)
    {
        deal = dealPanel;
        foreach (int oreId in oreIds)
            oreCards.Add(oreId, Instantiate(resourceCardPrefab, resourceContent, false));
        quantityOneButton.onClick.AddListener(() => SelectQuantity(1));
        quantityTenButton.onClick.AddListener(() => SelectQuantity(10));
        quantityAllButton.onClick.AddListener(() => SelectQuantity(int.MaxValue));
        SelectQuantity(transferAmount);
        deal.SelectionChanged += RefreshSelection;
        deal.OreReturnRequested += ReturnOre;
        GameManager.Instance.StockManager.SubscribeStockDataChange(Refresh);
        Refresh();
    }

    private void Refresh()
    {
        foreach (UI_InvenVisualCard card in gearCards.Values)
        {
            card.gameObject.SetActive(false);
            Destroy(card.gameObject);
        }
        gearCards.Clear();

        RefreshResources();
        foreach (ForgedGear gear in GameManager.Instance.StockManager.StockData.ForgedGears)
        {
            UI_InvenVisualCard card = Instantiate(gearCardPrefab, weaponContent, false);
            card.SetData(ForgedGearDB.GetData(gear.id), () => deal.AddGear(gear));
            card.SetSelected(deal.IsSelected(gear));
            gearCards.Add(gear, card);
        }
    }

    private void RefreshSelection()
    {
        RefreshResources();
        foreach (var entry in gearCards)
            entry.Value.SetSelected(deal.IsSelected(entry.Key));
    }

    private void RefreshResources()
    {
        foreach (int oreId in oreIds)
        {
            int remaining = Mathf.Max(0, GameManager.Instance.StockManager.GetOreAmount(oreId) - deal.GetOreAmount(oreId));
            UI_InvenVisualCard card = oreCards[oreId];
            card.SetData(OreDataDB.GetData(oreId), remaining, () => deal.AddOre(oreId, transferAmount));
            card.SetSelected(deal.IsSelected(oreId));
        }
    }

    private void SelectQuantity(int amount)
    {
        transferAmount = amount;
        Color32 selected = new(121, 115, 107, 255);
        Color32 normal = new(32, 36, 43, 255);
        quantityOneButton.image.color = amount == 1 ? selected : normal;
        quantityTenButton.image.color = amount == 10 ? selected : normal;
        quantityAllButton.image.color = amount == int.MaxValue ? selected : normal;
    }

    private void ReturnOre(int oreId)
    {
        deal.ReturnOre(oreId, transferAmount);
    }

    public void Dispose()
    {
        deal.SelectionChanged -= RefreshSelection;
        deal.OreReturnRequested -= ReturnOre;
        GameManager.Instance.StockManager.UnsubscribeStockDataChange(Refresh);
    }
}
