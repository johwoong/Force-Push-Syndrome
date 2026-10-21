using DG.Tweening;
using UnityEngine;

// 던진 순간의 타겟 위치로 포물선을 그리며 날아가고, 도착하면 OnLanded를 호출한 뒤 풀에 반환
// 날아가는 동안에는 적과 부딪히지 않음 (타겟을 따라가지 않고, 타겟이 죽어도 정한 위치에 도착)
// 도착 시 효과는 _landingPrefab(범위 폭발 등)으로 정하며, 다른 동작이 필요하면 상속해 OnLanded를 오버라이드
public class ParabolicProjectile : SkillProjectile
{
    // 도착하면 소환할 오브젝트. SkillObject<AoEData>가 붙은 프리팹이어야 함 (AoeHitObject 등). 비워두면 아무것도 소환하지 않음
    [SerializeField] private SkillObjectBase _landingPrefab;

    // 포물선 높이 (월드 단위)
    [SerializeField] private float _jumpPower = 1.5f;

    // 시트 속도가 0일 때 쓰는 비행 시간
    [SerializeField] private float _defaultFlightTime = 0.6f;

    // 아주 가까운 적에게 던져도 포물선이 보이도록 하는 최소 비행 시간
    [SerializeField] private float _minFlightTime = 0.2f;

    private Tween _flightTween;

    protected override void OnLaunch()
    {
        // 타겟이 없으면(정면 조준 등) 발사 방향으로 1만큼 떨어진 곳에 던짐
        Vector3 destination = target != null ? target.position : transform.position + (Vector3)direction;
        destination.z = transform.position.z;

        float distance = Vector2.Distance(transform.position, destination);
        float duration = stats.Speed > 0.0f ? distance / stats.Speed : _defaultFlightTime;

        KillFlight();

        _flightTween = transform.DOJump(destination, _jumpPower, 1, Mathf.Max(duration, _minFlightTime))
            .SetEase(Ease.Linear)
            .OnComplete(Land);
    }

    // 도착한 자리에서 일어날 일. 기본은 _landingPrefab 소환
    protected virtual void OnLanded()
    {
        SpawnLanding();
    }

    // 풀로 반환되거나 비활성화되면 남은 트윈을 정리 (다음에 꺼냈을 때 이전 트윈이 이어서 돌지 않도록)
    private void OnDisable()
    {
        KillFlight();
    }

    private void Land()
    {
        // 완료 콜백 안에서 자신을 다시 Kill하지 않도록 먼저 비움
        _flightTween = null;

        OnLanded();
        ReturnToPool();
    }

    private void SpawnLanding()
    {
        if(_landingPrefab == null)
        {
            return;
        }

        Vector3 position = transform.position;
        AoEData data = new AoEData(stats, position);

        SkillObjectPool.Instance.Get(_landingPrefab.gameObject, position, Quaternion.identity, instance =>
        {
            if(instance.TryGetComponent(out SkillObject<AoEData> landing))
            {
                landing.Init(data);
            }
        });
    }

    private void KillFlight()
    {
        if(_flightTween != null)
        {
            _flightTween.Kill();
            _flightTween = null;
        }
    }
}
