using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_MineView : UI_Base
{
    private enum UIs { UI_DepthGauge, UI_BackPackVisualizer }
    [SerializeField] private Button settingsButton;
    [SerializeField] private PanelAnimator commonPanel;
    [SerializeField] private Slider remainingLengthSlider;
    [SerializeField] private Image remainingLengthTrack;
    [SerializeField] private Image returnKeyBorder;
    [SerializeField] private Color ropeNormalColor = new Color(0.94f, 0.73f, 0.34f, 1f);
    [SerializeField] private Color ropeWarningColor = new Color(1f, 0.42f, 0.08f, 1f);
    [SerializeField] private Color ropeLimitColor = new Color(1f, 0.16f, 0.12f, 1f);
    private PlayerReturn playerReturn;
    private Image remainingLengthFill;
    private Color ropeTrackColor;

    protected override void OnInit()
    {
        settingsButton.onClick.AddListener(Owner.OpenSettings);
        playerReturn = Object.FindFirstObjectByType<PlayerReturn>();
        remainingLengthFill = remainingLengthSlider.fillRect.GetComponent<Image>();
        ropeTrackColor = remainingLengthTrack.color;
        remainingLengthSlider.normalizedValue = Mathf.Clamp01(
            1f - playerReturn.GetReturnDistance() / playerReturn.MaximumCableLength);
        Bind<GameObject>(typeof(UIs));
        GetGameObject((int)UIs.UI_DepthGauge).GetComponent<UI_DepthGauge>().Init();
        GetGameObject((int)UIs.UI_BackPackVisualizer).GetComponent<UI_BackPackVisualizer>().Init();
    }

    protected override IEnumerator OnShow()
    {
        Coroutine commonAnimation = StartCoroutine(commonPanel.Show());
        GetGameObject((int)UIs.UI_BackPackVisualizer).GetComponent<UI_BackPackVisualizer>().Refresh();
        yield return GetGameObject((int)UIs.UI_DepthGauge).GetComponent<UI_DepthGauge>().Show();
        yield return commonAnimation;
    }

    private void LateUpdate()
    {
        if (!IsInitialized) return;
        remainingLengthSlider.normalizedValue = Mathf.Clamp01(
            1f - playerReturn.GetReturnDistance() / playerReturn.MaximumCableLength);
        float warning = Mathf.InverseLerp(0.2f, 0f, remainingLengthSlider.normalizedValue);
        Color color = Color.Lerp(ropeNormalColor, ropeWarningColor, warning);
        color = Color.Lerp(color, ropeLimitColor, playerReturn.CableLimitFeedback);
        remainingLengthFill.color = color;
        returnKeyBorder.color = color;
        remainingLengthTrack.color = Color.Lerp(ropeTrackColor, ropeLimitColor, playerReturn.CableLimitFeedback);
    }

    protected override IEnumerator OnHide()
    {
        yield return commonPanel.Hide();
    }

    private void OnDestroy()
    {
        if (!IsInitialized) return;
        GetGameObject((int)UIs.UI_BackPackVisualizer).GetComponent<UI_BackPackVisualizer>().Dispose();
    }
}
