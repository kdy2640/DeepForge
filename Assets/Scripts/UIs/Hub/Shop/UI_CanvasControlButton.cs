using UnityEngine;
using UnityEngine.UI;

public sealed class UI_CanvasControlButton : MonoBehaviour
{
    [SerializeField] private Button orderButton;

    public void Init(System.Action navigate)
    {
        orderButton.onClick.AddListener(() => navigate.Invoke());
    }
}
