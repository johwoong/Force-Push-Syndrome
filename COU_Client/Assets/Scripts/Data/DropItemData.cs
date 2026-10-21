using System;

// 드롭 아이템 종류별 수치. DropItem.json의 "dropItemType" 문자열을 enum으로 바꿔서 쓴다.
[Serializable]
public class DropItemData
{
    public int dropItemId;
    public string dropItemType;   // DropItemType 이름 (예: "ExpGem1")
    public int value;

    [NonSerialized] private DropItemType parsedType;
    public DropItemType Type => parsedType;

    // JsonDataManager에서 JSON을 읽은 직후 한 번 호출. 알 수 없는 종류면 false
    public bool OnLoaded() => Enum.TryParse(dropItemType, out parsedType);
}
