using UnityEngine;
using UnityEngine.UI;

public sealed class UI_QueueSlotCard : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Text productName;
    [SerializeField] private Text remainingTime;
    private SmithSlot slot;

    public void SetData(SmithSlot smithSlot)
    {
        slot = smithSlot;
        icon.gameObject.SetActive(slot.IsWorking);
        if (slot.IsWorking)
        {
            if (slot.workType == SmithWorkType.EquipmentUpgrade)
            {
                EquipmentUpgradeDataSO equipment = UpgradeDataDB.GetEquipmentUpgrade(slot.equipmentId);
                icon.sprite = equipment.DisplayIcon;
                icon.color = Color.white;
                productName.text = $"{equipment.DisplayName} · Lv. {slot.equipmentLevel}";
            }
            else
            {
                ForgedGearUpgradeDataSO blueprint = UpgradeDataDB.GetForgedGearUpgrade(slot.product.id);
                icon.sprite = blueprint.DisplayIcon;
                icon.color = OreDataDB.GetData(slot.product.data.metalOreId).Color;
                productName.text = blueprint.DisplayName;
            }
            remainingTime.text = $"{slot.remainingSeconds:F1}초";
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
}
