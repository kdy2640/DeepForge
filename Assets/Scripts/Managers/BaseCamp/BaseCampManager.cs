using UnityEngine;

public sealed class BaseCampManager : MonoBehaviour
{
    public OrderManager Order { get; private set; }
    public SmithManager Smith { get; private set; }

    private void Awake()
    {
        Order = GetComponentInChildren<OrderManager>(true);
        Smith = GetComponentInChildren<SmithManager>(true);
    }
}
