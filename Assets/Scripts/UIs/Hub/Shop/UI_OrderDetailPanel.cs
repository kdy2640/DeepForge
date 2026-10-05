using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_OrderDetailPanel : MonoBehaviour
{
    [SerializeField] private Button closeButton;
    [SerializeField] private Image icon;
    [SerializeField] private Text titleText;
    [SerializeField] private Text summaryText;
    [SerializeField] private Text conditionsText;

    private UI_Shop_Order shop;
    private UI_OrderItemPanel items;

    public void Init(UI_Shop_Order owner, UI_OrderItemPanel itemPanel)
    {
        shop = owner;
        items = itemPanel;
        closeButton.onClick.AddListener(Close);
        shop.OrderSelected += OnOrderSelected;
        items.SelectionChanged += Refresh;
        Close();
    }

    private void OnOrderSelected(Order order)
    {
        if (order == null)
        {
            Close();
            return;
        }
        Refresh();
        gameObject.SetActive(true);
    }

    private void Refresh()
    {
        Order order = shop.SelectedOrder;
        if (order == null) return;
        bool completed = GameManager.Instance.BaseCamp.Order.IsCompleted(order.Index);
        int gearId = order.Conditions.OfType<GearOrderCondition>().Single().GearId;
        icon.sprite = UpgradeDataDB.GetForgedGearUpgrade(gearId).DisplayIcon;
        titleText.text = $"주문서 {order.Index + 1:00}";
        int ownedCount = GameManager.Instance.StockManager.StockData.ForgedGears.Count(
            gear => order.Conditions.All(condition => condition.IsSatisfied(gear)));
        summaryText.text = $"{ForgedGearDB.GetData(gearId).name}\n요청 수량  {order.RequiredCount}개\n보상  {order.RewardCurrency:N0} G\n"
            + (completed ? "납품 완료" : $"조건에 맞는 보유품  {ownedCount}개\n납품 선택  {items.SelectedGears.Count}/{order.RequiredCount}");

        StringBuilder text = new();
        foreach (OrderCondition condition in order.Conditions)
        {
            string description = condition switch
            {
                GearOrderCondition gear => $"제품  ·  {ForgedGearDB.GetData(gear.GearId).name}",
                HandleOreOrderCondition handle => $"손잡이 재료  ·  {OreDataDB.GetData(handle.OreId).DisplayName}",
                MetalOreOrderCondition metal => $"금속 재료  ·  {OreDataDB.GetData(metal.OreId).DisplayName}",
                GemOrderCondition gem => gem.HasGem ? $"보석  ·  {OreDataDB.GetData(gem.OreId).DisplayName}" : "보석  ·  미장착",
                _ => throw new System.NotSupportedException(condition.GetType().Name)
            };
            int satisfied = items.SelectedGears.Count(gear => condition.IsSatisfied(gear));
            string status = completed ? "완료" : $"{satisfied}/{order.RequiredCount}";
            string color = completed || satisfied == order.RequiredCount ? "#3DD6C6" : "#F2EEE5";
            text.AppendLine($"<color={color}>{description}    {status}</color>");
            text.AppendLine();
        }
        conditionsText.text = text.ToString();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void Dispose()
    {
        shop.OrderSelected -= OnOrderSelected;
        items.SelectionChanged -= Refresh;
    }
}
