using System;
using UnityEngine;

public class BossProjectile : MonoBehaviour, IPoolable
{
    [SerializeField] private float hitRadius = 0.4f;   // 플레이어 중심과 이 거리 안이면 명중

    // IPoolable : 풀로 돌아가기 직전에 알림
    public event Action<GameObject> OnBeforeReturn;

    private Vector3 direction;
    private float speed;
    private int damage;
    private float lifeTimer;
    private Transform target;
    private PlayerHealth targetHealth;
    private bool isFlying;   // Launch 전이나 반납 후에는 움직이지 않게

    // 보스로부터 날아갈 방향과 피해 정보를 받아 사용
    public void Launch(Vector2 dir, float moveSpeed, int hitDamage, float lifeTime,
                       Transform targetTransform, PlayerHealth health)
    {
        direction = dir.normalized;
        speed = moveSpeed;
        damage = hitDamage;
        lifeTimer = lifeTime;
        target = targetTransform;
        targetHealth = health;

        transform.right = direction;   // 스프라이트의 오른쪽 = 진행 방향
        isFlying = true;
    }

    // IPoolable: 풀에서 꺼내질 때마다 호출됨(Launch가 불리기 전까지는 정지)
    public void OnSpawn()
    {
        isFlying = false;
    }

    // IPoolable: 풀로 돌아갈 때 정리(참조를 끊음)
    public void OnDespawn()
    {
        isFlying = false;
        target = null;
        targetHealth = null;
    }

    private void Update()
    {
        if (!isFlying) { return; }

        transform.position += direction * speed * Time.deltaTime;
        lifeTimer -= Time.deltaTime;

        // 거리 기반 명중 판정
        if (target != null && Vector2.Distance(transform.position, target.position) <= hitRadius)
        {
            targetHealth?.GetDamage(damage);
            ReturnToPool();
            return;
        }

        // 수명이 다하면 사라짐
        if (lifeTimer <= 0f) ReturnToPool();
    }

    // 오브젝트 풀에 정리
    private void ReturnToPool()
    {
        // 같은 프레임에 두 번 반납되는 것 방지
        if (!isFlying) { return; }   

        OnBeforeReturn?.Invoke(gameObject);
        OnDespawn();
        GameManager.ObjectPool.ReturnObject(gameObject);
    }
}
