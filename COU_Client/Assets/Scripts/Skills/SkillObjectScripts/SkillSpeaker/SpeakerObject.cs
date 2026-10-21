using System.Collections.Generic;
using UnityEngine;

// 플레이어 주위를 도는 스피커. 풀에 스스로 반환되지 않고 스킬 해제 때까지 유지됨
// SkillStats: Speed = 초당 회전 각도, Damage = 접촉 대미지, Scale = 크기 배율 / OrbitData: Radius = 회전 반지름
public sealed class SpeakerObject : SkillAttackObject<OrbitData>
{
    // 기록이 이 개수 이상 쌓이면 시간이 지난 기록을 정리
    private const int PurgeThreshold = 64;

    // 한 번 때린 적을 다시 때리기까지 최소 시간의 상한 (스친 직후 콜라이더에 다시 들어와도 중복 대미지 방지)
    // 회전이 빨라 한 바퀴가 이 시간보다 짧으면 실제 간격은 한 바퀴 시간에 맞춰 짧아짐 (GetHitInterval)
    [SerializeField] private float _hitInterval = 0.5f;

    // 적별 마지막 타격 시각 (Enemy 대신 InstanceID를 키로 써서 죽은 적의 참조를 붙들지 않음)
    // 실제 타격 간격은 한 바퀴 시간(360 ÷ 회전 속도)의 이 비율을 넘지 않음 (0.5 = 한 바퀴에 적어도 두 번은 때릴 수 있음)
    private const float RevolutionIntervalRatio = 0.5f;

    // 타격 간격의 하한 (회전이 매우 빨라도 같은 적에게 한 프레임에 여러 번 들어가지 않도록)
    private const float MinHitInterval = 0.05f;

    private readonly Dictionary<int, float> _lastHitTimes = new();
    private readonly List<int> _expiredIds = new();

    private Transform _owner;
    private float _angle;
    private float _radius;

    // 풀에서 다시 꺼낼 때 바뀐 크기가 아니라 프리팹 원래 크기를 기준으로 삼기 위해 첫 Init에서 한 번만 저장
    private Vector3 _baseScale;
    private bool _initialized;

    protected override void OnInit(OrbitData data)
    {
        if(!_initialized)
        {
            _initialized = true;
            _baseScale = transform.localScale;
        }

        stats = data.Stats;

        // 범위 증가 보너스는 프리팹 크기에 곱해서 스프라이트와 판정이 함께 커지게 함
        transform.localScale = _baseScale * stats.Scale;
        _owner = data.Owner;
        _angle = data.StartAngle;
        _radius = data.Radius;
        _lastHitTimes.Clear();
        OnLaunch();
    }

    protected override void OnLaunch()
    {
        UpdatePosition();
    }

    private void Update()
    {
        if(_owner == null)
        {
            return;
        }

        _angle += stats.Speed * Time.deltaTime;
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if(_owner == null)
        {
            return;
        }

        float radian = _angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(radian), Mathf.Sin(radian), 0.0f) * _radius;

        transform.position = _owner.position + offset;
    }

    // 적을 스치는 순간 한 번만 대미지 (닿아 있는 동안 반복 대미지는 주지 않음)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if(!other.TryGetComponent(out Enemy enemy))
        {
            return;
        }

        int enemyId = enemy.GetInstanceID();
        float now = Time.time;
        float hitInterval = GetHitInterval();

        if(_lastHitTimes.TryGetValue(enemyId, out float lastHitTime) && now - lastHitTime < hitInterval)
        {
            return;
        }

        if(_lastHitTimes.Count >= PurgeThreshold)
        {
            PurgeExpiredRecords(now, hitInterval);
        }

        _lastHitTimes[enemyId] = now;

        PlayerDamageCalculator.HitResult hit = stats.CalculateHit();

        // 대미지 확인용 임시 로그 (확인이 끝나면 삭제)
        Debug.Log($"[Speaker#{GetInstanceID()}] -> Enemy#{enemyId} dmg={hit.damage} crit={hit.isCritical} frame={Time.frameCount} time={now:F2}");

        enemy.Damaged(hit.damage, stats.SkillId, KnockbackEnabled);
        PlayHitSfx();
        SpawnOnHit(enemy, stats);
    }

    // 같은 적을 다시 때릴 수 있기까지의 시간. 속도 증가로 한 바퀴가 _hitInterval보다 짧아지면 같은 구체가 다시 지나가도 막히므로, 한 바퀴 시간에 맞춰 줄임
    // 한 바퀴 시간 = 360 ÷ 회전 속도(초당 각도). 회전 속도가 느려 한 바퀴가 길면 _hitInterval 그대로
    private float GetHitInterval()
    {
        float speed = Mathf.Abs(stats.Speed);

        if(speed <= 0.0f)
        {
            return _hitInterval;
        }

        float revolutionTime = 360.0f / speed;

        return Mathf.Max(Mathf.Min(_hitInterval, revolutionTime * RevolutionIntervalRatio), MinHitInterval);
    }

    // 쿨타임이 지난 기록을 제거해서 죽은 적의 기록이 계속 쌓이지 않게 함
    private void PurgeExpiredRecords(float now, float hitInterval)
    {
        _expiredIds.Clear();

        foreach(KeyValuePair<int, float> pair in _lastHitTimes)
        {
            if(now - pair.Value >= hitInterval)
            {
                _expiredIds.Add(pair.Key);
            }
        }

        foreach(int id in _expiredIds)
        {
            _lastHitTimes.Remove(id);
        }
    }
}
