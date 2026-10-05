using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_OrderVisualCard : MonoBehaviour
{
    [SerializeField] private Button button;
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
        rewardText.text = $"보상 {order.RewardCurrency:N0} G";
        statusText.text = completed ? "납품 완료" : matchingCount >= order.RequiredCount ? "납품 가능" : $"보유 {matchingCount}/{order.RequiredCount}";
        statusText.color = completed ? new Color32(185, 179, 169, 255) : matchingCount >= order.RequiredCount ? new Color32(61, 214, 198, 255) : new Color32(242, 238, 229, 255);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => select.Invoke());
    }

    public void SetSelected(bool selected)
    {
        selectionOutline.enabled = selected;
    }
}
