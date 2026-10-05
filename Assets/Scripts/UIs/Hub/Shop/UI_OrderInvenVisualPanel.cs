using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_OrderInvenVisualPanel : MonoBehaviour
{
    [SerializeField] private RectTransform resourceContent;
    [SerializeField] private RectTransform weaponContent;
    [SerializeField] private UI_InvenVisualCard resourceCardPrefab;
    [SerializeField] private UI_InvenVisualCard gearCardPrefab;
    [SerializeField] private int[] oreIds = { 1, 2, 3, 4, 5, 6, 7, 8 };

    private readonly Dictionary<int, UI_InvenVisualCard> oreCards = new();
    private readonly Dictionary<ForgedGear, UI_InvenVisualCard> gearCards = new();
    private UI_OrderItemPanel items;

    public void Init(UI_OrderItemPanel itemPanel)
    {
        items = itemPanel;
        foreach (int oreId in oreIds)
            oreCards.Add(oreId, Instantiate(resourceCardPrefab, resourceContent, false));
        items.SelectionChanged += RefreshSelection;
        GameManager.Instance.StockManager.SubscribeStockDataChange(Refresh);
        Refresh();
    }

    private void Refresh()
    {
        foreach (int oreId in oreIds)
        {
            UI_InvenVisualCard card = oreCards[oreId];
            card.SetData(OreDataDB.GetData(oreId), GameManager.Instance.StockManager.GetOreAmount(oreId), () => { });
            card.GetComponent<Button>().interactable = false;
        }
        foreach (UI_InvenVisualCard card in gearCards.Values)
        {
            card.gameObject.SetActive(false);
            Destroy(card.gameObject);
        }
        gearCards.Clear();
        foreach (ForgedGear gear in GameManager.Instance.StockManager.StockData.ForgedGears)
        {
            UI_InvenVisualCard card = Instantiate(gearCardPrefab, weaponContent, false);
            card.SetData(ForgedGearDB.GetData(gear.id), () => items.AddGear(gear));
            gearCards.Add(gear, card);
        }
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        foreach (var entry in gearCards)
        {
            entry.Value.SetSelected(items.SelectedGears.Contains(entry.Key));
            entry.Value.GetComponent<Button>().interactable = items.CanSelect(entry.Key);
        }
    }

    public void Dispose()
    {
        items.SelectionChanged -= RefreshSelection;
        GameManager.Instance.StockManager.UnsubscribeStockDataChange(Refresh);
    }
}
