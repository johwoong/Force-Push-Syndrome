using UnityEngine;

public sealed class Skill_Tumbler : ProjectileSkillBase
{
    // 탄환 사이 간격 (도)
    private const float AnglePerProjectile = 15.0f;

    // 한 번에 나가는 최대 탄환 수 (개수 증가 패시브를 더해도 이 이상은 늘지 않음)
    private const int MaxProjectileCount = 3;

    protected override void Fire()
    {
        // 기본 1발, 진화(5레벨 + 에너지드링크)하면 부채꼴 3발. 개수 증가 패시브 보너스는 여기에 더해지되 MaxProjectileCount까지만
        int baseCount = IsEvolved ? 3 : 1;
        int count = Mathf.Min(GetFlatStat(SkillFlatStat.ProjectileCount, baseCount), MaxProjectileCount);

        FireSpread(count, AnglePerProjectile * (count - 1));
    }
}
