using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsPopup : MonoBehaviour
{
    [SerializeField] private Canvas SettingCanvas;
    [SerializeField] private RectTransform popupContainer;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backdropButton;
    [SerializeField] private Button toggleButton;
    [SerializeField] private TMP_Text toggleText;
    [SerializeField] private Button gameExitButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI masterVolText;
    [SerializeField] private TextMeshProUGUI bgmVolText;
    [SerializeField] private TextMeshProUGUI sfxVolText;

    private CanvasController owner;
    private AudioManager audioManager;
    private Tween popupTween;
    private bool soundEnabled;
    private float previousMaster = 1f;
    private float previousBGM = 1f;
    private float previousSFX = 1f;
    public bool IsBusy { get; private set; }

    public void Init(CanvasController controller)
    {
        owner = controller;
        audioManager = GameManager.Instance.Utility.Audio;
        closeButton.onClick.AddListener(owner.CloseSettings);
        backdropButton.onClick.AddListener(owner.CloseSettings);
        toggleButton.onClick.AddListener(Toggle);
        gameExitButton.onClick.AddListener(ExitGame);
        resetButton.onClick.AddListener(ResetGame);
        masterSlider.onValueChanged.AddListener(OnMasterChanged);
        bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        gameObject.SetActive(false);
    }

    public void Open()
    {
        gameObject.SetActive(true);
        SetBusy(false);
        masterSlider.SetValueWithoutNotify(audioManager.MasterVolume);
        bgmSlider.SetValueWithoutNotify(audioManager.BGMVolume);
        sfxSlider.SetValueWithoutNotify(audioManager.SFXVolume);
        RefreshVolumeText();
        popupContainer.localScale = Vector3.zero;
        if (popupTween != null) popupTween.Kill();
        popupTween = popupContainer.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public IEnumerator Close()
    {
        SetBusy(true);
        if (popupTween != null) popupTween.Kill();
        popupTween = popupContainer.DOScale(0f, 0.2f).SetEase(Ease.InBack).SetUpdate(true);
        yield return popupTween.WaitForCompletion();
        popupTween = null;
        gameObject.SetActive(false);
    }

    private void OnMasterChanged(float value)
    {
        audioManager.SetMasterVolume(value);
        RefreshVolumeText();
    }

    private void OnBGMChanged(float value)
    {
        audioManager.SetBGMVolume(value);
        RefreshVolumeText();
    }

    private void OnSFXChanged(float value)
    {
        audioManager.SetSFXVolume(value);
        audioManager.PlaySFX(SFXType.Global_ButtonClick);
        RefreshVolumeText();
    }

    private void RefreshVolumeText()
    {
        masterVolText.text = $"{masterSlider.value * 100f:0}%";
        bgmVolText.text = $"{bgmSlider.value * 100f:0}%";
        sfxVolText.text = $"{sfxSlider.value * 100f:0}%";
        soundEnabled = masterSlider.value > 0f || bgmSlider.value > 0f || sfxSlider.value > 0f;
        toggleText.text = soundEnabled ? "OFF" : "ON";
    }

    private void Toggle()
    {
        audioManager.PlaySFX(SFXType.Global_ButtonClick);
        if (soundEnabled)
        {
            previousMaster = masterSlider.value;
            previousBGM = bgmSlider.value;
            previousSFX = sfxSlider.value;
            masterSlider.value = bgmSlider.value = sfxSlider.value = 0f;
        }
        else
        {
            masterSlider.value = previousMaster;
            bgmSlider.value = previousBGM;
            sfxSlider.value = previousSFX;
        }
        RefreshVolumeText();
    }

    private void ExitGame()
    {
        audioManager.PlaySFX(SFXType.Global_ButtonClick);
        Application.Quit();
    }

    private void ResetGame()
    {
        if (IsBusy) return;
        SetBusy(true);
        owner.StartCoroutine(ResetGameRoutine());
    }

    private IEnumerator ResetGameRoutine()
    {
        var reset = GameManager.Instance.Utility.World.ResetWorldAsync();
        while (!reset.IsCompleted) yield return null;
        if (!reset.GetAwaiter().GetResult())
        {
            SetBusy(false);
            yield break;
        }
        GameManager.Instance.Utility.Save.ResetSave();
        audioManager.PlaySFX(SFXType.Global_Confirm);
        yield return owner.CloseSettingsRoutine();
        GameManager.Instance.SceneController.ChangeScene(SceneType.Main, true);
    }

    private void SetBusy(bool busy)
    {
        IsBusy = busy;
        closeButton.interactable = !busy;
        backdropButton.interactable = !busy;
        resetButton.interactable = !busy;
        gameExitButton.interactable = !busy;
        toggleButton.interactable = !busy;
        masterSlider.interactable = !busy;
        bgmSlider.interactable = !busy;
        sfxSlider.interactable = !busy;
    }

    private void OnDestroy()
    {
        if (popupTween != null) popupTween.Kill();
    }
}
