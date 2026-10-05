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

    public void SetData(OreDataSO data, int amount, Action select)
    {
        icon.sprite = data.Icon;
        icon.color = data.Color;
        nameText.text = data.DisplayName;
        amountText.text = amount.ToString("N0");
        button.interactable = amount > 0;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => select.Invoke());
    }

    public void SetData(ForgedGearSO data, Action select)
    {
        icon.sprite = null;
        icon.color = new Color32(185, 179, 169, 255);
        nameText.text = data.name;
        amountText.text = "× 1";
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => select.Invoke());
    }

    public void SetSelected(bool selected)
    {
        selectionOutline.enabled = selected;
    }
}
