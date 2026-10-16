using UnityEngine;
using UnityEngine.UI;

public sealed class UI_MerchantVisualizer : MonoBehaviour
{
    [SerializeField] private Text merchantText;
    private UI_Shop_Deal shop;
    private UI_DealPanel deal;

    public void Init(UI_Shop_Deal owner, UI_DealPanel dealPanel)
    {
        shop = owner;
        deal = dealPanel;
        shop.DealModeChanged += OnDealModeChanged;
        deal.TradeCompleted += SetDialogue;
        OnDealModeChanged(shop.CurrentDealMode);
    }

    private void OnDealModeChanged(UI_Shop_Deal.DealMode mode)
    {
        merchantText.text = mode switch
        {
            UI_Shop_Deal.DealMode.Buy => "구매할 설계도를\n매대에 올려보게.",
            UI_Shop_Deal.DealMode.Sell => "판매할 물건을\n매대에 올려보게.",
            _ => "필요한 물건을 골라\n매대에 올려보게."
        };
    }

    private void SetDialogue(string dialogue)
    {
        merchantText.text = dialogue;
    }

    public void Dispose()
    {
        shop.DealModeChanged -= OnDealModeChanged;
        deal.TradeCompleted -= SetDialogue;
    }
}