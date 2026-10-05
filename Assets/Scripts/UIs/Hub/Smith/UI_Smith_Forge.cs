using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_Smith_Forge : UI_Base
{
    private enum UIs
    {
        UI_BluePrintListPanel, UI_MaterialSelectPanel,
        UI_ForgedGearVisualPanel, UI_ForgedGearDetailPanel, UI_SmithInfoPanel
    }

    public ForgedGearUpgradeDataSO SelectedBlueprint { get; private set; }
    public event Action<ForgedGearUpgradeDataSO> BlueprintSelected;

    private enum Buttons
    {
        BackButton,
        ForgeCategoryButton,
        EquipmentCategoryButton
    }

    private enum PanelAnimators
    {
        Panel
    }

    protected override void OnInit()
    {
        Bind<Button>(typeof(Buttons));
        Bind<PanelAnimator>(typeof(PanelAnimators));
        Bind<GameObject>(typeof(UIs));
        GetButton((int)Buttons.BackButton).onClick.AddListener(
            () => Owner.RequestStateChange(CanvasController.CanvasState.BaseView));
        GetButton((int)Buttons.ForgeCategoryButton).onClick.AddListener(
            () => Owner.RequestStateChange(CanvasController.CanvasState.Smith_Forge));
        GetButton((int)Buttons.EquipmentCategoryButton).onClick.AddListener(
            () => Owner.RequestStateChange(CanvasController.CanvasState.Smith_Equipment));
        UI_MaterialSelectPanel materials = GetGameObject((int)UIs.UI_MaterialSelectPanel).GetComponent<UI_MaterialSelectPanel>();
        materials.Init();
        UI_ForgedGearVisualPanel visual = GetGameObject((int)UIs.UI_ForgedGearVisualPanel).GetComponent<UI_ForgedGearVisualPanel>();
        visual.Init(this);
        GetGameObject((int)UIs.UI_ForgedGearDetailPanel).GetComponent<UI_ForgedGearDetailPanel>().Init(this, materials, visual);
        GetGameObject((int)UIs.UI_BluePrintListPanel).GetComponent<UI_BluePrintListPanel>().Init(this);
        GetGameObject((int)UIs.UI_SmithInfoPanel).GetComponent<UI_SmithInfoPanel>().Init();
    }

    public void SelectBlueprint(ForgedGearUpgradeDataSO blueprint)
    {
        SelectedBlueprint = blueprint;
        BlueprintSelected.Invoke(blueprint);
    }

    protected override IEnumerator OnShow()
    {
        GetGameObject((int)UIs.UI_BluePrintListPanel).GetComponent<UI_BluePrintListPanel>().Refresh();
        yield return GetUI<PanelAnimator>((int)PanelAnimators.Panel).Show();
    }

    protected override IEnumerator OnHide()
    {
        yield return GetUI<PanelAnimator>((int)PanelAnimators.Panel).Hide();
    }

    private void OnDestroy()
    {
        if (!IsInitialized) return;
        // 종료 시 전역 매니저가 먼저 파괴되면 구독 대상도 이미 사라진 상태다.
        if (GameManager.Instance == null) return;
        GetGameObject((int)UIs.UI_BluePrintListPanel).GetComponent<UI_BluePrintListPanel>().Dispose();
        GetGameObject((int)UIs.UI_ForgedGearVisualPanel).GetComponent<UI_ForgedGearVisualPanel>().Dispose();
        GetGameObject((int)UIs.UI_ForgedGearDetailPanel).GetComponent<UI_ForgedGearDetailPanel>().Dispose();
        GetGameObject((int)UIs.UI_SmithInfoPanel).GetComponent<UI_SmithInfoPanel>().Dispose();
    }
}
