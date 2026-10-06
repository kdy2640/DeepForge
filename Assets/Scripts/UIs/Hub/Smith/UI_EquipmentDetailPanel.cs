using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_EquipmentDetailPanel : MonoBehaviour
{
    [SerializeField] private Text equipmentName;
    [SerializeField] private Text description;
    [SerializeField] private Text[] materialNames;
    [SerializeField] private Text[] materialAmounts;
    [SerializeField] private Button forgeButton;

    private UI_Smith_Equipment equipment;
    private UI_EquipmentVisualPanel visual;

    public void Init(UI_Smith_Equipment owner, UI_EquipmentVisualPanel visualPanel)
    {
        equipment = owner;
        visual = visualPanel;
        equipment.EquipmentSelected += OnEquipmentSelected;
        visual.LevelChanged += Refresh;
        GameManager.Instance.StockManager.SubscribeStockDataChange(Refresh);
        GameManager.Instance.Upgrade.SubscribeUpgradeChanged(Refresh);
        GameManager.Instance.BaseCamp.Smith.SlotsChanged += Refresh;
        forgeButton.onClick.AddListener(Forge);
        Refresh();
    }

    private void OnEquipmentSelected(EquipmentUpgradeDataSO data) => Refresh();

    private void Refresh()
    {
        foreach (Text text in materialNames) text.gameObject.SetActive(false);
        foreach (Text text in materialAmounts) text.gameObject.SetActive(false);
        EquipmentUpgradeDataSO data = equipment.SelectedEquipment;
        if (data == null)
        {
            equipmentName.text = "장비 미선택";
            description.text = "상점에서 장비 청사진을\n구매해 주세요.";
            forgeButton.interactable = false;
            return;
        }
        UpgradeState state = GameManager.Instance.Upgrade.GetState(data);
        bool working = GameManager.Instance.BaseCamp.Smith.IsEquipmentUpgradeInProgress(data.EquipmentId);
        equipmentName.text = data.DisplayName;
        description.text = $"현재 Lv. {state.level} → 목표 Lv. {visual.SelectedLevel}\n"
            + (working ? "제작 중" : visual.SelectedLevel <= state.level ? "현재보다 높은 레벨을 선택하세요." : "제작 시간 · 3초");
        var costs = data.RequiredOres.GroupBy(cost => cost.oreId)
            .Select(group => new OreAmount(group.Key, group.Sum(cost => cost.amount))).ToList();
        for (int i = 0; i < costs.Count; i++)
        {
            OreAmount cost = costs[i];
            int owned = GameManager.Instance.StockManager.GetOreAmount(cost.oreId);
            materialNames[i].gameObject.SetActive(true);
            materialAmounts[i].gameObject.SetActive(true);
            materialNames[i].text = OreDataDB.GetData(cost.oreId).DisplayName;
            materialAmounts[i].text = $"{owned:N0} / {cost.amount:N0}";
            materialAmounts[i].color = owned >= cost.amount ? new Color32(61, 214, 198, 255) : new Color32(255, 138, 50, 255);
        }
        forgeButton.interactable = visual.SelectedLevel > state.level
            && visual.SelectedLevel <= state.unlockedLevel
            && visual.SelectedLevel <= data.MaxLevel
            && !working && GameManager.Instance.BaseCamp.Smith.HasEmptySlot
            && GameManager.Instance.StockManager.CanConsumeOre(costs);
    }

    public void Forge()
    {
        EquipmentUpgradeDataSO data = equipment.SelectedEquipment;
        if (data == null) return;
        GameManager.Instance.BaseCamp.Smith.TryStartEquipmentUpgrade(data.EquipmentId, visual.SelectedLevel);
    }

    public void Dispose()
    {
        equipment.EquipmentSelected -= OnEquipmentSelected;
        visual.LevelChanged -= Refresh;
        GameManager.Instance.StockManager.UnsubscribeStockDataChange(Refresh);
        GameManager.Instance.Upgrade.UnsubscribeUpgradeChanged(Refresh);
        GameManager.Instance.BaseCamp.Smith.SlotsChanged -= Refresh;
    }
}
