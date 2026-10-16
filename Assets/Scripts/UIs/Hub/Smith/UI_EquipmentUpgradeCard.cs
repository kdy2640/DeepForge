using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_EquipmentUpgradeCard : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler
{
    [SerializeField] private Button button;

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (button.IsInteractable())
            GameManager.Instance.Utility.Audio.PlaySFX(SFXType.Global_ButtonHover);
    }
    [SerializeField] private Image icon;
    [SerializeField] private Text nameText;
    [SerializeField] private Outline selectionOutline;

    public void SetData(EquipmentUpgradeDataSO equipment, int currentLevel, Action select)
    {
        icon.sprite = equipment.DisplayIcon;
        if (equipment.EquipmentId == (int)EquipmentType.Pickaxe || equipment.EquipmentId == (int)EquipmentType.Boots
            || equipment.EquipmentId == (int)EquipmentType.ReturnRope) currentLevel++;
        nameText.text = $"{equipment.DisplayName}\nLv. {currentLevel}";
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            if (!selectionOutline.enabled)
                GameManager.Instance.Utility.Audio.PlaySFX(SFXType.Hub_Select);
            select.Invoke();
        });
    }

    public void SetSelected(bool selected) => selectionOutline.enabled = selected;
}
