using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_DealVisualCard : MonoBehaviour
{
    [SerializeField] private RectTransform visual;
    [SerializeField] private Image icon;
    [SerializeField] private Text amountText;
    [SerializeField] private Text priceText;
    [SerializeField] private Button removeButton;
    [SerializeField] private Button returnButton;
    private ForgedGear gear;

    public void SetData(Sprite sprite, Color color, int amount, long price, Action remove)
    {
        if (gear != null) gear.IconChanged -= RefreshIcon;
        gear = null;
        icon.sprite = sprite;
        icon.color = color;
        amountText.text = $"× {amount:N0}";
        priceText.text = $"{price:N0} G";
        removeButton.onClick.RemoveAllListeners();
        removeButton.onClick.AddListener(() => remove.Invoke());
        returnButton.onClick.RemoveAllListeners();
        returnButton.interactable = false;
    }

    public void SetData(ForgedGear data, long price, Action remove)
    {
        SetData(data.Icon, Color.white, 1, price, remove);
        gear = data;
        if (isActiveAndEnabled) gear.IconChanged += RefreshIcon;
        RefreshIcon();
        GameManager.Instance.GetComponentInChildren<ForgedGearIconBaker>().Request(gear);
    }

    private void RefreshIcon()
    {
        icon.sprite = gear.Icon != null ? gear.Icon : UpgradeDataDB.GetForgedGearUpgrade(gear.id).DisplayIcon;
        icon.preserveAspect = true;
    }

    public void SetReturnAction(Action returnAmount)
    {
        returnButton.interactable = true;
        returnButton.onClick.AddListener(() => returnAmount.Invoke());
    }

    public void Animate(bool buying)
    {
        visual.DOKill();
        visual.anchoredPosition = new Vector2(0, buying ? 48 : -96);
        visual.DOAnchorPos(Vector2.zero, 0.25f).SetEase(Ease.OutCubic).SetRecyclable(true);
    }

    private void OnDisable()
    {
        if (gear != null) gear.IconChanged -= RefreshIcon;
        visual.DOKill();
        visual.anchoredPosition = Vector2.zero;
    }

    private void OnEnable()
    {
        if (gear == null) return;
        gear.IconChanged += RefreshIcon;
        RefreshIcon();
    }
}
