using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_ForgedGearVisualPanel : MonoBehaviour
{
    [SerializeField] private Button levelUpButton;
    [SerializeField] private Button levelDownButton;
    [SerializeField] private Text levelText;

    private UI_Smith_Forge forge;
    public int SelectedLevel { get; private set; } = 1;
    public event Action LevelChanged;

    public void Init(UI_Smith_Forge owner)
    {
        forge = owner;
        forge.BlueprintSelected += OnBlueprintSelected;
        GameManager.Instance.Upgrade.SubscribeUpgradeChanged(Refresh);
        levelUpButton.onClick.AddListener(() => SelectLevel(SelectedLevel + 1));
        levelDownButton.onClick.AddListener(() => SelectLevel(SelectedLevel - 1));
        Refresh();
    }

    private void OnBlueprintSelected(ForgedGearUpgradeDataSO blueprint) => Refresh();

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
        if (SelectedLevel != previousLevel) LevelChanged?.Invoke();
    }

    public void Dispose()
    {
        forge.BlueprintSelected -= OnBlueprintSelected;
        GameManager.Instance.Upgrade.UnsubscribeUpgradeChanged(Refresh);
    }
}
