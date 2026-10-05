using UnityEngine;
using UnityEngine.UI;
using System;

public sealed class UI_MaterialSelectPanel : MonoBehaviour
{
    [SerializeField] private OreDataSO[] ores;
    [SerializeField] private Button previousHandleButton;
    [SerializeField] private Button nextHandleButton;
    [SerializeField] private Button previousMetalButton;
    [SerializeField] private Button nextMetalButton;
    [SerializeField] private Button previousGemButton;
    [SerializeField] private Button nextGemButton;
    [SerializeField] private Image handleIcon;
    [SerializeField] private Image metalIcon;
    [SerializeField] private Image gemIcon;
    [SerializeField] private Text handleName;
    [SerializeField] private Text metalName;
    [SerializeField] private Text gemName;

    private int handleIndex;
    private int metalIndex = 1;
    private int gemIndex = -1;

    public event Action<ForgedGearData> MaterialChanged;
    public ForgedGearData SelectedMaterials => new()
    {
        handleOreId = ores[handleIndex].Id,
        metalOreId = ores[metalIndex].Id,
        hasGem = gemIndex >= 0,
        gemOreId = gemIndex >= 0 ? ores[gemIndex].Id : 0
    };

    public void Init()
    {
        previousHandleButton.onClick.AddListener(() => SelectHandle(-1));
        nextHandleButton.onClick.AddListener(() => SelectHandle(1));
        previousMetalButton.onClick.AddListener(() => SelectMetal(-1));
        nextMetalButton.onClick.AddListener(() => SelectMetal(1));
        previousGemButton.onClick.AddListener(() => SelectGem(-1));
        nextGemButton.onClick.AddListener(() => SelectGem(1));
        Refresh();
    }

    private void SelectHandle(int direction)
    {
        handleIndex = (handleIndex + direction + ores.Length) % ores.Length;
        Refresh();
    }

    private void SelectMetal(int direction)
    {
        metalIndex = (metalIndex + direction + ores.Length) % ores.Length;
        Refresh();
    }

    private void SelectGem(int direction)
    {
        gemIndex = (gemIndex + 1 + direction + ores.Length + 1) % (ores.Length + 1) - 1;
        Refresh();
    }

    private void Refresh()
    {
        handleIcon.sprite = ores[handleIndex].Icon;
        handleIcon.color = ores[handleIndex].Color;
        handleName.text = ores[handleIndex].DisplayName;
        metalIcon.sprite = ores[metalIndex].Icon;
        metalIcon.color = ores[metalIndex].Color;
        metalName.text = ores[metalIndex].DisplayName;
        gemIcon.sprite = gemIndex >= 0 ? ores[gemIndex].Icon : null;
        gemIcon.color = gemIndex >= 0 ? ores[gemIndex].Color : new Color32(48, 51, 57, 255);
        gemName.text = gemIndex >= 0 ? ores[gemIndex].DisplayName : "없음";
        MaterialChanged?.Invoke(SelectedMaterials);
    }
}
