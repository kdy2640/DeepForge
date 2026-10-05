using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_OreViewCard : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI amountText;

    public void SetData(OreAmount oreAmount)
    {
        OreDataSO data = OreDataDB.GetData(oreAmount.oreId);
        icon.color = data.Color;
        amountText.color = Color.Lerp(data.Color, Color.white, 0.6f);
        amountText.text = oreAmount.amount.ToString("N0");
    }
}
