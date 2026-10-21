using System;

// 스킬 레벨 추가 효과 테이블(SkillLevel) 한 행. 스킬 하나의 레벨 하나가 행 하나
// 효과는 최대 2개(effect1, effect2)이고, 효과가 하나뿐이면 effect2Type에 "None"을 적음
// 값은 그 레벨에서 새로 더해지는 증가량이고, 현재 레벨 이하 행의 값을 모두 더해서 씀 (1레벨 5, 2레벨 10 → 2레벨에서 15)
// SkillLevel.json의 "effect1Type", "effect2Type" 문자열을 enum으로 바꿔서 씀
[Serializable]
public class SkillLevelData
{
    public long skillId;          // 효과를 받을 스킬 (Skill.json의 skillId)
    public int skillLevel;        // 이 행이 속한 레벨 (1 ~ SkillBase.MaxLevel)
    public string effect1Type;    // SkillLevelStatType 이름
    public float effect1Value;
    public string effect2Type;    // SkillLevelStatType 이름 또는 "None"
    public float effect2Value;
    public string description;    // 레벨 설명 (UI 표시용)

    [NonSerialized] private SkillLevelStatType parsedEffect1Type;
    public SkillLevelStatType Effect1Type => parsedEffect1Type;

    [NonSerialized] private SkillLevelStatType parsedEffect2Type;
    public SkillLevelStatType Effect2Type => parsedEffect2Type;

    // JsonDataManager에서 JSON을 읽은 직후 한 번 호출. 알 수 없는 종류면 false
    public bool OnLoaded()
        => Enum.TryParse(effect1Type, out parsedEffect1Type)
           && Enum.TryParse(effect2Type, out parsedEffect2Type);
}

// 테이블에 적는 효과 종류. 모두 좋아지는 방향의 양수로 적음 (CooldownReduction 10 = 쿨타임 -10%)
public enum SkillLevelStatType
{
    None,              // 효과 없음 (effect2가 비어 있을 때)

    // 퍼센트형 (5 = +5%)
    Damage,            // 피해증가
    Area,              // 범위증가
    Speed,             // 스킬속도증가
    CooldownReduction, // 쿨타임감소
    Range,             // 사거리증가

    // 고정값형 (1 = +1)
    Pierce,            // 관통
    Chain,             // 연쇄
    Repeat,            // 반복

    // 합산형 (퍼센트 포인트. 5 = 치명타율 +5)
    CriticalChance,    // 치명타율
    CriticalDamage     // 치명타증폭
}
