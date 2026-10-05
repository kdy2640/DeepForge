using UnityEngine;

public sealed class UI_SmithInfoPanel : MonoBehaviour
{
    [SerializeField] private UI_QueueSlotCard[] cards;

    public void Init()
    {
        GameManager.Instance.BaseCamp.Smith.SlotsChanged += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        for (int i = 0; i < cards.Length; i++)
            cards[i].SetData(GameManager.Instance.BaseCamp.Smith.Slots[i]);
    }

    public void Dispose()
    {
        GameManager.Instance.BaseCamp.Smith.SlotsChanged -= Refresh;
    }
}
