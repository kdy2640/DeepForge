using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public sealed class UI_BluePrintListPanel : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    [SerializeField] private UI_BluePrintCard cardPrefab;
    [SerializeField] private ForgedGearUpgradeDataSO[] blueprints;
    [SerializeField] private Text emptyMessage;

    private UI_Smith_Forge forge;
    private readonly Dictionary<ForgedGearUpgradeDataSO, UI_BluePrintCard> cards = new();

    public void Init(UI_Smith_Forge owner)
    {
        forge = owner;
        forge.BlueprintSelected += RefreshSelection;
        GameManager.Instance.Upgrade.SubscribeUpgradeChanged(Refresh);
        Refresh();
    }

    public void Refresh()
    {
        foreach (UI_BluePrintCard card in cards.Values)
        {
            card.gameObject.SetActive(false);
            Destroy(card.gameObject);
        }
        cards.Clear();
        foreach (ForgedGearUpgradeDataSO blueprint in blueprints.OrderBy(data => data.ForgedGearId))
        {
            if (!GameManager.Instance.Upgrade.HasState(blueprint)
                || GameManager.Instance.Upgrade.GetState(blueprint).unlockedLevel <= 0) continue;
            UI_BluePrintCard card = Instantiate(cardPrefab, content, false);
            card.SetData(blueprint, () => forge.SelectBlueprint(blueprint));
            cards.Add(blueprint, card);
        }
        emptyMessage.gameObject.SetActive(cards.Count == 0);
        if (forge.SelectedBlueprint == null || !cards.ContainsKey(forge.SelectedBlueprint))
            forge.SelectBlueprint(cards.Keys.FirstOrDefault());
        else
            RefreshSelection(forge.SelectedBlueprint);
    }

    private void RefreshSelection(ForgedGearUpgradeDataSO blueprint)
    {
        foreach (var entry in cards)
            entry.Value.SetSelected(entry.Key == blueprint);
    }

    public void Dispose()
    {
        forge.BlueprintSelected -= RefreshSelection;
        GameManager.Instance.Upgrade.UnsubscribeUpgradeChanged(Refresh);
    }
}
