using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_BluePrintPanel : MonoBehaviour
{
    [SerializeField] private Button productCategoryButton;
    [SerializeField] private Button equipmentCategoryButton;
    [SerializeField] private Text productCategoryText;
    [SerializeField] private Text equipmentCategoryText;
    [SerializeField] private RectTransform content;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private UI_BluePrintVisualCard cardPrefab;
    [SerializeField] private UpgradeDataSO[] productBlueprints;
    [SerializeField] private UpgradeDataSO[] equipmentBlueprints;

    private readonly Dictionary<UpgradeDataSO, UI_BluePrintVisualCard> cards = new();
    private UI_DealPanel deal;
    private bool showEquipment = true;

    public void Init(UI_DealPanel dealPanel)
    {
        deal = dealPanel;
        productCategoryButton.onClick.AddListener(() => SelectCategory(false));
        equipmentCategoryButton.onClick.AddListener(() => SelectCategory(true));
        deal.SelectionChanged += RefreshSelection;
        GameManager.Instance.StockManager.SubscribeStockDataChange(Refresh);
        GameManager.Instance.Upgrade.SubscribeUpgradeChanged(Refresh);
        Refresh();
    }

    private void SelectCategory(bool equipment)
    {
        showEquipment = equipment;
        Refresh();
        scrollRect.StopMovement();
        content.anchoredPosition = Vector2.zero;
    }

    private void Refresh()
    {
        Color32 panelColor = new(48, 51, 57, 255);
        Color32 backgroundColor = new(32, 36, 43, 255);
        productCategoryButton.image.color = showEquipment ? backgroundColor : panelColor;
        equipmentCategoryButton.image.color = showEquipment ? panelColor : backgroundColor;
        productCategoryText.color = showEquipment ? new Color32(242, 238, 229, 255) : new Color32(255, 201, 74, 255);
        equipmentCategoryText.color = showEquipment ? new Color32(255, 201, 74, 255) : new Color32(242, 238, 229, 255);

        foreach (UI_BluePrintVisualCard card in cards.Values)
        {
            card.gameObject.SetActive(false);
            Destroy(card.gameObject);
        }
        cards.Clear();
        foreach (UpgradeDataSO data in showEquipment ? equipmentBlueprints : productBlueprints)
        {
            UI_BluePrintVisualCard card = Instantiate(cardPrefab, content, false);
            int unlockedLevel = GameManager.Instance.Upgrade.HasState(data)
                ? GameManager.Instance.Upgrade.GetState(data).unlockedLevel : 0;
            card.SetData(data, GameManager.Instance.Upgrade.GetBlueprintAvailability(data), unlockedLevel,
                () => deal.AddBlueprint(data));
            card.SetSelected(deal.IsSelected(data));
            cards.Add(data, card);
        }
    }

    private void RefreshSelection()
    {
        foreach (var entry in cards)
            entry.Value.SetSelected(deal.IsSelected(entry.Key));
    }

    public void Dispose()
    {
        deal.SelectionChanged -= RefreshSelection;
        GameManager.Instance.StockManager.UnsubscribeStockDataChange(Refresh);
        GameManager.Instance.Upgrade.UnsubscribeUpgradeChanged(Refresh);
    }
}
