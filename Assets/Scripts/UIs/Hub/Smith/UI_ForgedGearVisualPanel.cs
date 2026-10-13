using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_ForgedGearVisualPanel : MonoBehaviour
{
    [SerializeField] private Button levelUpButton;
    [SerializeField] private Button levelDownButton;
    [SerializeField] private Text levelText;
    [SerializeField] private RawImage previewImage;
    [SerializeField] private CanvasGroup previewCover;
    [SerializeField] private float revealDuration = 0.3f;
    [SerializeField] private float coveredScale = 0.9f;

    private UI_Smith_Forge forge;
    private UI_MaterialSelectPanel materials;
    private ForgedGearIconBaker iconBaker;
    private bool previewVisible;
    public int SelectedLevel { get; private set; } = 1;
    public event Action LevelChanged;

    public void Init(UI_Smith_Forge owner, UI_MaterialSelectPanel materialPanel)
    {
        forge = owner;
        materials = materialPanel;
        iconBaker = GameManager.Instance.GetComponentInChildren<ForgedGearIconBaker>();
        forge.BlueprintSelected += OnBlueprintSelected;
        materials.MaterialChanged += OnMaterialChanged;
        GameManager.Instance.Upgrade.SubscribeUpgradeChanged(Refresh);
        levelUpButton.onClick.AddListener(() => SelectLevel(SelectedLevel + 1));
        levelDownButton.onClick.AddListener(() => SelectLevel(SelectedLevel - 1));
        Refresh();
    }

    private void OnBlueprintSelected(ForgedGearUpgradeDataSO blueprint)
    {
        Refresh();
        if (previewVisible) ShowPreview();
    }

    private void OnMaterialChanged(ForgedGearData selected)
    {
        if (previewVisible) ShowPreview();
    }

    public void ShowPreview()
    {
        previewVisible = true;
        DOTween.Kill(this);
        previewCover.gameObject.SetActive(true);
        previewCover.alpha = 1f;
        previewCover.blocksRaycasts = false;
        previewImage.rectTransform.localScale = Vector3.one * coveredScale;
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        if (!previewVisible) return;
        bool hasBlueprint = forge.SelectedBlueprint != null;
        previewImage.enabled = hasBlueprint;
        if (!hasBlueprint)
        {
            previewImage.texture = null;
            iconBaker.ReleasePreview();
            return;
        }

        previewImage.texture = iconBaker.RequestPreview(forge.SelectedBlueprint.ForgedGearId, materials.SelectedMaterials);
    }

    public void RevealPreview()
    {
        ShowPreview();
        DOTween.Sequence()
            .SetTarget(this)
            .SetUpdate(true)
            .Join(previewCover.DOFade(0f, revealDuration).SetEase(Ease.OutQuad))
            .Join(previewImage.rectTransform.DOScale(Vector3.one, revealDuration).SetEase(Ease.OutCubic))
            .OnComplete(() => previewCover.gameObject.SetActive(false));
    }

    public void HidePreview()
    {
        if (!previewVisible) return;
        previewVisible = false;
        DOTween.Kill(this);
        previewImage.texture = null;
        previewImage.enabled = false;
        iconBaker.ReleasePreview();
    }

    private void OnDisable() => HidePreview();

    private void Refresh() => SelectLevel(SelectedLevel);

    private void SelectLevel(int level)
    {
        ForgedGearUpgradeDataSO blueprint = forge.SelectedBlueprint;
        int maxLevel = blueprint != null && GameManager.Instance.Upgrade.HasState(blueprint)
            ? Mathf.Min(blueprint.MaxLevel, GameManager.Instance.Upgrade.GetState(blueprint).unlockedLevel)
            : 0;
        int previousLevel = SelectedLevel;
        SelectedLevel = Mathf.Clamp(level, 1, Mathf.Max(1, maxLevel));
        levelText.text = maxLevel > 0 ? $"Lv. {SelectedLevel}" : "—";
        levelUpButton.interactable = SelectedLevel < maxLevel;
        levelDownButton.interactable = maxLevel > 0 && SelectedLevel > 1;
        if (SelectedLevel != previousLevel)
        {
            if (previewVisible) ShowPreview();
            LevelChanged?.Invoke();
        }
    }

    public void Dispose()
    {
        HidePreview();
        forge.BlueprintSelected -= OnBlueprintSelected;
        materials.MaterialChanged -= OnMaterialChanged;
        GameManager.Instance.Upgrade.UnsubscribeUpgradeChanged(Refresh);
    }
}
