using System.Collections;
using UnityEngine;

public sealed class UI_MineView : UI_Base
{
    private enum UIs { UI_DepthGauge, UI_BackPackVisualizer }

    protected override void OnInit()
    {
        Bind<GameObject>(typeof(UIs));
        GetGameObject((int)UIs.UI_DepthGauge).GetComponent<UI_DepthGauge>().Init();
        GetGameObject((int)UIs.UI_BackPackVisualizer).GetComponent<UI_BackPackVisualizer>().Init();
    }

    protected override IEnumerator OnShow()
    {
        GetGameObject((int)UIs.UI_BackPackVisualizer).GetComponent<UI_BackPackVisualizer>().Refresh();
        yield return GetGameObject((int)UIs.UI_DepthGauge).GetComponent<UI_DepthGauge>().Show();
    }

    private void OnDestroy()
    {
        if (!IsInitialized) return;
        GetGameObject((int)UIs.UI_BackPackVisualizer).GetComponent<UI_BackPackVisualizer>().Dispose();
    }
}
