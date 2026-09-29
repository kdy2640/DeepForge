using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_Smith_Reinforce : UI_Base
{
    private enum Buttons
    {
        BackButton,
        ReinforceButton
    }

    [SerializeField] private UpgradeDataSO upgradeData;
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
        GetButton((int)Buttons.ReinforceButton).onClick.AddListener(Reinforce);
        GameManager.Instance.Upgrade.GetState(upgradeData);
    }

    private void Reinforce()
    {
        UpgradeManager upgrade = GameManager.Instance.Upgrade;
        UpgradeAvailability availability = upgrade.GetUpgradeAvailability(upgradeData);
        lastResult = upgrade.TryUpgrade(upgradeData)
            ? "강화 성공" : availability.ToString();
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
