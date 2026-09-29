using UnityEngine;

public sealed class BaseCampManager : MonoBehaviour
{
    public OrderManager Order { get; private set; }
    public BluePrintManager BluePrint { get; private set; }

    private void Awake()
    {
        Order = GetComponentInChildren<OrderManager>(true);
        BluePrint = GetComponentInChildren<BluePrintManager>(true);
    }
}
