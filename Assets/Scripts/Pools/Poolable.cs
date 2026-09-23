using System;
using UnityEngine;

// 풀 연결은 최초 생성 때 한 번 설정하고 대여 중일 때만 반납할 수 있다.
public abstract class Poolable : MonoBehaviour
{
    private Action<Poolable> returnToPool;
    public bool IsRented { get; private set; }

    internal void BindPool(Action<Poolable> onReturn)
    {
        returnToPool = onReturn;
    }

    internal void BeginRental()
    {
        IsRented = true;
    }

    internal void DetachPool()
    {
        IsRented = false;
        returnToPool = null;
    }

    public void RequestReturn()
    {
        if (!IsRented) return;
        IsRented = false;
        returnToPool(this);
    }

    public abstract void InitializePoolItem();
    public abstract void ResetState();
}
