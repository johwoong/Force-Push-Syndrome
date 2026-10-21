// 기계식 키보드 축: 피해증가 패시브 (퍼센트). 임시 수치: 레벨당 +10%
public sealed class Skill_KeyboardSwitch : PassiveSkillBase
{
    private const float DamageRatioPerLevel = 0.1f;

    protected override void ApplyEffect()
    {
        modifiers.SetPercent(this, SkillPercentStat.Damage, DamageRatioPerLevel * level);
    }
}
