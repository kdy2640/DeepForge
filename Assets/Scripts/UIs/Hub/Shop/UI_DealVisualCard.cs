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

    public void SetData(Sprite sprite, Color color, int amount, long price, Action remove)
    {
        icon.sprite = sprite;
        icon.color = color;
        amountText.text = $"× {amount:N0}";
        priceText.text = $"{price:N0} G";
        removeButton.onClick.RemoveAllListeners();
        removeButton.onClick.AddListener(() => remove.Invoke());
        returnButton.onClick.RemoveAllListeners();
        returnButton.interactable = false;
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
        visual.DOAnchorPos(Vector2.zero, 0.25f).SetEase(Ease.OutCubic);
    }

    private void OnDisable()
    {
        visual.DOKill();
        visual.anchoredPosition = Vector2.zero;
    }
}
