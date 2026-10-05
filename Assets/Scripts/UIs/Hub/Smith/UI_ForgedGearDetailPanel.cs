using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public sealed class UI_ForgedGearDetailPanel : MonoBehaviour
{
    [SerializeField] private Text gearName;
    [SerializeField] private Text description;
    [SerializeField] private Text sellPrice;
    [SerializeField] private Text[] materialNames;
    [SerializeField] private Text[] materialAmounts;
    [SerializeField] private Button forgeButton;

    private UI_Smith_Forge forge;
    private UI_MaterialSelectPanel materials;

    public void Init(UI_Smith_Forge owner, UI_MaterialSelectPanel materialPanel)
    {
        forge = owner;
        materials = materialPanel;
        forge.BlueprintSelected += OnBlueprintSelected;
        materials.MaterialChanged += OnMaterialChanged;
        GameManager.Instance.StockManager.SubscribeStockDataChange(Refresh);
        GameManager.Instance.Upgrade.SubscribeUpgradeChanged(Refresh);
        forgeButton.onClick.AddListener(Forge);
        Refresh();
    }

    private void OnBlueprintSelected(ForgedGearUpgradeDataSO blueprint) => Refresh();
    private void OnMaterialChanged(ForgedGearData data) => Refresh();

    private void Refresh()
    {
        foreach (Text text in materialNames) text.gameObject.SetActive(false);
        foreach (Text text in materialAmounts) text.gameObject.SetActive(false);

        ForgedGearUpgradeDataSO blueprint = forge.SelectedBlueprint;
        if (blueprint == null)
        {
            gearName.text = "제품 미선택";
            description.text = "상점에서 제품 청사진을\n구매해 주세요.";
            sellPrice.text = "— G";
            forgeButton.interactable = false;
            return;
        }

        ForgedGearSO product = ForgedGearDB.GetData(blueprint.ForgedGearId);
        ForgedGearData data = materials.SelectedMaterials;
        gearName.text = blueprint.DisplayName;
        sellPrice.text = $"{product.SellPrice:N0} G";
        description.text = $"손잡이  {OreDataDB.GetData(data.handleOreId).DisplayName} × {product.HandleOreCost}\n"
            + $"금속  {OreDataDB.GetData(data.metalOreId).DisplayName} × {product.MetalOreCost}\n"
            + (data.hasGem ? $"보석  {OreDataDB.GetData(data.gemOreId).DisplayName} × {product.GemOreCost}" : "보석  없음");

        List<OreAmount> costs = new()
        {
            new OreAmount(data.handleOreId, product.HandleOreCost),
            new OreAmount(data.metalOreId, product.MetalOreCost)
        };
        if (data.hasGem) costs.Add(new OreAmount(data.gemOreId, product.GemOreCost));
        List<OreAmount> totals = costs.GroupBy(cost => cost.oreId)
            .Select(group => new OreAmount(group.Key, group.Sum(cost => cost.amount))).ToList();

        for (int i = 0; i < totals.Count; i++)
        {
            OreAmount cost = totals[i];
            int owned = GameManager.Instance.StockManager.GetOreAmount(cost.oreId);
            materialNames[i].gameObject.SetActive(true);
            materialAmounts[i].gameObject.SetActive(true);
            materialNames[i].text = OreDataDB.GetData(cost.oreId).DisplayName;
            materialAmounts[i].text = $"{owned:N0} / {cost.amount:N0}";
            materialAmounts[i].color = owned >= cost.amount ? new Color32(61, 214, 198, 255) : new Color32(255, 138, 50, 255);
        }

        forgeButton.interactable = GameManager.Instance.Upgrade.HasState(blueprint)
            && GameManager.Instance.Upgrade.GetState(blueprint).unlockedLevel > 0
            && GameManager.Instance.StockManager.CanConsumeOre(totals);
    }

    public void Forge()
    {
        ForgedGearUpgradeDataSO blueprint = forge.SelectedBlueprint;
        if (blueprint == null) return;
        if (GameManager.Instance.StockManager.TryForgeGear(blueprint.ForgedGearId, materials.SelectedMaterials))
            description.text = $"{blueprint.DisplayName}\n1개 제작 완료";
    }

    public void Dispose()
    {
        forge.BlueprintSelected -= OnBlueprintSelected;
        materials.MaterialChanged -= OnMaterialChanged;
        GameManager.Instance.StockManager.UnsubscribeStockDataChange(Refresh);
        GameManager.Instance.Upgrade.UnsubscribeUpgradeChanged(Refresh);
    }
}
