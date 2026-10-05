using System;
using System.Collections;
using UnityEngine;

public sealed class UI_Shop_Deal : UI_Base
{
    public enum DealMode { None, Buy, Sell }
    private enum PanelAnimators { Panel }
    private enum UIs
    {
        UI_OptionPanel, UI_MerchantVisualizer, UI_BluePrintPanel,
        UI_DealPanel, UI_InvenVisualPanel, UI_CanvasControlButton
    }

    public event Action<DealMode> DealModeChanged;
    public DealMode CurrentDealMode { get; private set; }

    protected override void OnInit()
    {
        Bind<PanelAnimator>(typeof(PanelAnimators));
        Bind<GameObject>(typeof(UIs));
        UI_DealPanel deal = GetGameObject((int)UIs.UI_DealPanel).GetComponent<UI_DealPanel>();
        deal.Init(this);
        GetGameObject((int)UIs.UI_OptionPanel).GetComponent<UI_OptionPanel>().Init(ReturnToBaseView);
        GetGameObject((int)UIs.UI_BluePrintPanel).GetComponent<UI_BluePrintPanel>().Init(deal);
        GetGameObject((int)UIs.UI_InvenVisualPanel).GetComponent<UI_InvenVisualPanel>().Init(deal);
        GetGameObject((int)UIs.UI_MerchantVisualizer).GetComponent<UI_MerchantVisualizer>().Init(this, deal);
        GetGameObject((int)UIs.UI_CanvasControlButton).GetComponent<UI_CanvasControlButton>().Init(OpenOrders);
    }

    public void SetDealMode(DealMode mode)
    {
        if (CurrentDealMode == mode) return;
        CurrentDealMode = mode;
        DealModeChanged.Invoke(mode);
    }

    public void ReturnToBaseView()
    {
        Owner.RequestStateChange(CanvasController.CanvasState.BaseView);
    }

    public void OpenOrders()
    {
        Owner.RequestStateChange(CanvasController.CanvasState.Shop_Order);
    }

    protected override IEnumerator OnShow()
    {
        GetGameObject((int)UIs.UI_DealPanel).GetComponent<UI_DealPanel>().Clear();
        yield return GetUI<PanelAnimator>((int)PanelAnimators.Panel).Show();
    }

    protected override IEnumerator OnHide()
    {
        yield return GetUI<PanelAnimator>((int)PanelAnimators.Panel).Hide();
    }

    private void OnDestroy()
    {
        if (!IsInitialized) return;
        // 종료 시 전역 매니저가 먼저 파괴되면 구독 대상도 이미 사라진 상태다.
        if (GameManager.Instance == null) return;
        GetGameObject((int)UIs.UI_OptionPanel).GetComponent<UI_OptionPanel>().Dispose();
        GetGameObject((int)UIs.UI_BluePrintPanel).GetComponent<UI_BluePrintPanel>().Dispose();
        GetGameObject((int)UIs.UI_InvenVisualPanel).GetComponent<UI_InvenVisualPanel>().Dispose();
        GetGameObject((int)UIs.UI_MerchantVisualizer).GetComponent<UI_MerchantVisualizer>().Dispose();
        GetGameObject((int)UIs.UI_DealPanel).GetComponent<UI_DealPanel>().Dispose();
    }
}
