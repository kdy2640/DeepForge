using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_BluePrintVisualCard : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler
{
    [SerializeField] private Button button;

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (button.IsInteractable())
            GameManager.Instance.Utility.Audio.PlaySFX(SFXType.Global_ButtonHover);
    }
    [SerializeField] private Image icon;
    [SerializeField] private Text nameText;
    [SerializeField] private Text priceText;
    [SerializeField] private Outline selectionOutline;

    public void SetData(UpgradeDataSO data, UpgradeAvailability availability, int unlockedLevel, Action select)
    {
        bool owned = availability == UpgradeAvailability.BlueprintAlreadyOwned || availability == UpgradeAvailability.MaxLevel;
        nameText.text = data.MaxLevel > 1 ? $"{data.DisplayName} · {unlockedLevel}/{data.MaxLevel}"
            : owned ? $"{data.DisplayName} · 보유" : data.DisplayName;
        if (data is EquipmentUpgradeDataSO equipment)
        {
            int targetLevel = Mathf.Min(unlockedLevel + 1, equipment.MaxLevel);
            int levelOffset = equipment.EquipmentId == (int)EquipmentType.Pickaxe || equipment.EquipmentId == (int)EquipmentType.Boots
                || equipment.EquipmentId == (int)EquipmentType.ReturnRope ? 1 : 0;
            nameText.text = $"{data.DisplayName} · Lv. {targetLevel + levelOffset}/{data.MaxLevel + levelOffset}";
            icon.sprite = equipment.GetLevelIcon(targetLevel);
        }
        else
            icon.sprite = data.DisplayIcon;
        priceText.text = owned ? "보유" : $"{data.GetLevelCost(unlockedLevel + 1).BlueprintPrice:N0} G";
        icon.color = Color.white;
        button.interactable = availability == UpgradeAvailability.Available || availability == UpgradeAvailability.InsufficientCurrency;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => select.Invoke());
    }

    public void SetSelected(bool selected)
    {
        selectionOutline.enabled = selected;
    }
}
