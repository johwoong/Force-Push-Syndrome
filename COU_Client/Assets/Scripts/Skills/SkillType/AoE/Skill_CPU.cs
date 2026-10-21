// 가장 가까운 적이 있던 위치로 CPU(ParabolicProjectile)를 포물선으로 던짐. 도착하면 폭발이 소환되어 범위 대미지를 줌
public sealed class Skill_CPU : AoeSkillBase
{
    protected override void Fire()
    {
        // 투사체는 던지는 순간의 타겟 위치만 쓰고 따라가지 않음
        Spawn(transform.position, new ProjectileData(BuildStats(), fireDirection, fireTarget));
    }
}
