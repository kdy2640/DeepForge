using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_EquipmentVisualPanel : MonoBehaviour
{
    [SerializeField] private Button levelUpButton;
    [SerializeField] private Button levelDownButton;
    [SerializeField] private Text levelText;
    [SerializeField] private Image previewImage;

    private UI_Smith_Equipment equipment;
    public int SelectedLevel { get; private set; } = 1;
    public event Action LevelChanged;

    public void Init(UI_Smith_Equipment owner)
    {
        equipment = owner;
        equipment.EquipmentSelected += OnEquipmentSelected;
        GameManager.Instance.Upgrade.SubscribeUpgradeChanged(Refresh);
        levelUpButton.onClick.AddListener(() =>
        {
            int previousLevel = SelectedLevel;
            SelectLevel(SelectedLevel + 1);
            if (SelectedLevel != previousLevel)
                GameManager.Instance.Utility.Audio.PlaySFX(SFXType.Hub_Select);
        });
        levelDownButton.onClick.AddListener(() =>
        {
            int previousLevel = SelectedLevel;
            SelectLevel(SelectedLevel - 1);
            if (SelectedLevel != previousLevel)
                GameManager.Instance.Utility.Audio.PlaySFX(SFXType.Hub_Select);
        });
        Refresh();
    }

    private void OnEquipmentSelected(EquipmentUpgradeDataSO data)
    {
        SelectLevel(data != null ? GameManager.Instance.Upgrade.GetState(data).level + 1 : 1);
    }

    private void Refresh() => SelectLevel(SelectedLevel);

    private void SelectLevel(int level)
    {
        EquipmentUpgradeDataSO data = equipment.SelectedEquipment;
        int maxLevel = data != null && GameManager.Instance.Upgrade.HasState(data)
            ? Mathf.Min(data.MaxLevel, GameManager.Instance.Upgrade.GetState(data).unlockedLevel)
            : 0;
        int previousLevel = SelectedLevel;
        SelectedLevel = Mathf.Clamp(level, 1, Mathf.Max(1, maxLevel));
        int displayLevel = SelectedLevel + (data != null
            && (data.EquipmentId == (int)EquipmentType.Pickaxe || data.EquipmentId == (int)EquipmentType.Boots
                || data.EquipmentId == (int)EquipmentType.ReturnRope) ? 1 : 0);
        levelText.text = maxLevel > 0 ? $"Lv. {displayLevel}" : "—";
        previewImage.enabled = data != null;
        previewImage.sprite = data != null ? data.GetLevelIcon(maxLevel > 0 ? SelectedLevel : 0) : null;
        levelUpButton.interactable = SelectedLevel < maxLevel;
        levelDownButton.interactable = maxLevel > 0 && SelectedLevel > 1;
        if (SelectedLevel != previousLevel) LevelChanged?.Invoke();
    }

    public void Dispose()
    {
        equipment.EquipmentSelected -= OnEquipmentSelected;
        GameManager.Instance.Upgrade.UnsubscribeUpgradeChanged(Refresh);
    }
}
