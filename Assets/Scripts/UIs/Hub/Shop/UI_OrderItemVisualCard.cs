using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_OrderItemVisualCard : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Text nameText;
    [SerializeField] private Text amountText;
    [SerializeField] private Button removeButton;

    public void SetData(ForgedGear gear, Action remove)
    {
        icon.sprite = UpgradeDataDB.GetForgedGearUpgrade(gear.id).DisplayIcon;
        icon.color = OreDataDB.GetData(gear.data.metalOreId).Color;
        nameText.text = ForgedGearDB.GetData(gear.id).name;
        amountText.text = "× 1";
        removeButton.onClick.RemoveAllListeners();
        removeButton.onClick.AddListener(() => remove.Invoke());
    }
}
