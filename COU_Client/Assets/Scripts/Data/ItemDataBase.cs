using UnityEngine;

public static class ItemDatabase
{
    // 등급 프레임은 모든 부위가 같은 SF 프레임을 쓴다. (부위별로 나누지 않는다)
    private const string GradeFolder = "Equip/Inven/Item/Equip/Grade/";

    public static Sprite GetGradeIcon(EquipSlotType slotType, ItemGrade grade)
    {
        return Resources.Load<Sprite>(GradeFolder + grade);
    }

}
