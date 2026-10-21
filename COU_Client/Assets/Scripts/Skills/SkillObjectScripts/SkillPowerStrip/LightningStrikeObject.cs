using UnityEngine;

// 대상 적의 위치에 내리치는 낙뢰. _hitDelay가 지나면 대상 한 명에게 대미지를 주고, _lifeTime이 지나면 풀에 반환 (단일 대상, 범위 판정 없음)
// 대상이 움직이면 따라가고, 대상이 죽거나 풀로 돌아가면 마지막 위치에 남아 연출만 끝냄 (대미지는 주지 않음)
// 프리팹의 pivot은 번개가 닿는 지점에 둠. 범위 증가 보너스는 프리팹 크기에 곱해짐
public sealed class LightningStrikeObject : SkillAttackObject<StrikeData>
{
    // 생성 후 대미지가 들어가기까지의 시간 (번개 애니메이션이 땅에 닿는 순간에 맞춤)
    [SerializeField] private float _hitDelay = 0.0f;

    // 풀에 반환되기까지의 시간 (애니메이션 길이). 대미지 시점보다 짧아도 대미지는 준 뒤 반환
    [SerializeField] private float _lifeTime = 0.4f;

    private Transform _target;
    private Enemy _enemy;
    private Vector3 _targetPosition;
    private Vector3 _baseScale;
    private bool _initialized;
    private float _elapsed;
    private bool _hasHit;

    // 타격 시 소환되면 맞은 적의 머리 위에 낙뢰가 내리침
    public override bool CanSpawnOnHit => true;

    public override void InitFromHit(HitSpawnData hit)
    {
        Init(new StrikeData(hit.Stats, hit.Target));
    }

    protected override void OnInit(StrikeData data)
    {
        // 풀에서 다시 꺼낼 때 바뀐 크기가 아니라 프리팹 원래 크기를 기준으로 삼기 위해 첫 Init에서 한 번만 저장
        if(!_initialized)
        {
            _initialized = true;
            _baseScale = transform.localScale;
        }

        stats = data.Stats;
        _target = data.Target;
        _enemy = null;

        if(_target != null)
        {
            _target.TryGetComponent(out _enemy);
            _targetPosition = _target.position;
        }
        else
        {
            _targetPosition = transform.position;
        }

        transform.localScale = _baseScale * stats.Scale;

        OnLaunch();
    }

    protected override void OnLaunch()
    {
        _elapsed = 0.0f;
        _hasHit = false;
        FollowTarget();
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        FollowTarget();

        if(!_hasHit && _elapsed >= _hitDelay)
        {
            _hasHit = true;
            Hit();
        }

        if(_elapsed >= Mathf.Max(_lifeTime, _hitDelay))
        {
            ReturnToPool();
        }
    }

    // 대상이 살아 있는 동안만 따라감. 죽어서 풀로 돌아가면 마지막 위치에 고정
    private void FollowTarget()
    {
        if(_target != null && _target.gameObject.activeInHierarchy)
        {
            _targetPosition = _target.position;
        }

        transform.position = _targetPosition;
    }

    private void Hit()
    {
        // 낙뢰가 내리치기 전에 대상이 죽었으면 대미지를 주지 않음
        if(_enemy == null || _enemy.isDead || !_enemy.gameObject.activeInHierarchy)
        {
            return;
        }

        PlayerDamageCalculator.HitResult hit = stats.CalculateHit();

        _enemy.Damaged(hit.damage, stats.SkillId, KnockbackEnabled);
        PlayHitSfx();
        SpawnOnHit(_enemy, stats);
    }
}
