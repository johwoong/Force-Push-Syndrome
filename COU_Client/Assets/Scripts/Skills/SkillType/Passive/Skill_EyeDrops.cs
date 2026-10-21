// 안약: 사거리증가 패시브 (퍼센트, 적 탐지 범위). 임시 수치: 레벨당 +10%
public sealed class Skill_EyeDrops : PassiveSkillBase
{
    private const float RangeRatioPerLevel = 0.1f;

    protected override void ApplyEffect()
    {
        modifiers.SetPercent(this, SkillPercentStat.Range, RangeRatioPerLevel * level);
    }
}
