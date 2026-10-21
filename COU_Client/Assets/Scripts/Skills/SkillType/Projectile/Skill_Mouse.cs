public sealed class Skill_Mouse : ProjectileSkillBase
{
    protected override void Fire()
    {
        // 가장 가까운 적을 타겟으로 지정하면 탄환(HomingProjectile)이 끝까지 쫓아감
        SpawnProjectile(fireDirection);
    }
}
