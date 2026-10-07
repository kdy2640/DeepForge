using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_BluePrintPanel : MonoBehaviour
{
    [SerializeField] private Button productCategoryButton;
    [SerializeField] private Button equipmentCategoryButton;
    [SerializeField] private Text productCategoryText;
    [SerializeField] private Text equipmentCategoryText;
    [SerializeField] private Color selectedCategoryColor = new Color32(48, 51, 57, 255);
    [SerializeField] private Color unselectedCategoryColor = new Color32(32, 36, 43, 255);
    [SerializeField] private Color selectedCategoryTextColor = new Color32(255, 201, 74, 255);
    [SerializeField] private Color unselectedCategoryTextColor = new Color32(242, 238, 229, 255);
    [SerializeField] private RectTransform content;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private UI_BluePrintVisualCard cardPrefab;

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
        productCategoryButton.image.color = showEquipment ? unselectedCategoryColor : selectedCategoryColor;
        equipmentCategoryButton.image.color = showEquipment ? selectedCategoryColor : unselectedCategoryColor;
        productCategoryText.color = showEquipment ? unselectedCategoryTextColor : selectedCategoryTextColor;
        equipmentCategoryText.color = showEquipment ? selectedCategoryTextColor : unselectedCategoryTextColor;

        foreach (UI_BluePrintVisualCard card in cards.Values)
        {
            card.gameObject.SetActive(false);
            Destroy(card.gameObject);
        }
        cards.Clear();
        IEnumerable<UpgradeDataSO> blueprints = showEquipment
            ? UpgradeDataDB.GetAll<EquipmentUpgradeDataSO>()
            : UpgradeDataDB.GetAll<ForgedGearUpgradeDataSO>();
        foreach (UpgradeDataSO data in blueprints.OrderBy(data => data.name, System.StringComparer.Ordinal))
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
