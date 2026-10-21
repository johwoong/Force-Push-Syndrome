// 진화: 대미지가 사거리(탐지 범위) 증가량만큼 늘어남
//       시트 대미지 × (1 + 레벨 대미지% 합 + 패시브 대미지% 합 + 사거리% 합) × 반복 비율 (사거리% 합 = 레벨 효과 + 패시브)
public sealed class Skill_Glasses : ProjectileSkillBase
{
    protected override float ExtraDamagePercent => IsEvolved ? GetPercentTotal(SkillPercentStat.Range) : 0.0f;

    protected override void Fire()
    {
        // SFX

        // 기본 관통 1. 관통 보너스(레벨 효과, 패시브)는 SpawnProjectile에서 더해짐
        SpawnProjectile(fireDirection);
    }
}
