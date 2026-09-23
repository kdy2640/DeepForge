using UnityEngine;

public sealed class StonePooler : Pooler<StoneActor>
{
    public StonePooler(StoneActor prefab, Transform parent) : base(prefab, parent)
    {
    }
}
