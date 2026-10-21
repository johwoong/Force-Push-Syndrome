using UnityEngine;

// 가장 가까운 적에게 부메랑(BoomerangProjectile)을 던짐. 부메랑이 돌아올 때까지 다음 발사를 막고,
// 돌아온 시점부터 쿨타임을 다시 셈 (시트 쿨타임이 0에 가까우면 돌아오자마자 다시 발사)
// 반복 패시브로 여러 개가 나가면 모두 돌아온 시점부터 셈
public sealed class Skill_CtrlZ : ProjectileSkillBase
{
    // 아직 돌아오지 않은 부메랑 수
    private int _thrownCount;

    protected override bool CanActivate()
    {
        // 던진 부메랑이 아직 돌아오지 않았으면 장전되지 않은 상태
        if(_thrownCount > 0)
        {
            return false;
        }

        return base.CanActivate();
    }

    protected override void Fire()
    {
        GameObject instance = Spawn(transform.position, new ProjectileData(BuildStats(), fireDirection, fireTarget, 1, transform));

        if(instance == null || !instance.TryGetComponent(out BoomerangProjectile boomerang))
        {
            return;
        }

        _thrownCount++;
        boomerang.OnBeforeReturn += HandleBoomerangReturned;
    }

    private void HandleBoomerangReturned(GameObject instance)
    {
        // 같은 오브젝트가 풀에서 다시 나올 때 중복 구독되지 않도록 해제
        if(instance.TryGetComponent(out BoomerangProjectile boomerang))
        {
            boomerang.OnBeforeReturn -= HandleBoomerangReturned;
        }

        _thrownCount = Mathf.Max(_thrownCount - 1, 0);

        if(_thrownCount == 0)
        {
            ResetCooldown();
        }
    }
}
