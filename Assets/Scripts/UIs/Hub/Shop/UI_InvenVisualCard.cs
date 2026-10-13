using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_InvenVisualCard : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private Text nameText;
    [SerializeField] private Text amountText;
    [SerializeField] private Outline selectionOutline;
    private ForgedGear gear;

    public void SetData(OreDataSO data, int amount, Action select)
    {
        if (gear != null) gear.IconChanged -= RefreshIcon;
        gear = null;
        icon.sprite = data.Icon;
        icon.color = data.Color;
        nameText.text = data.DisplayName;
        amountText.text = amount.ToString("N0");
        button.interactable = amount > 0;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => select.Invoke());
    }

    public void SetData(ForgedGear data, Action select)
    {
        if (gear != null) gear.IconChanged -= RefreshIcon;
        gear = data;
        if (isActiveAndEnabled) gear.IconChanged += RefreshIcon;
        RefreshIcon();
        GameManager.Instance.GetComponentInChildren<ForgedGearIconBaker>().Request(gear);
        nameText.text = ForgedGearDB.GetData(data.id).name;
        amountText.text = "× 1";
        button.interactable = true;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => select.Invoke());
    }

    public void SetSelected(bool selected)
    {
        selectionOutline.enabled = selected;
    }

    private void RefreshIcon()
    {
        icon.sprite = gear.Icon != null ? gear.Icon : UpgradeDataDB.GetForgedGearUpgrade(gear.id).DisplayIcon;
        icon.color = Color.white;
        icon.preserveAspect = true;
    }

    private void OnEnable()
    {
        if (gear == null) return;
        gear.IconChanged += RefreshIcon;
        RefreshIcon();
    }

    private void OnDisable()
    {
        if (gear != null) gear.IconChanged -= RefreshIcon;
    }
}
