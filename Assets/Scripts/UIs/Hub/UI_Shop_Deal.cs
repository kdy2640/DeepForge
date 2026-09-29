using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_Shop_Deal : UI_Base
{
    private enum Buttons
    {
        BackButton,
        SellOreButton,
        SellGearButton,
        BuyBlueprintButton
    }

    [Header("시연 버튼 대상")]
    [SerializeField] private OreAmount sellOre = new(1, 1);
    [SerializeField, Min(0)] private int sellGearIndex;
    [SerializeField] private UpgradeDataSO blueprint;
    [SerializeField] private string lastResult;

    private enum PanelAnimators
    {
        Panel
    }

    protected override void OnInit()
    {
        Bind<Button>(typeof(Buttons));
        Bind<PanelAnimator>(typeof(PanelAnimators));
        GetButton((int)Buttons.BackButton).onClick.AddListener(
            () => Owner.RequestStateChange(CanvasController.CanvasState.BaseView));
        GetButton((int)Buttons.SellOreButton).onClick.AddListener(SellOre);
        GetButton((int)Buttons.SellGearButton).onClick.AddListener(SellGear);
        GetButton((int)Buttons.BuyBlueprintButton).onClick.AddListener(BuyBlueprint);
    }

    private void SellOre()
    {
        lastResult = GameManager.Instance.StockManager.TrySellOre(sellOre)
            ? "자원 판매 성공" : "자원 부족 또는 잘못된 수량";
    }

    private void SellGear()
    {
        StockManager stock = GameManager.Instance.StockManager;
        if (sellGearIndex < 0 || sellGearIndex >= stock.StockData.ForgedGears.Count)
        {
            lastResult = "판매할 제품 없음";
            return;
        }

        lastResult = stock.TrySellForgedGear(stock.StockData.ForgedGears[sellGearIndex])
            ? "제품 판매 성공" : "판매할 제품 없음";
    }

    private void BuyBlueprint()
    {
        UpgradeManager upgrade = GameManager.Instance.Upgrade;
        UpgradeAvailability availability = upgrade.GetBlueprintAvailability(blueprint);
        lastResult = upgrade.TryBuyBlueprint(blueprint)
            ? "설계도 구매 성공" : availability.ToString();
    }

    protected override IEnumerator OnShow()
    {
        yield return GetUI<PanelAnimator>((int)PanelAnimators.Panel).Show();
    }

    protected override IEnumerator OnHide()
    {
        yield return GetUI<PanelAnimator>((int)PanelAnimators.Panel).Hide();
    }
}
