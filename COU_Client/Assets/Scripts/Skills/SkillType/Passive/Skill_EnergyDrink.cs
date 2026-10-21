// 에너지드링크: 쿨타임감소 패시브 (퍼센트). 임시 수치: 레벨당 -8%
// 퍼센트 보너스는 기본값 × (1 + 합계)로 계산되므로 감소는 음수로 등록함
public sealed class Skill_EnergyDrink : PassiveSkillBase
{
    private const float CooldownReductionPerLevel = 0.08f;

    protected override void ApplyEffect()
    {
        modifiers.SetPercent(this, SkillPercentStat.Cooldown, -CooldownReductionPerLevel * level);
    }
}
