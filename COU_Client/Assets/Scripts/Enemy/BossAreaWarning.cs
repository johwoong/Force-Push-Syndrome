using System;
using UnityEngine;

public class BossAreaWarning : MonoBehaviour, IPoolable
{
    [SerializeField] private Color startColor = new(1f, 0f, 0f, 0.2f);   // 처음엔 연하게
    [SerializeField] private Color endColor = new(1f, 0f, 0f, 0.6f);     // 터지기 직전엔 진하게

    // IPoolable : 풀로 돌아가기 직전에 알림
    public event Action<GameObject> OnBeforeReturn;

    private SpriteRenderer spriteRenderer;
    private float radius;
    private float delay;
    private float elapsed;
    private int damage;
    private Transform target;
    private PlayerHealth targetHealth;
    private bool isActive;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    // 보스로부터 크기, 터질 때까지 시간, 피해 정보를 받아 사용
    public void Begin(float blastRadius, float explodeDelay, int hitDamage,
                      Transform targetTransform, PlayerHealth health)
    {
        radius = blastRadius;
        delay = explodeDelay;
        damage = hitDamage;
        target = targetTransform;
        targetHealth = health;
        elapsed = 0f;

        transform.localScale = Vector3.one * (radius * 2f);
        if (spriteRenderer != null) spriteRenderer.color = startColor;
        isActive = true;
    }

    // IPoolable: 풀에서 꺼내질 때마다 호출됨
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

        elapsed += Time.deltaTime;

        // 진행도 계산해서 색을 점점 진하게
        float t = delay > 0f ? Mathf.Clamp01(elapsed / delay) : 1f;
        if (spriteRenderer != null) spriteRenderer.color = Color.Lerp(startColor, endColor, t);

        if (elapsed >= delay) Explode();
    }

    // 폭발
    private void Explode()
    {
        // 거리 기반으로 터지는 순간 플레이어가 원 안에 있으면 피해
        if (target != null && Vector2.Distance(transform.position, target.position) <= radius)
        {
            targetHealth?.GetDamage(damage);
        }

        // TODO : 폭발 이펙트(지금은 원이 사라지기만 함)

        isActive = false;
        OnBeforeReturn?.Invoke(gameObject);
        OnDespawn();  
        GameManager.ObjectPool.ReturnObject(gameObject);
    }
}