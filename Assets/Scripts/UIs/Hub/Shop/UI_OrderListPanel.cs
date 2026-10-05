using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_OrderListPanel : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    [SerializeField] private UI_OrderVisualCard cardPrefab;
    [SerializeField] private Button refreshButton;

    private UI_Shop_Order shop;
    private readonly Dictionary<Order, UI_OrderVisualCard> cards = new();

    public void Init(UI_Shop_Order owner)
    {
        shop = owner;
        refreshButton.onClick.AddListener(RefreshOrders);
        shop.OrderSelected += RefreshSelection;
        GameManager.Instance.BaseCamp.Order.OrdersChanged += Rebuild;
        GameManager.Instance.StockManager.SubscribeStockDataChange(RefreshAvailability);
        Rebuild();
    }

    private void RefreshOrders()
    {
        shop.SelectOrder(null);
        GameManager.Instance.BaseCamp.Order.GenerateOrders(Guid.NewGuid().GetHashCode());
    }

    private void Rebuild()
    {
        foreach (UI_OrderVisualCard card in cards.Values)
        {
            card.gameObject.SetActive(false);
            Destroy(card.gameObject);
        }
        cards.Clear();
        foreach (Order order in GameManager.Instance.BaseCamp.Order.Orders)
            cards.Add(order, Instantiate(cardPrefab, content, false));
        RefreshAvailability();
    }

    private void RefreshAvailability()
    {
        foreach (var entry in cards)
        {
            Order order = entry.Key;
            int matchingCount = GameManager.Instance.StockManager.StockData.ForgedGears.Count(
                gear => order.Conditions.All(condition => condition.IsSatisfied(gear)));
            entry.Value.SetData(order, matchingCount, GameManager.Instance.BaseCamp.Order.IsCompleted(order.Index),
                () => shop.SelectOrder(order));
            entry.Value.SetSelected(order == shop.SelectedOrder);
        }
    }

    private void RefreshSelection(Order order)
    {
        foreach (var entry in cards)
            entry.Value.SetSelected(entry.Key == order);
    }

    public void Dispose()
    {
        shop.OrderSelected -= RefreshSelection;
        GameManager.Instance.BaseCamp.Order.OrdersChanged -= Rebuild;
        GameManager.Instance.StockManager.UnsubscribeStockDataChange(RefreshAvailability);
    }
}
