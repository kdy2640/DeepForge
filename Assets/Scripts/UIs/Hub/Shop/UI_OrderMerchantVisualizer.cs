using UnityEngine;
using UnityEngine.UI;

public sealed class UI_OrderMerchantVisualizer : MonoBehaviour
{
    [SerializeField] private Text merchantText;
    private UI_Shop_Order shop;
    private UI_OrderItemPanel items;

    public void Init(UI_Shop_Order owner, UI_OrderItemPanel itemPanel)
    {
        shop = owner;
        items = itemPanel;
        shop.OrderSelected += OnOrderSelected;
        items.DeliveryCompleted += OnDeliveryCompleted;
        OnOrderSelected(shop.SelectedOrder);
    }

    private void OnOrderSelected(Order order)
    {
        merchantText.text = order == null ? "주문서를 골라\n조건을 확인해 보게."
            : GameManager.Instance.BaseCamp.Order.IsCompleted(order.Index) ? "이미 납품을 마친\n주문서라네."
            : "조건에 맞는 제품을\n준비해서 납품해 주게.";
    }

    private void OnDeliveryCompleted()
    {
        merchantText.text = "납품을 확인했네.\n보상을 받아 가게.";
    }

    public void Dispose()
    {
        shop.OrderSelected -= OnOrderSelected;
        items.DeliveryCompleted -= OnDeliveryCompleted;
    }
}
