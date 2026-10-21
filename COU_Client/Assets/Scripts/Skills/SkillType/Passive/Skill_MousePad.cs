// 마우스 패드: 관통 패시브 (고정값). 임시 수치: 레벨당 +1
// 맞히면 사라지는 투사체(텀블러, 마우스, 안경)에만 적용됨. 무한 관통(모니터), 부메랑(CtrlZ), 포물선(CPU)은 효과 없음
public sealed class Skill_MousePad : PassiveSkillBase
{
    private const int PiercePerLevel = 1;

    protected override void ApplyEffect()
    {
        modifiers.SetFlat(this, SkillFlatStat.Pierce, PiercePerLevel * level);
    }
}
