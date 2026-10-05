using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class OrderManager : MonoBehaviour
{
    private const int OrderCount = 5;

    private readonly List<Order> orders = new();
    private readonly HashSet<int> completedOrderIndices = new();

    public int Seed { get; private set; }
    public IReadOnlyList<Order> Orders => orders;
    public event Action OrdersChanged;

    public void GenerateOrders(int seed)
    {
        GenerateOrderList(seed);
        completedOrderIndices.Clear();
        OrdersChanged?.Invoke();
    }

    public bool IsCompleted(int orderIndex)
    {
        return completedOrderIndices.Contains(orderIndex);
    }

    public bool TryComplete(int orderIndex, IReadOnlyList<ForgedGear> forgedGears)
    {
        if (orderIndex < 0 || orderIndex >= orders.Count || IsCompleted(orderIndex))
            return false;

        Order order = orders[orderIndex];
        if (!order.IsSatisfied(forgedGears))
            return false;

        // 재고 변경 구독자가 다시 납품해도 같은 주문을 두 번 완료할 수 없게 한다.
        completedOrderIndices.Add(orderIndex);
        if (!GameManager.Instance.StockManager.TryDeliverForgedGears(forgedGears, order.RewardCurrency))
        {
            completedOrderIndices.Remove(orderIndex);
            return false;
        }

        OrdersChanged?.Invoke();
        return true;
    }

    public OrderSaveData CreateOrderSaveData()
    {
        List<int> completedIndices = new(completedOrderIndices);
        completedIndices.Sort();
        return new OrderSaveData
        {
            seed = Seed,
            completedOrderIndices = completedIndices
        };
    }

    public void LoadOrderSaveData(OrderSaveData saveData)
    {
        // 주문 데이터가 없던 기존 세이브는 첫 주문 묶음을 생성한다.
        if (saveData == null)
        {
            ResetOrderSaveData();
            return;
        }

        GenerateOrderList(saveData.seed);
        completedOrderIndices.Clear();
        foreach (int index in saveData.completedOrderIndices)
            completedOrderIndices.Add(index);
        OrdersChanged?.Invoke();
    }

    public void ResetOrderSaveData()
    {
        GenerateOrders(Guid.NewGuid().GetHashCode());
    }

    private void GenerateOrderList(int seed)
    {
        Seed = seed;
        orders.Clear();

        System.Random random = new(seed);
        ForgedGearSO[] gears = Resources.LoadAll<ForgedGearSO>("SOs/ForgedGearDatas");
        OreDataSO[] ores = Resources.LoadAll<OreDataSO>("SOs/OreDatas");
        Array.Sort(gears, (left, right) => left.Id.CompareTo(right.Id));
        Array.Sort(ores, (left, right) => left.Id.CompareTo(right.Id));

        for (int index = 0; index < OrderCount; index++)
        {
            ForgedGearSO gear = gears[random.Next(gears.Length)];
            int requiredCount = random.Next(1, 4);
            List<OrderCondition> conditions = new() { new GearOrderCondition(gear.Id) };

            if (random.Next(2) == 1)
                conditions.Add(new HandleOreOrderCondition(ores[random.Next(ores.Length)].Id));
            if (random.Next(2) == 1)
                conditions.Add(new MetalOreOrderCondition(ores[random.Next(ores.Length)].Id));
            if (random.Next(2) == 1)
            {
                bool hasGem = random.Next(2) == 1;
                int gemOreId = hasGem ? ores[random.Next(ores.Length)].Id : 0;
                conditions.Add(new GemOrderCondition(hasGem, gemOreId));
            }

            int rewardCurrency = (int)Math.Min((long)gear.SellPrice * requiredCount * 2, int.MaxValue);
            orders.Add(new Order(index, requiredCount, rewardCurrency, conditions));
        }
    }
}
