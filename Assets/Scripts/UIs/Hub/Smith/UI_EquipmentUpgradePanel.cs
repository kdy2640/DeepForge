using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_EquipmentUpgradePanel : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    [SerializeField] private UI_EquipmentUpgradeCard cardPrefab;
    [SerializeField] private EquipmentUpgradeDataSO[] upgrades;
    [SerializeField] private Text emptyMessage;

    private UI_Smith_Equipment equipment;
    private readonly Dictionary<EquipmentUpgradeDataSO, UI_EquipmentUpgradeCard> cards = new();

    public void Init(UI_Smith_Equipment owner)
    {
        equipment = owner;
        equipment.EquipmentSelected += RefreshSelection;
        GameManager.Instance.Upgrade.SubscribeUpgradeChanged(Refresh);
        Refresh();
    }

    public void Refresh()
    {
        foreach (UI_EquipmentUpgradeCard card in cards.Values)
        {
            card.gameObject.SetActive(false);
            Destroy(card.gameObject);
        }
        cards.Clear();
        foreach (EquipmentUpgradeDataSO data in upgrades.OrderBy(data => data.EquipmentId))
        {
            if (!GameManager.Instance.Upgrade.HasState(data)
                || GameManager.Instance.Upgrade.GetState(data).unlockedLevel <= 0) continue;
            UI_EquipmentUpgradeCard card = Instantiate(cardPrefab, content, false);
            card.SetData(data, GameManager.Instance.Upgrade.GetState(data).level, () => equipment.SelectEquipment(data));
            cards.Add(data, card);
        }
        emptyMessage.gameObject.SetActive(cards.Count == 0);
        if (equipment.SelectedEquipment == null || !cards.ContainsKey(equipment.SelectedEquipment))
            equipment.SelectEquipment(cards.Keys.FirstOrDefault());
        else
            RefreshSelection(equipment.SelectedEquipment);
    }

    private void RefreshSelection(EquipmentUpgradeDataSO data)
    {
        foreach (var entry in cards)
            entry.Value.SetSelected(entry.Key == data);
    }

    public void Dispose()
    {
        equipment.EquipmentSelected -= RefreshSelection;
        GameManager.Instance.Upgrade.UnsubscribeUpgradeChanged(Refresh);
    }
}
