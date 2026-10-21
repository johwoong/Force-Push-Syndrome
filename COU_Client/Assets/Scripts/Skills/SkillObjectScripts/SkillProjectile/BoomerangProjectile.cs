using UnityEngine;

// 타겟을 쫓아가다 적에게 처음 닿으면 대미지를 주고 주인(Owner)에게 돌아오는 탄환. 주인에게 도착하면 풀에 반환
// 타겟이 사라지거나 너무 오래 나가 있으면 맞추지 못해도 돌아옴
// _catchHealRatio가 0보다 크면 주인에게 돌아와 도착할 때마다 주인의 최대 체력 비율만큼 회복시킴 (적을 맞혔는지와 무관)
public sealed class BoomerangProjectile : SkillProjectile
{
    private enum State
    {
        Outbound,
        Returning
    }

    // 주인에게 이 거리 안으로 들어오면 도착한 것으로 봄
    [SerializeField] private float _catchDistance = 0.3f;

    // 맞추지 못한 채 나가 있을 수 있는 최대 시간. 지나면 돌아옴
    [SerializeField] private float _maxOutboundTime = 3.0f;

    // 주인에게 닿지 못하는 상황을 대비한 최대 수명. 지나면 그 자리에서 풀에 반환
    [SerializeField] private float _maxLifeTime = 10.0f;

    // 주인에게 도착했을 때 회복시킬 주인의 최대 체력 비율 (0.01 = 1%). 0이면 회복 없음. 진화 프리팹에만 지정함
    // 회복량은 최소 1이고 받는 치유량 보너스가 곱해짐. 반복 발동으로 나간 부메랑도 같은 양을 회복함
    [SerializeField] private float _catchHealRatio = 0.0f;

    private Transform _owner;
    private PlayerHealth _ownerHealth;
    private Enemy _targetEnemy;
    private State _state;
    private float _elapsed;
    private float _speed;

    protected override void OnInit(ProjectileData data)
    {
        // base.OnInit 안에서 OnLaunch가 불리므로 그 전에 주인을 받아 둠
        _owner = data.Owner;
        _ownerHealth = null;

        if(_owner != null)
        {
            _owner.TryGetComponent(out _ownerHealth);
        }

        base.OnInit(data);
    }

    protected override void OnLaunch()
    {
        _state = State.Outbound;
        _elapsed = 0.0f;
        _speed = stats.Speed > 0.0f ? stats.Speed : 1.0f;
        _targetEnemy = null;

        if(target != null)
        {
            target.TryGetComponent(out _targetEnemy);
        }

        // 탄두가 진행 방향을 향하도록 회전
        FaceDirection(direction);
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        if(_elapsed >= _maxLifeTime)
        {
            ReturnToPool();
            return;
        }

        if(_state == State.Outbound)
        {
            UpdateOutbound();
        }
        else
        {
            UpdateReturning();
        }
    }

    private void UpdateOutbound()
    {
        if(IsTargetAlive())
        {
            SteerTowards(_targetEnemy.transform.position);
        }
        else if(_targetEnemy != null || _elapsed >= _maxOutboundTime)
        {
            // 쫓던 적이 죽었거나 너무 오래 나가 있으면 돌아옴
            _state = State.Returning;
            return;
        }

        Move();
    }

    private void UpdateReturning()
    {
        if(_owner == null || !_owner.gameObject.activeInHierarchy)
        {
            ReturnToPool();
            return;
        }

        Vector2 toOwner = _owner.position - transform.position;

        // 이번 프레임 이동량이 남은 거리보다 크면 지나쳐 버리므로 도착으로 처리
        if(toOwner.magnitude <= Mathf.Max(_catchDistance, _speed * Time.deltaTime))
        {
            HealOwner();
            ReturnToPool();
            return;
        }

        SteerTowards(_owner.position);
        Move();
    }

    // 주인에게 도착했을 때 주인의 최대 체력 _catchHealRatio만큼 회복 (최소 1, 받는 치유량 보너스 적용)
    private void HealOwner()
    {
        if(_catchHealRatio <= 0.0f || _ownerHealth == null)
        {
            return;
        }

        int amount = Mathf.Max(1, Mathf.RoundToInt(_ownerHealth.MaxHealth * _catchHealRatio));
        int healReceived = stats.PlayerStats != null ? stats.PlayerStats.HealReceived : 0;

        _ownerHealth.Heal(PlayerDamageCalculator.ApplyHealReceived(amount, healReceived));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 돌아오는 중에는 대미지를 주지 않음
        if(_state != State.Outbound)
        {
            return;
        }

        if(!other.TryGetComponent(out Enemy enemy))
        {
            return;
        }

        // 쫓던 타겟이 아니어도 처음 닿은 적에게 대미지를 주고 돌아옴
        ApplyDamage(enemy);
        _state = State.Returning;
    }

    private void SteerTowards(Vector3 position)
    {
        Vector2 toPosition = position - transform.position;

        if(toPosition.sqrMagnitude > 0.0001f)
        {
            FaceDirection(toPosition.normalized);
        }
    }

    private void Move()
    {
        MoveForward(_speed);
    }

    // 죽었거나 풀로 반환된 적은 따라가지 않음
    private bool IsTargetAlive()
    {
        return _targetEnemy != null && !_targetEnemy.isDead && _targetEnemy.gameObject.activeInHierarchy;
    }
}
