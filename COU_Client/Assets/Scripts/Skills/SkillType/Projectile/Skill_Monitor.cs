using UnityEngine;

public sealed class Skill_Monitor : ProjectileSkillBase
{
    // 한 번에 나가는 최대 방향 수 (개수 증가 패시브를 더해도 이 이상은 늘지 않음)
    private const int MaxProjectileCount = 4;

    protected override void Fire()
    {
        // 모니터는 모든 레벨에서 적을 계속 관통. 기본 1발, 진화(5레벨 + 보조 모니터)하면 앞/우/뒤/좌 4방향
        // 개수 증가 패시브 보너스는 여기에 더해지되 MaxProjectileCount까지만
        int baseCount = IsEvolved ? 4 : 1;

        // SFX\

        FireRadial(Mathf.Min(GetFlatStat(SkillFlatStat.ProjectileCount, baseCount), MaxProjectileCount), InfinitePierce);
    }
}
