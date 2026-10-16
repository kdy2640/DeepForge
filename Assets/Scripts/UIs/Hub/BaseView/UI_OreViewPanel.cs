using UnityEngine;

public sealed class UI_OreViewPanel : MonoBehaviour
{
    [SerializeField] private int[] oreIds = { 1, 2, 3, 4, 5, 6, 7, 8 };
    [SerializeField] private UI_OreViewCard[] cards;

    private StockManager stockManager;
    private bool isSubscribed;

    public void Init()
    {
        stockManager = GameManager.Instance.StockManager;
        stockManager.SubscribeStockDataChange(Refresh);
        isSubscribed = true;
        Refresh();
    }

    public void Refresh()
    {
        for (int i = 0; i < oreIds.Length; i++)
        {
            int oreId = oreIds[i];
            cards[i].SetData(new OreAmount(oreId, stockManager.GetOreAmount(oreId)));
        }
    }

    private void OnDestroy()
    {
        if (isSubscribed)
            stockManager.UnsubscribeStockDataChange(Refresh);
    }
}
