using UnityEngine;

// 주인과 대상 적 사이에 늘어나는 번개. 생성되는 순간 대상에게 대미지를 주고, 잠깐 보인 뒤 풀에 반환
// 프리팹은 pivot을 가운데에 두고 오른쪽(+X)으로 길이 1인 모양으로 만듦 (X 스케일 = 두 지점 사이 거리)
public sealed class LightningObject : SkillAttackObject<LinkData>
{
    // 번개가 보이는 시간 (대미지는 생성 즉시 들어감)
    [SerializeField] private float _displayTime = 0.2f;

    private Transform _owner;
    private Transform _target;
    private Vector3 _ownerPosition;
    private Vector3 _targetPosition;
    private Vector3 _baseScale;
    private bool _initialized;
    private float _elapsed;

    // 타격 시 소환되면 스킬 사용자에서 맞은 적까지 이어지는 번개가 생김
    public override bool CanSpawnOnHit => true;

    public override void InitFromHit(HitSpawnData hit)
    {
        Init(new LinkData(hit.Stats, hit.Owner, hit.Target));
    }

    protected override void OnInit(LinkData data)
    {
        // 풀에서 다시 꺼낼 때 바뀐 스케일이 아니라 프리팹 원래 스케일을 기준으로 삼기 위해 첫 Init에서 한 번만 저장
        if(!_initialized)
        {
            _initialized = true;
            _baseScale = transform.localScale;
        }

        stats = data.Stats;
        _owner = data.Owner;
        _target = data.Target;
        _ownerPosition = _owner != null ? _owner.position : transform.position;
        _targetPosition = _target != null ? _target.position : _ownerPosition;

        OnLaunch();
    }

    protected override void OnLaunch()
    {
        _elapsed = 0.0f;

        if(_target != null && _target.TryGetComponent(out Enemy enemy))
        {
            PlayerDamageCalculator.HitResult hit = stats.CalculateHit();

            enemy.Damaged(hit.damage, stats.SkillId, KnockbackEnabled);
            SpawnOnHit(enemy, stats);
        }

        UpdateShape();
    }

    // 이동이 끝난 뒤의 위치로 번개를 맞추기 위해 LateUpdate에서 갱신
    private void LateUpdate()
    {
        _elapsed += Time.deltaTime;

        if(_elapsed >= _displayTime)
        {
            ReturnToPool();
            return;
        }

        UpdateShape();
    }

    private void UpdateShape()
    {
        // 대상이 죽어 풀로 돌아가면 마지막 위치에 고정
        if(_owner != null && _owner.gameObject.activeInHierarchy)
        {
            _ownerPosition = _owner.position;
        }

        if(_target != null && _target.gameObject.activeInHierarchy)
        {
            _targetPosition = _target.position;
        }

        Vector3 delta = _targetPosition - _ownerPosition;
        delta.z = 0.0f;

        float distance = delta.magnitude;

        transform.position = (_ownerPosition + _targetPosition) * 0.5f;

        if(distance > 0.0001f)
        {
            transform.right = delta / distance;
        }

        // 길이는 거리에 맞추고, 범위 증가 보너스는 두께에만 반영
        transform.localScale = new Vector3(distance, _baseScale.y * stats.Scale, _baseScale.z);
    }
}
