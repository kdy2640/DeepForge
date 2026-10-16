using UnityEngine;
using UnityEngine.UI;

public sealed class UI_OptionPanel : MonoBehaviour
{
    [SerializeField] private Text currencyText;
    [SerializeField] private Button backButton;

    public void Init(System.Action close)
    {
        backButton.onClick.AddListener(() => close.Invoke());
        GameManager.Instance.StockManager.SubscribeStockDataChange(RefreshCurrency);
        RefreshCurrency();
    }

    private void RefreshCurrency()
    {
        currencyText.text = $"{GameManager.Instance.StockManager.StockData.Currency:N0} G";
    }

    public void Dispose()
    {
        GameManager.Instance.StockManager.UnsubscribeStockDataChange(RefreshCurrency);
    }
}
