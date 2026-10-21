using System;
using UnityEngine;

// 독 장판. 유지 시간 동안 남아 있고, 플레이어가 반지름 안에 있으면 tickInterval마다 피해를 줌.
// 피해 판정은 BossAreaWarning, BossProjectile과 같은 거리 기반이다.
public class TrapZone : MonoBehaviour, IPoolable
{
    [SerializeField] private float tickInterval = 1f;                  // 피해 간격(초). Enemy 접촉 공격과 같은 1초
    [SerializeField] private Color zoneColor = new(0.4f, 1f, 0.2f, 0.4f);

    // IPoolable : 풀로 돌아가기 직전에 알림
    public event Action<GameObject> OnBeforeReturn;

    private SpriteRenderer spriteRenderer;
    private float radius;
    private float remainTime;
    private float nextTickTime;
    private int damage;
    private Transform target;
    private PlayerHealth targetHealth;
    private bool isActive;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    // 몬스터로부터 크기, 유지 시간, 틱 피해를 받아 시작
    public void Begin(float zoneRadius, float duration, int tickDamage,
                      Transform targetTransform, PlayerHealth health)
    {
        radius = zoneRadius;
        remainTime = duration;
        damage = tickDamage;
        target = targetTransform;
        targetHealth = health;
        nextTickTime = 0f;   // 밟는 순간 바로 첫 피해

        transform.localScale = Vector3.one * (radius * 2f);   // BossAreaWarning과 같은 크기 규칙(스프라이트 지름 1 기준)
        if (spriteRenderer != null) spriteRenderer.color = zoneColor;
        isActive = true;
    }

    // IPoolable: 풀에서 꺼내질 때(Begin 전까지 정지)
    public void OnSpawn()
    {
        isActive = false;
    }

    // IPoolable: 풀로 돌아갈 때 정리(참조를 끊음)
    public void OnDespawn()
    {
        isActive = false;
        target = null;
        targetHealth = null;
    }

    private void Update()
    {
        if (!isActive) { return; }

        remainTime -= Time.deltaTime;
        if (remainTime <= 0f)
        {
            ReturnToPool();
            return;
        }

        if (Time.time < nextTickTime || target == null) { return; }

        if (Vector2.Distance(transform.position, target.position) <= radius)
        {
            targetHealth?.GetDamage(damage);
            nextTickTime = Time.time + tickInterval;
        }
    }

    // 오브젝트 풀에 정리 (BossAreaWarning과 같은 순서)
    private void ReturnToPool()
    {
        if (!isActive) { return; }   // 같은 프레임에 두 번 반납 방지

        OnBeforeReturn?.Invoke(gameObject);
        OnDespawn();
        GameManager.ObjectPool.ReturnObject(gameObject);
    }
}