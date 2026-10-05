using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_Smith_Equipment : UI_Base
{
    private enum UIs { UI_SmithInfoPanel }
    private enum Buttons
    {
        BackButton,
        ForgeCategoryButton,
        EquipmentCategoryButton
    }

    private enum PanelAnimators
    {
        Panel
    }

    protected override void OnInit()
    {
        Bind<Button>(typeof(Buttons));
        Bind<PanelAnimator>(typeof(PanelAnimators));
        Bind<GameObject>(typeof(UIs));
        GetGameObject((int)UIs.UI_SmithInfoPanel).GetComponent<UI_SmithInfoPanel>().Init();
        GetButton((int)Buttons.BackButton).onClick.AddListener(
            () => Owner.RequestStateChange(CanvasController.CanvasState.BaseView));
        GetButton((int)Buttons.ForgeCategoryButton).onClick.AddListener(
            () => Owner.RequestStateChange(CanvasController.CanvasState.Smith_Forge));
        GetButton((int)Buttons.EquipmentCategoryButton).onClick.AddListener(
            () => Owner.RequestStateChange(CanvasController.CanvasState.Smith_Equipment));
    }

    protected override IEnumerator OnShow()
    {
        yield return GetUI<PanelAnimator>((int)PanelAnimators.Panel).Show();
    }

    protected override IEnumerator OnHide()
    {
        yield return GetUI<PanelAnimator>((int)PanelAnimators.Panel).Hide();
    }

    private void OnDestroy()
    {
        if (!IsInitialized) return;
        if (GameManager.Instance == null) return;
        GetGameObject((int)UIs.UI_SmithInfoPanel).GetComponent<UI_SmithInfoPanel>().Dispose();
    }
}
