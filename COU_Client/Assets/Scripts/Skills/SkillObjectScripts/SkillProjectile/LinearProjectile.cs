using System.Collections.Generic;
using UnityEngine;

// 앞으로 날아가다 적을 맞히는 탄환
// 맞힐 수 있는 적 수(pierceCount)를 다 쓰면, 연쇄 횟수(chainCount)가 남아 있을 때 주변 적에게 튕기고 아니면 사라짐
// homingTarget이 있으면 그 적을 쫓아감 (유도탄, 연쇄로 튕긴 뒤)
public class LinearProjectile : SkillProjectile
{
    // 안전장치용 최대 비행 시간. 평소에는 사거리 기반 최대 비행 거리(MaxDistanceRangeRatio)에서 먼저 사라지고,
    // 사거리 정보가 없을 때만 이 시간이 수명이 됨. 거리 기반으로 날아갈 시간이 이보다 길면 그 시간까지 늘어남
    [SerializeField] protected float lifeTime = 3f;

    // 연쇄로 튕길 다음 적을 찾는 반경 (방금 맞힌 적 기준)
    [SerializeField] private float _chainRadius = 3.0f;

    // 최대 비행 거리 = 사거리 × 이 비율 (넉넉하게 2배). 연쇄로 튕기면 거리를 다시 셈
    private const float MaxDistanceRangeRatio = 2.0f;

    // 거리 기반 비행에 걸리는 시간에 곱하는 여유 (속도가 줄거나 방향을 틀어 돌아가도 거리보다 시간이 먼저 끊기지 않도록)
    private const float TimeSafetyMargin = 1.5f;

    // 같은 적을 두 번 맞히거나, 튕길 때 이미 맞힌 적에게 돌아가지 않도록 기록
    private readonly HashSet<Enemy> _hitEnemies = new();

    protected float elapsed;
    protected float currentSpeed;
    protected Enemy homingTarget;

    private int _remainingHits;
    private int _remainingChains;

    // 발사(또는 마지막 연쇄) 이후 실제로 날아간 거리와 최대 비행 거리, 시간 안전장치
    private float _traveled;
    private float _maxDistance;
    private float _timeLimit;
    private Vector3 _lastPosition;

    protected override void OnLaunch()
    {
        elapsed = 0f;
        currentSpeed = stats.Speed > 0f ? stats.Speed : 1.0f;
        homingTarget = null;

        _remainingHits = pierceCount;
        _remainingChains = chainCount;
        _hitEnemies.Clear();

        // 사거리 정보가 있으면 거리로 수명을 정하고, 시간은 안전장치로만 씀
        _traveled = 0f;
        _lastPosition = transform.position;
        _maxDistance = stats.Range > 0f ? stats.Range * MaxDistanceRangeRatio : float.PositiveInfinity;
        _timeLimit = float.IsInfinity(_maxDistance) ? lifeTime : Mathf.Max(lifeTime, _maxDistance / currentSpeed * TimeSafetyMargin);

        // 탄두가 발사 방향을 향하도록 회전
        FaceDirection(direction);
    }

    private void Update()
    {
        Movement();

        elapsed += Time.deltaTime;

        _traveled += Vector3.Distance(transform.position, _lastPosition);
        _lastPosition = transform.position;

        if(_traveled >= _maxDistance || elapsed >= _timeLimit)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Enemy에만 반응 (플레이어 등 다른 물체와 겹쳐도 사라지지 않음)
        if(!other.TryGetComponent(out Enemy enemy))
        {
            return;
        }

        OnHitEnemy(enemy);
    }

    protected virtual void OnHitEnemy(Enemy enemy)
    {
        // 이미 다 맞힌 경우(한 물리 스텝에 여러 적과 겹친 경우 등), 죽은 적, 이미 맞힌 적은 무시
        if(_remainingHits <= 0 || enemy.isDead || !_hitEnemies.Add(enemy))
        {
            return;
        }

        ApplyDamage(enemy);
        _remainingHits--;

        // 쫓던 적을 맞혔으면 더 쫓지 않고 지금 방향으로 계속 나아감 (관통 중에 같은 적 주위를 맴돌지 않도록)
        if(enemy == homingTarget)
        {
            homingTarget = null;
        }

        // 관통: 아직 맞힐 수 있으면 계속 나아감
        if(_remainingHits > 0)
        {
            return;
        }

        if(_remainingChains > 0 && TryChain(enemy))
        {
            return;
        }

        OnFinishHit();
    }

    // 더 맞힐 수도, 튕길 수도 없을 때 호출. 기본은 바로 풀에 반환
    protected virtual void OnFinishHit()
    {
        ReturnToPool();
    }

    protected virtual void Movement()
    {
        if(IsHomingTargetAlive())
        {
            SteerTowards(homingTarget.transform.position);
        }

        MoveForward(currentSpeed);
    }

    // 방금 맞힌 적 주변에서 아직 맞히지 않은 가장 가까운 적에게 튕김. 튕길 적이 없으면 false
    private bool TryChain(Enemy from)
    {
        Enemy next = SkillTargeting.FindNearestEnemy(from.transform.position, _chainRadius, _hitEnemies);

        if(next == null)
        {
            return false;
        }

        _remainingChains--;
        _remainingHits = 1;
        homingTarget = next;

        // 튕긴 뒤 날아갈 시간과 거리를 확보하도록 수명을 다시 셈
        elapsed = 0f;
        _traveled = 0f;

        SteerTowards(next.transform.position);
        return true;
    }

    private void SteerTowards(Vector3 position)
    {
        Vector2 toPosition = position - transform.position;

        if(toPosition.sqrMagnitude > 0.0001f)
        {
            FaceDirection(toPosition.normalized);
        }
    }

    // 죽었거나 풀로 반환된 적은 따라가지 않음
    private bool IsHomingTargetAlive()
    {
        return homingTarget != null && !homingTarget.isDead && homingTarget.gameObject.activeInHierarchy;
    }
}
