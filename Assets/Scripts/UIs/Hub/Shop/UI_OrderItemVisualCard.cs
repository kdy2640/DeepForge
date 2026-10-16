using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_OrderItemVisualCard : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Text nameText;
    [SerializeField] private Text amountText;
    [SerializeField] private Button removeButton;
    private ForgedGear displayedGear;

    public void SetData(ForgedGear gear, Action remove)
    {
        if (displayedGear != null) displayedGear.IconChanged -= RefreshIcon;
        displayedGear = gear;
        if (isActiveAndEnabled) displayedGear.IconChanged += RefreshIcon;
        RefreshIcon();
        GameManager.Instance.GetComponentInChildren<ForgedGearIconBaker>().Request(gear);
        nameText.text = ForgedGearDB.GetData(gear.id).name;
        amountText.text = "× 1";
        removeButton.onClick.RemoveAllListeners();
        removeButton.onClick.AddListener(() => remove.Invoke());
    }

    private void RefreshIcon()
    {
        icon.sprite = displayedGear.Icon != null ? displayedGear.Icon : UpgradeDataDB.GetForgedGearUpgrade(displayedGear.id).DisplayIcon;
        icon.color = Color.white;
        icon.preserveAspect = true;
    }

    private void OnEnable()
    {
        if (displayedGear == null) return;
        displayedGear.IconChanged += RefreshIcon;
        RefreshIcon();
    }

    private void OnDisable()
    {
        if (displayedGear != null) displayedGear.IconChanged -= RefreshIcon;
    }
}
