using System.Collections.Generic;
using UnityEngine;

// 일반 몬스터. MonsterAttack 탭에 자기 monsterId 행이 있으면 그 공격을, 없으면 Enemy의 접촉 공격만 함(데이터 기반)
// 보스와 달리 예고(WindUp) 상태 머신이 없어서, 쿨타임이 끝나고 거리 조건이 맞으면 바로 발동
public class Monster : Enemy
{
    [Header("Ranged")]
    [SerializeField] private GameObject projectilePrefab;      // BossProjectile.prefab
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private float projectileLifeTime = 3f;

    [Header("Area")]
    [SerializeField] private GameObject areaWarningPrefab;     // BossAreaWarning.prefab
    [SerializeField] private float areaRadius = 1.2f;
    [SerializeField] private float areaDelay = 1f;             // 경고 후 폭발까지(초)
    [SerializeField] private float areaSpread = 2f;

    [Header("Trap")]
    [SerializeField] private GameObject trapPrefab;            // TrapZone.prefab
    [SerializeField] private float trapRadius = 0.8f;
    [SerializeField] private float trapDefaultDuration = 3f;   // 시트 값이 0일 때
    [SerializeField] private float trapSpread = 1f;

    // 이 몬스터의 공격들과 각 공격의 "다음 사용 가능 시각"(같은 인덱스끼리 짝)
    private readonly List<MonsterAttackData> attacks = new();
    private readonly List<float> readyTimes = new();

    protected override void OnInit()
    {
        attacks.Clear();
        readyTimes.Clear();

        foreach (MonsterAttackData attack in GameManager.JsonData.GetMonsterAttacks(Data.monsterId))
        {
            if (attack.AttackType == MonsterAttackType.Melee) { continue; }

            attacks.Add(attack);
            readyTimes.Add(Time.time + attack.monsterAttackCooldown);   // 스폰 직후 바로 쓰지 않게
        }
    }

    // Enemy.Update → Move, Attack(접촉) 다음에 매 프레임 호출됨
    protected override void Tick()
    {
        if (attacks.Count == 0 || playerHealth == null) { return; }

        float distance = Vector2.Distance(transform.position, player.transform.position);

        for (int i = 0; i < attacks.Count; i++)
        {
            if (Time.time < readyTimes[i]) { continue; }
            if (distance > attacks[i].monsterAttackTriggerRange) { continue; }

            Execute(attacks[i]);
            readyTimes[i] = Time.time + attacks[i].monsterAttackCooldown;
        }
    }

    private void Execute(MonsterAttackData attack)
    {
        switch (attack.AttackType)
        {
            case MonsterAttackType.Ranged: FireRanged(attack); break;
            case MonsterAttackType.Area: SpawnAreaWarnings(attack); break;
            case MonsterAttackType.Trap: SpawnTraps(attack); break;
        }
    }

    // 플레이어 방향을 가운데로 count발. angle이 360 이상이면 원형
    private void FireRanged(MonsterAttackData attack)
    {
        if (projectilePrefab == null) { Debug.LogWarning("[Monster] projectilePrefab이 연결되지 않았습니다."); return; }

        Vector2 baseDir = ((Vector2)(player.transform.position - transform.position)).normalized;
        if (baseDir == Vector2.zero) baseDir = Vector2.right;

        int count = Mathf.Max(1, attack.monsterAttackCount);
        float angle = attack.monsterAttackAngle;
        bool fullCircle = angle >= 360f;
        float stepAngle = count <= 1 ? 0f : (fullCircle ? 360f / count : angle / (count - 1));
        float startAngle = (count <= 1 || fullCircle) ? 0f : -angle / 2f;

        for (int i = 0; i < count; i++)
        {
            Vector2 dir = Quaternion.Euler(0f, 0f, startAngle + stepAngle * i) * baseDir;
            GameObject obj = GameManager.ObjectPool.GetObject(projectilePrefab, transform.position, Quaternion.identity);
            obj.GetComponent<BossProjectile>()?.Launch(
                dir, projectileSpeed, ScaleDamage(attack.monsterAttackDamage), projectileLifeTime,
                player.transform, playerHealth);
        }
    }

    // 첫 번째는 플레이어 발밑, 나머지는 주변에. 예고 연출과 폭발은 BossAreaWarning이 스스로 처리
    private void SpawnAreaWarnings(MonsterAttackData attack)
    {
        if (areaWarningPrefab == null) { Debug.LogWarning("[Monster] areaWarningPrefab이 연결되지 않았습니다."); return; }

        int count = Mathf.Max(1, attack.monsterAttackCount);
        for (int i = 0; i < count; i++)
        {
            Vector2 offset = i == 0 ? Vector2.zero : Random.insideUnitCircle * areaSpread;
            Vector3 position = player.transform.position + (Vector3)offset;

            GameObject obj = GameManager.ObjectPool.GetObject(areaWarningPrefab, position, Quaternion.identity);
            obj.GetComponent<BossAreaWarning>()?.Begin(
                areaRadius, areaDelay, ScaleDamage(attack.monsterAttackDamage), player.transform, playerHealth);
        }
    }

    // 첫 번째는 자기 발밑, 나머지는 주변에
    private void SpawnTraps(MonsterAttackData attack)
    {
        if (trapPrefab == null) { Debug.LogWarning("[Monster] trapPrefab이 연결되지 않았습니다."); return; }

        int count = Mathf.Max(1, attack.monsterAttackCount);
        float duration = attack.monsterAttackDuration > 0f ? attack.monsterAttackDuration : trapDefaultDuration;

        for (int i = 0; i < count; i++)
        {
            Vector2 offset = i == 0 ? Vector2.zero : Random.insideUnitCircle * trapSpread;
            Vector3 position = transform.position + (Vector3)offset;

            GameObject obj = GameManager.ObjectPool.GetObject(trapPrefab, position, Quaternion.identity);
            obj.GetComponent<TrapZone>()?.Begin(
                trapRadius, duration, ScaleDamage(attack.monsterAttackDamage), player.transform, playerHealth);
        }
    }

    // 풀로 돌아갈 때 정리(다른 monsterId로 재사용될 때 이전 공격이 남지 않게)
    protected override void OnDespawned()
    {
        attacks.Clear();
        readyTimes.Clear();
    }
}
