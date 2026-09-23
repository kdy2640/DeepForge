using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

// 비활성 상태로 대여하고 호출 측에서 배치와 초기화를 끝낸 뒤 활성화한다.
public class Pooler<T> : IDisposable where T : Poolable
{
    private readonly T prefab;
    private readonly Transform container;
    private readonly Queue<T> available = new Queue<T>();
    private readonly List<T> items = new List<T>();

    public int CountAll => items.Count;
    public int CountInactive => available.Count;

    public Pooler(T prefab, Transform parent)
    {
        this.prefab = prefab;
        GameObject root = new GameObject($"[Pool] {typeof(T).Name}");
        root.SetActive(false);
        root.transform.SetParent(parent, false);
        container = root.transform;
    }

    public T Get()
    {
        T item = available.Count > 0 ? available.Dequeue() : Create();
        item.BeginRental();
        return item;
    }

    // count는 대여 중인 인스턴스를 포함한 총 보유량의 목표치다.
    public void Prewarm(int count)
    {
        while (items.Count < count)
            available.Enqueue(Create());
    }

    public IEnumerator Prewarm(int count, int perFrame)
    {
        int batchSize = Mathf.Max(1, perFrame);
        while (items.Count < count)
        {
            Prewarm(Mathf.Min(count, items.Count + batchSize));
            yield return null;
        }
    }

    private T Create()
    {
        T item = Object.Instantiate(prefab, container);
        item.gameObject.SetActive(false);
        item.BindPool(Return);
        item.InitializePoolItem();
        items.Add(item);
        return item;
    }

    private void Return(Poolable pooledItem)
    {
        T item = (T)pooledItem;
        item.ResetState();
        item.gameObject.SetActive(false);
        item.transform.SetParent(container, false);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        item.transform.localScale = prefab.transform.localScale;
        available.Enqueue(item);
    }

    public void Dispose()
    {
        // 씬 종료 시 부모와 함께 먼저 파괴된 인스턴스는 건너뛴다.
        foreach (T item in items)
        {
            if (item == null) continue;
            item.DetachPool();
            item.gameObject.SetActive(false);
            if (Application.isPlaying) Object.Destroy(item.gameObject);
            else Object.DestroyImmediate(item.gameObject);
        }
        items.Clear();
        available.Clear();
        if (container != null)
        {
            if (Application.isPlaying) Object.Destroy(container.gameObject);
            else Object.DestroyImmediate(container.gameObject);
        }
    }
}
