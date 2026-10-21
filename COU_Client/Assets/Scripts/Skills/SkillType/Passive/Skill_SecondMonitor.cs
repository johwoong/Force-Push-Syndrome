// 보조 모니터: 반복 패시브 (고정값). 임시 수치: 레벨당 +1
// 쿨타임으로 발동하는 스킬이 간격을 두고 다시 발동되며, 다시 발동한 스킬은 대미지 50% (CooldownSkillBase)
public sealed class Skill_SecondMonitor : PassiveSkillBase
{
    private const int RepeatPerLevel = 1;

    protected override void ApplyEffect()
    {
        modifiers.SetFlat(this, SkillFlatStat.Repeat, RepeatPerLevel * level);
    }
}
