using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_BaseView : UI_Base
{
    [SerializeField] private UI_OreViewPanel oreViewPanel;
    [SerializeField] private Button settingsButton;
    [SerializeField] private PanelAnimator commonPanel;
    private enum Buttons
    { 
    }

    private enum PanelAnimators
    { 
    }

    protected override void OnInit()
    {
        oreViewPanel.Init();
        settingsButton.onClick.AddListener(Owner.OpenSettings);
    }

    protected override IEnumerator OnShow()
    {
        yield return commonPanel.Show();
    }

    protected override IEnumerator OnHide()
    {
        yield return commonPanel.Hide();
    }
}
