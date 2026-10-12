using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UI_BackPackVisualizer : MonoBehaviour
{
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private int[] oreIds = { 1, 2, 3, 4, 5, 6, 7, 8 };
    [SerializeField] private Image[] capacityFills;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.76f, 0.3f);
    [SerializeField] private Color fullColor = new Color(1f, 0.42f, 0.18f);

    private BackPack backPack;

    public void Init()
    {
        backPack = GameManager.Instance.GameLoop.BackPack;
        for (int i = 0; i < oreIds.Length; i++)
            capacityFills[i].color = OreDataDB.GetData(oreIds[i]).Color;
        backPack.Changed += Refresh;
        Refresh();
    }

    public void Refresh()
    {
        int count = backPack.Count;
        int capacity = backPack.Capacity;
        float ratio = Mathf.Clamp01((float)count / capacity);
        bool full = count >= capacity;
        Color color = full ? fullColor : ratio >= 0.8f ? warningColor : normalColor;

        amountText.SetText("{0} <size=75%><color=#9DA6AF>/ {1}</color></size>", count, capacity);
        amountText.color = color;
        titleText.text = full ? "배낭 · 가득 참" : "배낭";

        int accumulated = 0;
        for (int i = 0; i < oreIds.Length; i++)
        {
            int amount = backPack.GetOreAmount(oreIds[i]);
            Image fill = capacityFills[i];
            fill.gameObject.SetActive(amount > 0);
            fill.rectTransform.anchorMin = new Vector2(Mathf.Clamp01((float)accumulated / capacity), 0f);
            accumulated += amount;
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((float)accumulated / capacity), 1f);
        }
    }

    public void Dispose()
    {
        backPack.Changed -= Refresh;
    }
}
