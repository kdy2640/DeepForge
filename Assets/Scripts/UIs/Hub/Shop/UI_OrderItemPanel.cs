using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_OrderItemPanel : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    [SerializeField] private UI_OrderItemVisualCard cardPrefab;
    [SerializeField] private Button deliverButton;
    [SerializeField] private Text rewardText;
    [SerializeField] private Text countText;
    [SerializeField] private Text emptyMessage;

    private UI_Shop_Order shop;
    private readonly List<ForgedGear> selectedGears = new();
    private readonly List<UI_OrderItemVisualCard> cards = new();

    public IReadOnlyList<ForgedGear> SelectedGears => selectedGears;
    public event Action SelectionChanged;
    public event Action DeliveryCompleted;

    public void Init(UI_Shop_Order owner)
    {
        shop = owner;
        shop.OrderSelected += SelectOrder;
        deliverButton.onClick.AddListener(Deliver);
        GameManager.Instance.StockManager.SubscribeStockDataChange(RefreshStock);
        GameManager.Instance.BaseCamp.Order.OrdersChanged += RefreshStock;
        RefreshSelection();
    }

    private void SelectOrder(Order order)
    {
        selectedGears.Clear();
        if (order != null && !GameManager.Instance.BaseCamp.Order.IsCompleted(order.Index))
        {
            foreach (ForgedGear gear in GameManager.Instance.StockManager.StockData.ForgedGears)
            {
                if (selectedGears.Count == order.RequiredCount) break;
                if (order.Conditions.All(condition => condition.IsSatisfied(gear)))
                    selectedGears.Add(gear);
            }
        }
        RefreshSelection();
    }

    public bool CanSelect(ForgedGear gear)
    {
        Order order = shop.SelectedOrder;
        return order != null && !GameManager.Instance.BaseCamp.Order.IsCompleted(order.Index)
            && selectedGears.Count < order.RequiredCount && !selectedGears.Contains(gear)
            && GameManager.Instance.StockManager.StockData.ForgedGears.Contains(gear)
            && order.Conditions.All(condition => condition.IsSatisfied(gear));
    }

    public void AddGear(ForgedGear gear)
    {
        if (!CanSelect(gear)) return;
        selectedGears.Add(gear);
        RefreshSelection();
    }

    public void RemoveGear(ForgedGear gear)
    {
        if (!selectedGears.Remove(gear)) return;
        RefreshSelection();
    }

    private void RefreshStock()
    {
        Order order = shop.SelectedOrder;
        if (order == null || GameManager.Instance.BaseCamp.Order.IsCompleted(order.Index))
            selectedGears.Clear();
        else
            selectedGears.RemoveAll(gear => !GameManager.Instance.StockManager.StockData.ForgedGears.Contains(gear)
                || !order.Conditions.All(condition => condition.IsSatisfied(gear)));
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        foreach (UI_OrderItemVisualCard card in cards)
        {
            card.gameObject.SetActive(false);
            Destroy(card.gameObject);
        }
        cards.Clear();
        foreach (ForgedGear gear in selectedGears)
        {
            UI_OrderItemVisualCard card = Instantiate(cardPrefab, content, false);
            card.SetData(gear, () => RemoveGear(gear));
            cards.Add(card);
        }

        Order order = shop.SelectedOrder;
        bool completed = order != null && GameManager.Instance.BaseCamp.Order.IsCompleted(order.Index);
        rewardText.text = order == null ? "— G" : $"{order.RewardCurrency:N0} G";
        countText.text = order == null ? "주문 미선택" : completed ? "납품 완료" : $"선택 {selectedGears.Count}/{order.RequiredCount}";
        emptyMessage.gameObject.SetActive(selectedGears.Count == 0);
        emptyMessage.text = order == null ? "주문서를 선택해 주세요" : completed ? "납품을 완료한 주문입니다" : "조건에 맞는 제품을 아래에서 선택해 주세요";
        deliverButton.interactable = order != null && !completed && order.IsSatisfied(selectedGears);
        SelectionChanged?.Invoke();
    }

    public void Deliver()
    {
        Order order = shop.SelectedOrder;
        if (order == null || !GameManager.Instance.BaseCamp.Order.TryComplete(order.Index, selectedGears)) return;
        DeliveryCompleted?.Invoke();
    }

    public void Dispose()
    {
        shop.OrderSelected -= SelectOrder;
        GameManager.Instance.StockManager.UnsubscribeStockDataChange(RefreshStock);
        GameManager.Instance.BaseCamp.Order.OrdersChanged -= RefreshStock;
    }
}
