using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_Smith_Equipment : UI_Base
{
    private enum UIs { UI_EquipmentUpgradePanel, UI_EquipmentVisualPanel, UI_EquipmentDetailPanel, UI_SmithInfoPanel }
    public EquipmentUpgradeDataSO SelectedEquipment { get; private set; }
    public event Action<EquipmentUpgradeDataSO> EquipmentSelected;
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
        UI_EquipmentVisualPanel visual = GetGameObject((int)UIs.UI_EquipmentVisualPanel).GetComponent<UI_EquipmentVisualPanel>();
        visual.Init(this);
        GetGameObject((int)UIs.UI_EquipmentDetailPanel).GetComponent<UI_EquipmentDetailPanel>().Init(this, visual);
        GetGameObject((int)UIs.UI_EquipmentUpgradePanel).GetComponent<UI_EquipmentUpgradePanel>().Init(this);
        GetGameObject((int)UIs.UI_SmithInfoPanel).GetComponent<UI_SmithInfoPanel>().Init();
        GetButton((int)Buttons.BackButton).onClick.AddListener(
            () => Owner.RequestStateChange(CanvasController.CanvasState.BaseView));
        GetButton((int)Buttons.ForgeCategoryButton).onClick.AddListener(
            () => Owner.RequestStateChange(CanvasController.CanvasState.Smith_Forge));
        GetButton((int)Buttons.EquipmentCategoryButton).onClick.AddListener(
            () => Owner.RequestStateChange(CanvasController.CanvasState.Smith_Equipment));
    }

    public void SelectEquipment(EquipmentUpgradeDataSO data)
    {
        SelectedEquipment = data;
        EquipmentSelected.Invoke(data);
    }

    protected override IEnumerator OnShow()
    {
        GetGameObject((int)UIs.UI_EquipmentUpgradePanel).GetComponent<UI_EquipmentUpgradePanel>().Refresh();
        yield return GetUI<PanelAnimator>((int)PanelAnimators.Panel).Show();
    }

    protected override IEnumerator OnHide()
    {
        yield return GetUI<PanelAnimator>((int)PanelAnimators.Panel).Hide();
    }

    private void OnDestroy()
    {
        if (!IsInitialized) return;
        if (GameManager.Instance == null) return;
        GetGameObject((int)UIs.UI_EquipmentUpgradePanel).GetComponent<UI_EquipmentUpgradePanel>().Dispose();
        GetGameObject((int)UIs.UI_EquipmentVisualPanel).GetComponent<UI_EquipmentVisualPanel>().Dispose();
        GetGameObject((int)UIs.UI_EquipmentDetailPanel).GetComponent<UI_EquipmentDetailPanel>().Dispose();
        GetGameObject((int)UIs.UI_SmithInfoPanel).GetComponent<UI_SmithInfoPanel>().Dispose();
    }
}
