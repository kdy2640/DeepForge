using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_OrderVisualCard : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler
{
    [SerializeField] private Button button;

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (button.IsInteractable())
            GameManager.Instance.Utility.Audio.PlaySFX(SFXType.Global_ButtonHover);
    }
    [SerializeField] private Image icon;
    [SerializeField] private Text titleText;
    [SerializeField] private Text nameText;
    [SerializeField] private Text rewardText;
    [SerializeField] private Text statusText;
    [SerializeField] private Outline selectionOutline;

    public void SetData(Order order, int matchingCount, bool completed, Action select)
    {
        int gearId = order.Conditions.OfType<GearOrderCondition>().Single().GearId;
        titleText.text = $"주문서 {order.Index + 1:00}";
        nameText.text = $"{ForgedGearDB.GetData(gearId).name} × {order.RequiredCount}";
        icon.sprite = UpgradeDataDB.GetForgedGearUpgrade(gearId).DisplayIcon;
        rewardText.text = completed ? "납품 완료" : $"판매가의 {order.RewardMultiplier:P0}";
        statusText.text = completed ? "납품 완료" : matchingCount >= order.RequiredCount ? "납품 가능" : $"보유 {matchingCount}/{order.RequiredCount}";
        statusText.color = completed ? new Color32(185, 179, 169, 255) : matchingCount >= order.RequiredCount ? new Color32(61, 214, 198, 255) : new Color32(242, 238, 229, 255);
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
