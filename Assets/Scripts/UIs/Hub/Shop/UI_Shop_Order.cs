using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public sealed class UI_Shop_Order : UI_Base
{
    private enum UIs
    {
        UI_OptionPanel, UI_OrderMerchantVisualizer, UI_OrderListPanel,
        UI_OrderItemPanel, UI_OrderInvenVisualPanel, UI_CanvasControlButton,
        UI_OrderDetailPanel
    }

    public Order SelectedOrder { get; private set; }
    public event Action<Order> OrderSelected;

    private enum PanelAnimators
    {
        Panel
    }

    protected override void OnInit()
    {
        Bind<PanelAnimator>(typeof(PanelAnimators));
        Bind<GameObject>(typeof(UIs));
        GameManager.Instance.BaseCamp.Order.OrdersChanged += OnOrdersChanged;
        UI_OrderItemPanel items = GetGameObject((int)UIs.UI_OrderItemPanel).GetComponent<UI_OrderItemPanel>();
        items.Init(this);
        GetGameObject((int)UIs.UI_OrderDetailPanel).GetComponent<UI_OrderDetailPanel>().Init(this, items);
        GetGameObject((int)UIs.UI_OrderListPanel).GetComponent<UI_OrderListPanel>().Init(this);
        GetGameObject((int)UIs.UI_OrderInvenVisualPanel).GetComponent<UI_OrderInvenVisualPanel>().Init(items);
        GetGameObject((int)UIs.UI_OrderMerchantVisualizer).GetComponent<UI_OrderMerchantVisualizer>().Init(this, items);
        GetGameObject((int)UIs.UI_OptionPanel).GetComponent<UI_OptionPanel>().Init(ReturnToBaseView);
        GetGameObject((int)UIs.UI_CanvasControlButton).GetComponent<UI_CanvasControlButton>().Init(OpenDeal);
    }

    public void SelectOrder(Order order)
    {
        SelectedOrder = order;
        OrderSelected.Invoke(order);
    }

    public void ReturnToBaseView()
    {
        Owner.RequestStateChange(CanvasController.CanvasState.BaseView);
    }

    public void OpenDeal()
    {
        Owner.RequestStateChange(CanvasController.CanvasState.Shop_Deal);
    }

    private void OnOrdersChanged()
    {
        if (SelectedOrder != null && !GameManager.Instance.BaseCamp.Order.Orders.Contains(SelectedOrder))
            SelectOrder(null);
    }

    protected override IEnumerator OnShow()
    {
        SelectOrder(null);
        yield return GetUI<PanelAnimator>((int)PanelAnimators.Panel).Show();
    }

    protected override IEnumerator OnHide()
    {
        GetGameObject((int)UIs.UI_OrderDetailPanel).GetComponent<UI_OrderDetailPanel>().Close();
        yield return GetUI<PanelAnimator>((int)PanelAnimators.Panel).Hide();
    }

    private void OnDestroy()
    {
        if (!IsInitialized) return;
        // 종료 시 전역 매니저가 먼저 파괴되면 구독 대상도 이미 사라진 상태다.
        if (GameManager.Instance == null) return;
        GameManager.Instance.BaseCamp.Order.OrdersChanged -= OnOrdersChanged;
        GetGameObject((int)UIs.UI_OptionPanel).GetComponent<UI_OptionPanel>().Dispose();
        GetGameObject((int)UIs.UI_OrderListPanel).GetComponent<UI_OrderListPanel>().Dispose();
        GetGameObject((int)UIs.UI_OrderDetailPanel).GetComponent<UI_OrderDetailPanel>().Dispose();
        GetGameObject((int)UIs.UI_OrderInvenVisualPanel).GetComponent<UI_OrderInvenVisualPanel>().Dispose();
        GetGameObject((int)UIs.UI_OrderMerchantVisualizer).GetComponent<UI_OrderMerchantVisualizer>().Dispose();
        GetGameObject((int)UIs.UI_OrderItemPanel).GetComponent<UI_OrderItemPanel>().Dispose();
    }
}
