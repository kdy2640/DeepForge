using UnityEngine;
using UnityEngine.UI;

public sealed class UI_ForgedGearVisualPanel : MonoBehaviour
{
    [SerializeField] private Image gearIcon;
    [SerializeField] private Text gearName;
    [SerializeField] private Text materialText;

    private UI_Smith_Forge forge;
    private UI_MaterialSelectPanel materials;

    public void Init(UI_Smith_Forge owner, UI_MaterialSelectPanel materialPanel)
    {
        forge = owner;
        materials = materialPanel;
        forge.BlueprintSelected += OnBlueprintSelected;
        materials.MaterialChanged += OnMaterialChanged;
        Refresh();
    }

    private void OnBlueprintSelected(ForgedGearUpgradeDataSO blueprint) => Refresh();
    private void OnMaterialChanged(ForgedGearData data) => Refresh();

    private void Refresh()
    {
        ForgedGearUpgradeDataSO blueprint = forge.SelectedBlueprint;
        gearIcon.gameObject.SetActive(blueprint != null);
        if (blueprint == null)
        {
            gearName.text = "청사진을 선택해 주세요";
            materialText.text = "";
            return;
        }
        ForgedGearData data = materials.SelectedMaterials;
        gearIcon.sprite = blueprint.DisplayIcon;
        gearIcon.color = OreDataDB.GetData(data.metalOreId).Color;
        gearName.text = blueprint.DisplayName;
        materialText.text = $"손잡이 · {OreDataDB.GetData(data.handleOreId).DisplayName}\n"
            + $"금속 · {OreDataDB.GetData(data.metalOreId).DisplayName}\n"
            + (data.hasGem ? $"보석 · {OreDataDB.GetData(data.gemOreId).DisplayName}" : "보석 · 없음");
    }

    public void Dispose()
    {
        forge.BlueprintSelected -= OnBlueprintSelected;
        materials.MaterialChanged -= OnMaterialChanged;
    }
}
