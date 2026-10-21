using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Enemy : MonoBehaviour, IPoolable
{
    [Header("Contact Attack")]
    [SerializeField] protected float attackRange = 0.6f;    // 이 거리 안이면 닿은 것으로 봄
    [SerializeField] protected float attackInterval = 1f;   // 접촉 피해 간격(초)

    [Header("Animation")]
    [SerializeField] private float hitAnimDuration = 0.2f;    // Hit 후 Walk 복귀까지(초)
    [SerializeField] private float deadAnimDuration = 0.5f;   // Dead 모션 후 풀 반납까지(초)

    [Header("Charge")]
    [SerializeField] private float chargeSpeedMultiplier = 2f;   // 돌진 속도 = monsterMoveSpeed × 배율

    [Header("Knockback")]
    [SerializeField] private float knockbackDistance = 0.3f;   // 한 번에 밀리는 거리 (0이면 제자리 경직)
    [SerializeField] private float knockbackDuration = 0.1f;   // 밀리는 시간(초)
    [SerializeField] private float knockbackCooldown = 0.25f;  // 다시 밀릴 수 있기까지(초, 연속 피격 시 계속 밀리지 않게)

    [Header("Damage Text")]
    [SerializeField] private Vector3 damageTextOffset = new(0f, 1f, 0f);

    public MonsterData Data { get; private set; }
    // 스테이지 로스터 배율을 적용한 최대 체력
    public int MaxHp { get; private set; }

    // 몬스터의 개별 상태
    public int currentHp;
    public bool isDead;
    private float nextAttackTime;
    // 스테이지 로스터의 피해 배율
    private float damageScale;
    // 돌진 방향과 남은 돌진 거리. 남은 거리가 0이면 추적
    private Vector3 chargeDirection;
    private float chargeRemaining;
    // 몬스터 외형 교체(monsterAsset)
    protected SpriteRenderer spriteRenderer;
    protected EnemyVisual visual;      // 외형
    private Collider2D hitCollider;
    private float hitEndTime;

    private Vector3 knockbackVelocity;   // 넉백 중 이동 속도
    private float knockbackEndTime;      // 이 시각까지 넉백 중
    private float nextKnockbackTime;     // 이 시각 이후에 다시 넉백 가능

    // 추적할 Player
    public GameObject player;
    protected PlayerHealth playerHealth;

    // IPoolable : 풀로 돌아가기 직전에 알림 (스포너가 구독해서 사용)
    public event Action<GameObject> OnBeforeReturn;
    // 몬스터 "처치됨" 알림(보스 등장시 몬스터들 사라질때를 위함)
    public event Action<Enemy> OnDied;
    // 보스 HP 바 갱신용: InGameHUD.SetBossHp(enemy.HpRatio)
    public event Action<Enemy> OnHpChanged;

    // 마지막으로 피해를 준 스킬. 0이면 스킬이 아닌 피해(폭탄 등)
    public long LastHitSkillId { get; private set; }
    
    public MonsterType Type => Data != null ? Data.Type : MonsterType.Normal;
    public bool IsBoss => Data != null && Data.IsBoss;
    public float HpRatio => MaxHp <= 0 ? 0f : (float)currentHp / MaxHp;

    // 등급별 넉백 저항(0 = 100% 밀림, 1 = 안 밀림) 기본값
    protected virtual float KnockbackResistance => Type switch
    {
        MonsterType.Normal => 0f,
        MonsterType.Elite => 0.5f,
        _ => 1f      // Boss, Box: 밀리지 않음
    };

    protected virtual void Awake()
    {
        spriteRenderer = gameObject.GetComponentInChildren<SpriteRenderer>();
        visual = GetComponent<EnemyVisual>();
        hitCollider = GetComponent<Collider2D>();
    }

    // 몬스터 초기화. 배율은 스테이지 로스터 값(체력 최소 1)
    public void Init(MonsterData data, float hpScale, float damageScale)
    {
        Data = data;
        this.damageScale = damageScale;
        MaxHp = Mathf.Max(1, Mathf.RoundToInt(data.monsterMaxHealthPoint * hpScale));
        currentHp = MaxHp;
        isDead = false;
        LastHitSkillId = 0; // 풀에서 재사용되므로 초기화
        nextAttackTime = 0f;
        if (visual != null)
        {
            visual.Apply(data.monsterAsset);
        }
        else
        {
            Sprite sprite = Resources.Load<Sprite>(data.monsterAsset);
            if (sprite != null) spriteRenderer.sprite = sprite;
        }

        // 풀 재사용 대비 초기화
        if (hitCollider != null) hitCollider.enabled = true;
        hitEndTime = 0f;

        chargeRemaining = 0f;   // 풀 재사용 시 이전 돌진이 남지 않게

        // 풀 재사용 시 이전 넉백이 남지 않게
        knockbackEndTime = 0f;
        nextKnockbackTime = 0f;
        knockbackVelocity = Vector3.zero;

        OnInit();
    }
    // 몬스터 초기화(자식용)
    protected virtual void OnInit() { }

    // player와 PlayerHealth 연결(Init 시점엔 player가 비어 있으므로 따로)
    public void SetTarget(GameObject target)
    {
        player = target;
        playerHealth = target != null ? target.GetComponent<PlayerHealth>() : null;
    }

    // direction으로 distance만큼 직진한 뒤 보상 없이 반납된다
    public void StartCharge(Vector2 direction, float distance)
    {
        chargeDirection = direction.normalized;
        if (chargeDirection.sqrMagnitude < 0.0001f) chargeDirection = Vector3.right;   // 완전히 겹쳐 있을 때 대비
        chargeRemaining = distance;

        // 돌진 시 방향 고정
        visual?.FaceTo(chargeDirection.x);
    }

    // IPoolable: 풀에서 꺼내질 때마다 호출됨
    public void OnSpawn()
    {

    }

    // IPoolable: 풀로 돌아갈 때 정리(참조를 끊음)
    public void OnDespawn()
    {
        player = null;
        playerHealth = null;

        OnDespawned();
    }
    // 풀로 돌아갈 때 정리(자식용)
    protected virtual void OnDespawned() { }

    protected void Update()
    {
        // 몬스터가 죽은 상태면 Data/player가 없으면 return
        if (isDead || Data == null || player == null) { return; }

        // 컨트롤러에 전환(Transition)이 없어서, Hit가 끝나면 코드가 Walk로 되돌림
        if (hitEndTime > 0f && Time.time >= hitEndTime)
        {
            hitEndTime = 0f;
            visual?.PlayWalk();
        }

        // 넉백 중이면 추적(Move) 대신 밀려나는 이동만 함. 끝나면 다시 추적
        if (Time.time < knockbackEndTime)
        {
            transform.position += knockbackVelocity * Time.deltaTime;
        }
        else
        {
            Move();
        }
        // 돌진을 마쳐 반납된 프레임에는 공격하지 않음
        if (isDead) { return; }
        Attack();
        Tick();
    }
    // 매 프레임 할 일(자식용)
    protected virtual void Tick() { }

    // 몬스터의 움직임
    protected virtual void Move()
    {
        // 몬스터가 죽은 상태면 return
        if (isDead) { return; }

        // 돌진 중이면 정해진 방향으로 직진, 거리를 다 쓰면 보상·킬 수 없이 반납
        if (chargeRemaining > 0f)
        {
            float step = Data.monsterMoveSpeed * chargeSpeedMultiplier * Time.deltaTime;
            this.transform.position += chargeDirection * step;
            chargeRemaining -= step;

            if (chargeRemaining <= 0f) Die(false);
            return;
        }

        // player쪽 방향 찾기
        Vector3 direction = player.transform.position - this.transform.position;
        direction.Normalize();

        this.transform.position += direction * Data.monsterMoveSpeed * Time.deltaTime;

        visual?.FaceTo(direction.x);
    }

    // 몬스터의 공격(닿아 있는 동안 attackInterval마다 피해)
    protected virtual void Attack()
    {
        // 몬스터가 죽은 상태면 return
        if (isDead || playerHealth == null) { return; }
        // 몬스터 다음 공격 시간보다 덜 지났으면 return
        if (Time.time < nextAttackTime) { return; }

        float distance = Vector2.Distance(transform.position, player.transform.position);
        if (distance > attackRange) { return; }

        playerHealth.GetDamage(ScaleDamage(Data.monsterContactDamage));
        nextAttackTime = Time.time + attackInterval;
    }

    // 피해 배율 적용. 원래 값이 0보다 크면 최소 1
    protected int ScaleDamage(int baseDamage)
        => baseDamage <= 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(baseDamage * damageScale));

    // 몬스터의 피격
    // applyKnockback: false면 넉백 없이 피해만 줌 (기본값 true로 기존 호출은 그대로 넉백). 보스와 상자는 true여도 넉백 저항 때문에 밀리지 않음
    public virtual void Damaged(int damage, long sourceSkillId = 0, bool applyKnockback = true)
    {
        // 몬스터가 죽은 상태면 return
        if (isDead) { return; }

        LastHitSkillId = sourceSkillId;
        currentHp -= damage;
        // 보스 HP 바 갱신
        OnHpChanged?.Invoke(this);   

        DamageTextSpawner.Instance?.Show(damage, transform.position + damageTextOffset);

        // 몬스터가 죽으면
        if (currentHp <= 0)
        {
            Die();
            return;
        }

        if (visual != null && visual.HasAnimator)
        {
            visual.PlayHit();
            hitEndTime = Time.time + hitAnimDuration;
        }

        // 살아남았을 때만 넉백 (호출한 쪽이 넉백을 끈 피해는 제외)
        if (applyKnockback)
        {
            ApplyKnockback();
        }
    }

    // 플레이어 반대 방향으로 knockbackDuration 동안 (거리 × 저항 반영)만큼 밀어냄
    private void ApplyKnockback()
    {
        float power = 1f - KnockbackResistance;
        if (power <= 0f) { return; }                        // 보스, 상자
        if (chargeRemaining > 0f) { return; }               // 돌진 중에는 끊지 않음
        if (Time.time < nextKnockbackTime) { return; }      // 쿨타임 중
        if (player == null) { return; }

        Vector3 direction = transform.position - player.transform.position;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector3.right;   // 완전히 겹쳤을 때 대비
        direction.Normalize();

        // 거리 = 속력 × 시간 → 속력 = 거리 ÷ 시간
        float speed = knockbackDistance * power / Mathf.Max(knockbackDuration, 0.01f);
        knockbackVelocity = direction * speed;
        knockbackEndTime = Time.time + knockbackDuration;
        nextKnockbackTime = Time.time + knockbackCooldown;
    }

    // 스킬이 아닌 즉사(폭탄 등). 마지막 타격 스킬을 지워서 스킬 처치 업적에 집계되지 않음
    public void Kill()
    {
        LastHitSkillId = 0;
        Die();
    }

    // 몬스터가 죽었을 때
    public void Die(bool giveReward = true)
    {
        // 몬스터가 죽은 상태면 return
        if (isDead) { return; }

        isDead = true;

        // 보상 (giveReward)
        // - true  (기본) : 플레이어가 잡음 → 경험치 드롭 + OnDied(킬 수 집계)
        // - false        : 보스 등장 정리 등 "강제로 치움" → 보상/킬 없이 반납만
        if (giveReward) { GiveReward(); }

        // 반납 알림
        OnBeforeReturn?.Invoke(gameObject);

        // 몬스터가 죽은 상태면 비활성화(Pool로 반납)
        // 처치 + 애니메이션 외형이면 Dead 모션 후 반납. 보스 등장 정리(giveReward=false)는 즉시 반납
        if (giveReward && visual != null && visual.HasAnimator)
        {
            if (hitCollider != null) hitCollider.enabled = false;
            visual.PlayDead();
            StartCoroutine(ReturnAfter(deadAnimDuration));
        }
        else
        {
            GameManager.ObjectPool.ReturnObject(gameObject);
        }
    }

    // 처치 보상(자식용): 드롭 + 킬 수 집계
    protected virtual void GiveReward()
    {
        GameManager.DropItem.SpawnTable(Type, transform.position);

        OnDied?.Invoke(this);
    }

    private IEnumerator ReturnAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        GameManager.ObjectPool.ReturnObject(gameObject);
    }

    // 테스트용 피격, 죽음
    [ContextMenu("TestDamaged")]
    private void TestDamaged() => Damaged(5);

    [ContextMenu("TestKill")]
    private void TestKill() => Die();
}
