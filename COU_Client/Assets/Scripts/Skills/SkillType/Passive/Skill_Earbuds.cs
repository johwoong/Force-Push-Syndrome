// 이어폰: 스킬속도증가 + 범위증가 패시브 (퍼센트). 임시 수치: 레벨당 속도 +10%, 범위 +10%
public sealed class Skill_Earbuds : PassiveSkillBase
{
    private const float SpeedRatioPerLevel = 0.1f;
    private const float AreaRatioPerLevel = 0.1f;

    protected override void ApplyEffect()
    {
        modifiers.SetPercent(this, SkillPercentStat.Speed, SpeedRatioPerLevel * level);
        modifiers.SetPercent(this, SkillPercentStat.Area, AreaRatioPerLevel * level);
    }
}
