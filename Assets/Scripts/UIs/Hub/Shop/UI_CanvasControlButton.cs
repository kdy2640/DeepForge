using UnityEngine;
using UnityEngine.UI;

public sealed class UI_CanvasControlButton : MonoBehaviour
{
    [SerializeField] private Button orderButton;

    public void Init(UI_Shop_Deal owner)
    {
        orderButton.onClick.AddListener(owner.OpenOrders);
    }
}
