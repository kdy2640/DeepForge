using System.Collections;
using UnityEngine.UI;

public sealed class UI_BaseView : UI_Base
{
    private enum Buttons
    {
        Shop_OrderButton,
        Shop_DealButton,
        Smith_ReinforceButton,
        Smith_ForgeButton,
        Mine_PrepareButton
    }

    private enum PanelAnimators
    {
        Panel
    }

    protected override void OnInit()
    {
        Bind<Button>(typeof(Buttons));
        Bind<PanelAnimator>(typeof(PanelAnimators));
        GetButton((int)Buttons.Shop_OrderButton).onClick.AddListener(
            () => Owner.RequestStateChange(CanvasController.CanvasState.Shop_Order));
        GetButton((int)Buttons.Shop_DealButton).onClick.AddListener(
            () => Owner.RequestStateChange(CanvasController.CanvasState.Shop_Deal));
        GetButton((int)Buttons.Smith_ReinforceButton).onClick.AddListener(
            () => Owner.RequestStateChange(CanvasController.CanvasState.Smith_Reinforce));
        GetButton((int)Buttons.Smith_ForgeButton).onClick.AddListener(
            () => Owner.RequestStateChange(CanvasController.CanvasState.Smith_Forge));
        GetButton((int)Buttons.Mine_PrepareButton).onClick.AddListener(
            () => Owner.RequestStateChange(CanvasController.CanvasState.Mine_Prepare));
    }

    protected override IEnumerator OnShow()
    {
        yield return GetUI<PanelAnimator>((int)PanelAnimators.Panel).Show();
    }

    protected override IEnumerator OnHide()
    {
        yield return GetUI<PanelAnimator>((int)PanelAnimators.Panel).Hide();
    }
}
