// 투사체/오브젝트 개수 증가 패시브 (고정값). 임시 수치: 레벨당 +1
public sealed class Skill_ProjectileUp : PassiveSkillBase
{
    private const int CountPerLevel = 1;

    protected override void ApplyEffect()
    {
        modifiers.SetFlat(this, SkillFlatStat.ProjectileCount, CountPerLevel * level);
    }
}
