using System.Collections.Generic;
using UnityEngine;

// 사용자를 따라다니며 바라보는 방향(PlayerMovement.FacingDirection)에 범위를 계속 유지하는 오브젝트 (럼블 Q처럼 앞쪽 지속 범위)
// 판정 시점은 스킬이 정함: 스킬이 틱마다 Hit을 부르면 그 순간 콜라이더와 겹친 적 모두에게 한 번씩 피해를 줌 (넉백 없음)
// 풀에 스스로 반환되지 않으며, 스킬이 해제될 때 스킬이 반환함
// _loopSfx를 지정하면 살아 있는 동안 반복 재생하고, 틱이 빨라질수록 피치를 올림 (풀로 돌아가면 멈춤)
// 범위의 위치/크기는 프리팹 콜라이더로 정함 (오브젝트 pivot = 사용자 위치, 로컬 right(+X) = 바라보는 방향)
public class FacingFieldObject : SkillAttackObject<FacingFieldData>
{
    [SerializeField] private Collider2D _hitArea;

    [Header("반복 효과음 (비워 두면 재생하지 않음)")]
    [SerializeField] private AudioClip _loopSfx;

    [Tooltip("클립이 원래 속도(피치 1)일 때 한 번 휘두르는 데 걸리는 시간(초). 틱 간격이 이 값과 같으면 피치 1, 틱이 빨라질수록 이 값 ÷ 틱 간격만큼 피치가 올라감")]
    [SerializeField] private float _loopReferenceTickInterval = 0.2f;

    [SerializeField] private float _loopMinPitch = 0.8f;
    [SerializeField] private float _loopMaxPitch = 1.6f;

    private readonly List<Collider2D> _overlaps = new();
    private readonly HashSet<Enemy> _hitEnemies = new();

    private ContactFilter2D _filter;
    private Vector3 _baseScale;
    private bool _initialized;

    private Transform _owner;
    private PlayerMovement _movement;

    private SfxLoopHandle _loopHandle;
    private float _tickInterval;

    protected override void OnInit(FacingFieldData data)
    {
        EnsureInitialized();

        stats = data.Stats;
        _owner = data.Owner;
        _movement = data.Movement;

        StartLoopSfx();
        OnLaunch();
    }

    protected override void OnLaunch()
    {
        UpdateTransform();
    }

    // 스킬이 틱마다 부름. 그 순간의 수치(대미지, 범위 증가)로 겹친 적 모두에게 한 번씩 피해를 줌
    // tickInterval은 다음 틱까지의 간격(게임 시간 초). 반복 효과음의 피치를 정하는 데 씀
    public void Hit(SkillStats tickStats, float tickInterval)
    {
        stats = tickStats;
        _tickInterval = tickInterval;
        UpdateTransform();

        if(_hitArea == null)
        {
            return;
        }

        // 방금 옮기고 회전한 상태가 물리에 반영된 상태에서 판정
        Physics2D.SyncTransforms();

        _overlaps.Clear();
        _hitArea.Overlap(_filter, _overlaps);
        _hitEnemies.Clear();

        foreach(Collider2D other in _overlaps)
        {
            // 콜라이더가 여러 개인 적에게 한 틱에 중복 피해가 들어가지 않도록 함
            if(!other.TryGetComponent(out Enemy enemy) || !_hitEnemies.Add(enemy))
            {
                continue;
            }

            OnHitEnemy(enemy);
        }
    }

    // 판정에 걸린 적 하나에 주는 효과. 지속 범위는 틱마다 넉백하면 적이 범위 밖으로 밀려나므로, 인스펙터의 넉백은 기본(꺼짐)으로 두는 것을 권장
    protected virtual void OnHitEnemy(Enemy enemy)
    {
        PlayerDamageCalculator.HitResult hit = stats.CalculateHit();

        enemy.Damaged(hit.damage, stats.SkillId, KnockbackEnabled);
        SpawnOnHit(enemy, stats);
    }

    // 사용자의 이동이 끝난 뒤의 위치와 방향에 맞추기 위해 LateUpdate에서 갱신
    private void LateUpdate()
    {
        UpdateTransform();
        UpdateLoopPitch();
    }

    // 풀로 돌아가거나 파괴될 때 반복 효과음을 멈춤 (스킬 해제, 씬 이동)
    private void OnDisable()
    {
        StopLoopSfx();
    }

    private void StartLoopSfx()
    {
        StopLoopSfx();

        if(_loopSfx == null)
        {
            return;
        }

        // 첫 틱이 오기 전에는 기준 간격으로 간주해 피치 1에서 시작
        _tickInterval = _loopReferenceTickInterval;
        _loopHandle = GameManager.Sound.PlayLoopSFX(_loopSfx, 1.0f, GetLoopPitch());
    }

    private void StopLoopSfx()
    {
        if(!_loopHandle.IsValid)
        {
            return;
        }

        // 씬 정리나 종료 중에는 SoundManager가 먼저 사라졌을 수 있고, Instance로 접근하면 새로 만들어 버리므로 있을 때만 정지
        if(SoundManager.HasInstance)
        {
            GameManager.Sound.StopLoopSFX(_loopHandle);
        }

        _loopHandle = SfxLoopHandle.None;
    }

    // 게임 배속(Time.timeScale)이 바뀌어도 소리가 휘두르는 속도를 따라가도록 매 프레임 반영
    private void UpdateLoopPitch()
    {
        if(_loopHandle.IsValid)
        {
            GameManager.Sound.SetLoopPitch(_loopHandle, GetLoopPitch());
        }
    }

    // 틱 간격이 짧을수록 높은 피치. 소리는 실시간으로 재생되므로 게임 배속을 곱함
    private float GetLoopPitch()
    {
        if(_tickInterval <= 0.0f)
        {
            return _loopMaxPitch;
        }

        float pitch = _loopReferenceTickInterval / _tickInterval * Time.timeScale;

        return Mathf.Clamp(pitch, _loopMinPitch, _loopMaxPitch);
    }

    private void UpdateTransform()
    {
        if(_owner == null)
        {
            return;
        }

        transform.position = _owner.position;

        // 범위 증가 보너스는 프리팹 크기에 곱해서 스프라이트와 판정이 함께 커지게 함
        Vector3 scale = _baseScale * stats.Scale;
        Vector2 facing = _movement != null ? _movement.FacingDirection : Vector2.right;

        if(facing != Vector2.zero)
        {
            transform.right = facing;

            // 왼쪽을 향하면 180도 회전해서 스프라이트가 뒤집혀 보이므로 Y 스케일을 반전해 바로 세움
            if(facing.x < 0.0f)
            {
                scale.y = -scale.y;
            }
        }

        transform.localScale = scale;
    }

    // 풀에서 꺼낸 직후엔 Awake가 아직 불리지 않았을 수 있으므로 첫 Init에서 한 번만 준비
    private void EnsureInitialized()
    {
        if(_initialized)
        {
            return;
        }

        _initialized = true;
        _baseScale = transform.localScale;

        _filter = new ContactFilter2D();
        _filter.SetLayerMask(LayerMask.GetMask("Enemy"));
        _filter.useTriggers = true;

        if(_hitArea == null && !TryGetComponent(out _hitArea))
        {
            Debug.LogWarning($"[FacingFieldObject] 판정용 콜라이더가 없습니다: {name}");
        }
    }
}
