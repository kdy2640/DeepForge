using UnityEngine;
using UnityEngine.UI;
using System;

public sealed class UI_BluePrintCard : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler
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

    public void SetData(ForgedGearUpgradeDataSO blueprint, Action select)
    {
        icon.sprite = blueprint.DisplayIcon;
        nameText.text = blueprint.DisplayName;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            if (!selectionOutline.enabled)
                GameManager.Instance.Utility.Audio.PlaySFX(SFXType.Hub_Select);
            select.Invoke();
        });
    }

    public void SetSelected(bool selected)
    {
        selectionOutline.enabled = selected;
    }
}
