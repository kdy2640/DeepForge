using UnityEngine;
using UnityEngine.UI;

public sealed class UI_QueueSlotCard : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Text productName;
    [SerializeField] private Text remainingTime;
    [SerializeField] private Button collectButton;
    private int slotIndex;
    private SmithSlot slot;
    private ForgedGear displayedGear;

    public void Init(int index)
    {
        slotIndex = index;
        collectButton.onClick.AddListener(Collect);
    }

    private void Collect()
    {
        GameManager.Instance.BaseCamp.Smith.TryCollect(slotIndex);
    }

    public void SetData(SmithSlot smithSlot)
    {
        if (displayedGear != null) displayedGear.IconChanged -= RefreshIcon;
        displayedGear = null;
        slot = smithSlot;
        collectButton.gameObject.SetActive(slot.isReady);
        icon.gameObject.SetActive(slot.IsOccupied);
        if (slot.IsOccupied)
        {
            if (slot.workType == SmithWorkType.EquipmentUpgrade)
            {
                EquipmentUpgradeDataSO equipment = UpgradeDataDB.GetEquipmentUpgrade(slot.equipmentId);
                icon.sprite = equipment.DisplayIcon;
                icon.color = Color.white;
                int displayLevel = slot.equipmentLevel + (equipment.EquipmentId == (int)EquipmentType.Pickaxe
                    || equipment.EquipmentId == (int)EquipmentType.Boots
                    || equipment.EquipmentId == (int)EquipmentType.ReturnRope ? 1 : 0);
                productName.text = $"{equipment.DisplayName} · Lv. {displayLevel}";
            }
            else
            {
                ForgedGearUpgradeDataSO blueprint = UpgradeDataDB.GetForgedGearUpgrade(slot.product.id);
                displayedGear = slot.product;
                if (isActiveAndEnabled) displayedGear.IconChanged += RefreshIcon;
                RefreshIcon();
                GameManager.Instance.GetComponentInChildren<ForgedGearIconBaker>().Request(displayedGear);
                productName.text = blueprint.DisplayName;
            }
            remainingTime.text = slot.isReady ? "완료" : $"{slot.remainingSeconds:F1}초";
        }
        else
        {
            productName.text = "대기 중";
            remainingTime.text = "";
        }
    }

    private void Update()
    {
        if (slot.IsWorking) remainingTime.text = $"{slot.remainingSeconds:F1}초";
    }

    private void RefreshIcon()
    {
        icon.sprite = displayedGear.Icon != null ? displayedGear.Icon : UpgradeDataDB.GetForgedGearUpgrade(displayedGear.id).DisplayIcon;
        icon.color = Color.white;
        icon.preserveAspect = true;
    }

    private void OnEnable()
    {
        if (displayedGear == null) return;
        displayedGear.IconChanged += RefreshIcon;
        RefreshIcon();
    }

    private void OnDisable()
    {
        if (displayedGear != null) displayedGear.IconChanged -= RefreshIcon;
    }
}
