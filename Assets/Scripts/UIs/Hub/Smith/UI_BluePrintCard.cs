using UnityEngine;
using UnityEngine.UI;
using System;

public sealed class UI_BluePrintCard : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private Text nameText;
    [SerializeField] private Outline selectionOutline;

    public void SetData(ForgedGearUpgradeDataSO blueprint, Action select)
    {
        icon.sprite = blueprint.DisplayIcon;
        nameText.text = blueprint.DisplayName;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => select.Invoke());
    }

    public void SetSelected(bool selected)
    {
        selectionOutline.enabled = selected;
    }
}
