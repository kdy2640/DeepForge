using TMPro;
using UnityEngine;

public sealed class UI_GoldElement : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;
    private StockManager stock;
    private bool subscribed;

    private void Start()
    {
        stock = GameManager.Instance.StockManager;
        stock.SubscribeStockDataChange(UpdateGold);
        subscribed = true;
        UpdateGold();
    }

    private void UpdateGold()
    {
        goldText.text = $"{stock.StockData.Currency:N0} G";
    }

    private void OnDestroy()
    {
        if (subscribed) stock.UnsubscribeStockDataChange(UpdateGold);
    }
}
