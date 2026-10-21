using System;

// 드롭 테이블 한 행. 몬스터 역할(monsterType)마다 테이블 하나다.
// DropTable.json의 "monsterType", "dropItemType" 문자열을 enum으로 바꿔서 쓴다.
[Serializable]
public class DropTableEntryData
{
    public string monsterType;    // "Normal" / "Elite" / "Boss" / "Box". Monster.json의 monsterType과 같은 값
    public int dropGroup;         // 같은 테이블 안에서 그룹마다 한 번씩 추첨
    public string dropItemType;   // DropItemType 이름 또는 "None"
    public int weight;            // 그룹 안의 가중치
    public int count;             // 뽑혔을 때 생성할 개수

    [NonSerialized] private MonsterType parsedMonsterType;
    public MonsterType MonsterType => parsedMonsterType;

    [NonSerialized] private DropItemType parsedType;
    public DropItemType Type => parsedType;
    public bool IsNone => dropItemType == "None";

    // JsonDataManager에서 JSON을 읽은 직후 한 번 호출. 알 수 없는 역할이나 종류면 false
    public bool OnLoaded()
        => Enum.TryParse(monsterType, out parsedMonsterType)
           && (IsNone || Enum.TryParse(dropItemType, out parsedType));
}
