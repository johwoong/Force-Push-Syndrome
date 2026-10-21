// 정전기: 연쇄 패시브 (고정값). 임시 수치: 레벨당 +1
// 투사체는 관통을 다 쓴 뒤 주변 적에게 튕기고(LinearProjectile), 멀티탭 번개는 맞은 적에게서 다음 적으로 이어짐(Skill_PowerStrip)
public sealed class Skill_StaticShock : PassiveSkillBase
{
    private const int ChainPerLevel = 1;

    protected override void ApplyEffect()
    {
        modifiers.SetFlat(this, SkillFlatStat.Chain, ChainPerLevel * level);
    }
}
