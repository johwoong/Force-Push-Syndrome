using System;
using UnityEngine;

[Serializable]
public class ItemData
{
    public long itemId;
    public string itemName;
    public string description;
    public string iconPath;
    public string slotType;
    public string grade;           
    public int hpBonus;
    public int attackBonus;
    public int moveSpeedBonus;

    [NonSerialized] private EquipSlotType parsedSlotType;
    [NonSerialized] private ItemGrade parsedGrade;

    public EquipSlotType SlotType => parsedSlotType;
    public ItemGrade Grade => parsedGrade;

    // JsonDataManager에서 JSON을 읽은 직후 한 번 호출
    public void OnLoaded()
    {
        if (!Enum.TryParse(slotType, out parsedSlotType))
            Debug.LogWarning($"[ItemData] {itemId}의 slotType \"{slotType}\"을(를) 알 수 없어 Weapon으로 처리합니다.");

        if (!Enum.TryParse(grade, out parsedGrade))
            Debug.LogWarning($"[ItemData] {itemId}의 grade \"{grade}\"을(를) 알 수 없어 General로 처리합니다.");
    }
}
