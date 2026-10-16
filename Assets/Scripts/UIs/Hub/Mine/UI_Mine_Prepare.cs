using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_Mine_Prepare : UI_Base
{
    [SerializeField] private PanelAnimator commonPanel;
    private enum Buttons
    {
        ExitButton,
        StartButton
    }

    private enum PanelAnimators
    {
        Panel
    }

    protected override void OnInit()
    {
        Bind<Button>(typeof(Buttons));
        Bind<PanelAnimator>(typeof(PanelAnimators));
        GetButton((int)Buttons.ExitButton).onClick.AddListener(
            () => Owner.RequestStateChange(CanvasController.CanvasState.BaseView));
        GetButton((int)Buttons.StartButton).onClick.AddListener(
            () => GameManager.Instance.SceneController.ChangeScene(SceneType.GameLoop));
    }

    protected override IEnumerator OnShow()
    {
        Coroutine commonAnimation = StartCoroutine(commonPanel.Show());
        yield return GetUI<PanelAnimator>((int)PanelAnimators.Panel).Show();
        yield return commonAnimation;
    }

    protected override IEnumerator OnHide()
    {
        StartCoroutine(commonPanel.Hide());
        yield return GetUI<PanelAnimator>((int)PanelAnimators.Panel).Hide();
    }
}
