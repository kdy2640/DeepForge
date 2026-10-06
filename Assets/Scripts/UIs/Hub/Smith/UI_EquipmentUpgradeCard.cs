using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_EquipmentUpgradeCard : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private Text nameText;
    [SerializeField] private Outline selectionOutline;

    public void SetData(EquipmentUpgradeDataSO equipment, int currentLevel, Action select)
    {
        icon.sprite = equipment.DisplayIcon;
        nameText.text = $"{equipment.DisplayName}\nLv. {currentLevel}";
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => select.Invoke());
    }

    public void SetSelected(bool selected) => selectionOutline.enabled = selected;
}
