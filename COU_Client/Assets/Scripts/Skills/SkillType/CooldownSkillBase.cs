using System;
using System.Collections.Generic;
using UnityEngine;

// 쿨타임마다 사거리 안의 적을 찾아 발동하는 스킬의 공통 베이스
public abstract class CooldownSkillBase : SkillBase
{
    // 쿨타임 감소 합계의 상한 (0.5 = 최대 50% 감소). 이 이상 쌓아도 쿨타임은 더 줄지 않음
    private const float MaxCooldownReduction = 0.5f;

    // 반복 발동 사이 간격 (초). 임시 수치
    private const float RepeatInterval = 0.1f;

    // 반복으로 다시 발동한 스킬의 대미지 비율
    private const float RepeatDamageRatio = 0.5f;

    protected float cooldownTimer;
    protected Vector2 fireDirection;
    protected Transform fireTarget;
    public event Action<CooldownSkillBase> OnFired;
    public event Action<CooldownSkillBase> OnFireStarted;
    public float FireDelay { get; set; }
    private bool isFirePending;
    private float fireDelayTimer;

    private int _remainingRepeats;
    private float _repeatTimer;
    private bool _isRepeating;

    protected override float DamageScale => _isRepeating ? RepeatDamageRatio : 1.0f;

    public override void Initialize(SkillData data)
    {
        base.Initialize(data);
        cooldownTimer = data.skillCooldown;
    }

    public override void Tick(float deltaTime)
    {
        // 반복 발동은 쿨타임과 따로 진행
        TickRepeat(deltaTime);

        // 발사 대기 중: 대기 시간이 끝나면 발사
        if (isFirePending)
        {
            fireDelayTimer -= deltaTime;

            if (fireDelayTimer > 0.0f)
            {
                return;
            }

            isFirePending = false;
            FireNow();
            return;
        }

        ReduceCooldown(deltaTime);

        if (CanActivate())
        {
            OnFireStarted?.Invoke(this);

            if (FireDelay > 0.0f)
            {
                isFirePending = true;
                fireDelayTimer = FireDelay;
                return;
            }

            FireNow();
        }
    }

    // 실제 발사 + 쿨타임 초기화 + 알림
    private void FireNow()
    {
        // 대기 중 대상이 사라졌으면 처음 조준한 방향으로만 발사
        if (fireTarget != null && !fireTarget.gameObject.activeInHierarchy)
        {
            fireTarget = null;
        }

        Fire();
        ResetCooldown();

        // 선딜레이만큼 쿨타임을 줄여 발사 간격을 기존과 같게 유지
        if (FireDelay > 0.0f)
        {
            cooldownTimer = Mathf.Max(cooldownTimer - FireDelay, 0.01f);
        }

        // 반복 패시브만큼 간격을 두고 다시 발동 (앞선 반복이 남아 있으면 새 발동 기준으로 덮어씀)
        _remainingRepeats = GetFlatStat(SkillFlatStat.Repeat, 0);
        _repeatTimer = RepeatInterval;

        OnFired?.Invoke(this);
    }

    // 반복 발동. 쿨타임, 발동 알림(OnFired 등)과 관계없이 Fire만 다시 부르고 대미지는 RepeatDamageRatio만큼
    private void TickRepeat(float deltaTime)
    {
        if(_remainingRepeats <= 0)
        {
            return;
        }

        _repeatTimer -= deltaTime;

        if(_repeatTimer > 0.0f)
        {
            return;
        }

        _remainingRepeats--;
        _repeatTimer = RepeatInterval;

        // 앞선 발동에 대상이 죽었을 수 있으므로 다시 조준. 사거리 안에 적이 없으면 직전 방향 그대로 발사
        FindFireDirection();

        if(fireTarget != null && !fireTarget.gameObject.activeInHierarchy)
        {
            fireTarget = null;
        }

        _isRepeating = true;
        Fire();
        _isRepeating = false;
    }

    // 쿨타임이 끝났고, 사거리 안에 적이 있을 때만 발동
    protected virtual bool CanActivate()
    {
        if(cooldownTimer > 0.0f)
        {
            return false;
        }

        return FindFireDirection();
    }

    // 실제 발동 동작. 레벨별 분기는 각 스킬이 필요한 만큼만 구현
    protected abstract void Fire();

    // 사거리 안의 적을 찾아서 발사 방향(fireDirection)을 정함. 적이 없으면 false
    private bool FindFireDirection()
    {
        List<Collider2D> enemies = SkillTargeting.OverlapEnemies(transform.position, Range);

        if(enemies.Count == 0)
        {
            return false;
        }

        // Forward: 플레이어가 바라보는 방향
        if(skillData.TargetType == SkillTargetType.Forward)
        {
            fireDirection = movement.FacingDirection;
            fireTarget = null;
            return true;
        }

        // Nearest: 사거리 안에서 가장 가까운 적 방향 (제곱 거리로 비교해 제곱근 계산을 줄임)
        Vector2 origin = transform.position;
        Collider2D nearest = null;
        float minSqrDistance = float.MaxValue;

        foreach(Collider2D enemy in enemies)
        {
            float sqrDistance = ((Vector2)enemy.transform.position - origin).sqrMagnitude;

            if(sqrDistance < minSqrDistance)
            {
                minSqrDistance = sqrDistance;
                nearest = enemy;
            }
        }

        fireDirection = ((Vector2)nearest.transform.position - origin).normalized;
        fireTarget = nearest.transform;
        return true;
    }

    private void ReduceCooldown(float deltaTime)
    {
        if(cooldownTimer > 0.0f)
        {
            cooldownTimer -= deltaTime;
        }
    }

    protected void ResetCooldown()
    {
        cooldownTimer = GetCooldown();
    }

    // 레벨 효과와 패시브를 반영한 최종 쿨타임(초)
    protected float GetCooldown()
    {
        if(skillData == null)
        {
            return 0.01f;
        }

        // 쿨타임 감소는 레벨 효과와 패시브를 합쳐 최대 MaxCooldownReduction까지만 적용됨
        float total = Mathf.Max(GetPercentTotal(SkillPercentStat.Cooldown), -MaxCooldownReduction);

        // 시트 쿨타임이 0이어도 매 프레임 발동하지 않도록 하한을 둠
        return Mathf.Max(skillData.skillCooldown * (1.0f + total), 0.01f);
    }
}
