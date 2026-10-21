// 오버클럭: 치명타증폭 패시브 (합산, 퍼센트 포인트). 임시 수치: 레벨당 +20
// SkillController가 합계를 PlayerStats 보너스로 전달하므로 여기서는 modifiers에만 등록함
public sealed class Skill_Overclock : PassiveSkillBase
{
    private const float CritDamagePerLevel = 20.0f;

    protected override void ApplyEffect()
    {
        modifiers.SetAdd(this, SkillAddStat.CritDamage, CritDamagePerLevel * level);
    }
}
