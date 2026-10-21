
using System;

// ItemGradeEffect.json의 한 행을 나타내는 데이터 클래스
// 아이템의 특정 등급에서 해금되는 효과를 연결한다.
[Serializable]
public class ItemGradeEffectData
{
    public int itemGradeEffectId; // 행 고유 ID
    public long itemId;           // Item.itemId 참조
    public string grade;          // General / Super / Rare
    public long effectId;         // EquipEffect.effectId 참조

    [NonSerialized]
    private ItemGrade parsedGrade;

    public ItemGrade Grade => parsedGrade;

    // JsonDataManager에서 JSON 역직렬화 직후 호출
    public void OnLoaded()
    {
        if (!Enum.TryParse(grade, out parsedGrade)
            || !Enum.IsDefined(typeof(ItemGrade), parsedGrade)
            || !string.Equals(
                grade,
                parsedGrade.ToString(),
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"[ItemGradeEffectData] ID {itemGradeEffectId}: " +
                $"잘못된 grade '{grade}'");
        }
    }
}
