using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_BaseView : UI_Base
{
    [SerializeField] private UI_OreViewPanel oreViewPanel;
    private enum Buttons
    { 
    }

    private enum PanelAnimators
    { 
    }

    protected override void OnInit()
    {
        oreViewPanel.Init();
    }

    protected override IEnumerator OnShow()
    {
        yield return null;
    }

    protected override IEnumerator OnHide()
    {
        yield return null;
    }
}
