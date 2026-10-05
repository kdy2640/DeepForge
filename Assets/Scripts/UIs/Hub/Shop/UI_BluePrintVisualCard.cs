using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_BluePrintVisualCard : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private Text nameText;
    [SerializeField] private Text priceText;
    [SerializeField] private Outline selectionOutline;

    public void SetData(UpgradeDataSO data, UpgradeAvailability availability, int unlockedLevel, Action select)
    {
        bool owned = availability == UpgradeAvailability.BlueprintAlreadyOwned || availability == UpgradeAvailability.MaxLevel;
        nameText.text = data.MaxLevel > 1 ? $"{data.DisplayName} · {unlockedLevel}/{data.MaxLevel}"
            : owned ? $"{data.DisplayName} · 보유" : data.DisplayName;
        priceText.text = $"{data.BlueprintPrice:N0} G";
        icon.sprite = data.DisplayIcon;
        icon.color = new Color32(185, 179, 169, 255);
        button.interactable = availability == UpgradeAvailability.Available || availability == UpgradeAvailability.InsufficientCurrency;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => select.Invoke());
    }

    public void SetSelected(bool selected)
    {
        selectionOutline.enabled = selected;
    }
}
