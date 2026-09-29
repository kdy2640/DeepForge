using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_Smith_Forge : UI_Base
{
    private enum Buttons
    {
        BackButton,
        ForgeButton
    }

    [Header("시연 버튼 대상")]
    [SerializeField] private ForgedGearType gearType = ForgedGearType.Sword;
    [SerializeField] private ForgedGearData gearData = new()
    {
        handleOreId = 1,
        metalOreId = 2
    };
    [SerializeField] private string lastResult;

    private enum PanelAnimators
    {
        Panel
    }

    protected override void OnInit()
    {
        Bind<Button>(typeof(Buttons));
        Bind<PanelAnimator>(typeof(PanelAnimators));
        GetButton((int)Buttons.BackButton).onClick.AddListener(
            () => Owner.RequestStateChange(CanvasController.CanvasState.BaseView));
        GetButton((int)Buttons.ForgeButton).onClick.AddListener(Forge);
    }

    private void Forge()
    {
        lastResult = GameManager.Instance.StockManager.TryForgeGear(gearType, gearData)
            ? "제품 제작 성공" : "제작 자원 부족";
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
