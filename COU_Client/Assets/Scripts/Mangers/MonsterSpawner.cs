using System;
using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawner : MonoSingleton<MonsterSpawner>
{
    [Header("Prefab")]
    [SerializeField] private GameObject normalPrefab;      // 일반 몬스터용
    [SerializeField] private GameObject eliteBossPrefab;   // 엘리트·보스 공용
    [SerializeField] private GameObject boxPrefab;         // 상자용

    [Header("Player")]
    [SerializeField] private GameObject player;

    // 킬 수 집계용
    public event Action<Enemy> OnEnemyKilled;
    // 보스 연출/HUD용
    public event Action<Enemy> OnBossSpawned;
    public event Action<Enemy> OnBossKilled;

    // 스킬이 "가장 가까운 몬스터"를 찾을 때 사용
    public IReadOnlyList<Enemy> SpawnedEnemies => spawnedEnemies;
    public Enemy CurrentBoss { get; private set; }
    public bool IsBossBattle => CurrentBoss != null;

    // 현재 관리 중인 살아있는 몬스터
    private readonly List<Enemy> spawnedEnemies = new();

    // monsterId로 데이터 조회 -> 타입에 맞는 풀에서 꺼내 Init/SetTarget
    // hpScale, damageScale: 스테이지 로스터 배율
    public Enemy SpawnMonster(int monsterId, Vector3 position, float hpScale, float damageScale)
    {
        MonsterData data = GameManager.JsonData.GetMonsterDataFromJson(monsterId);
        if (data == null) { return null; }

        // 보스·중간 보스 등장(남아있는 몬스터를 모두 사망 처리(보상 없음))
        if (data.IsBoss)
            ClearAllEnemies(giveReward: false);

        // 풀(프리팹) 선택
        GameObject prefab = data.Type switch
        {
            MonsterType.Normal => normalPrefab,
            MonsterType.Box => boxPrefab,
            _ => eliteBossPrefab
        };

        // 오브젝트 풀에서 몬스터 가져오기
        GameObject obj = GameManager.ObjectPool.GetObject(prefab, position, Quaternion.identity);

        // 가져온 GameObject에서 Enemy 컴포넌트 가져오기
        Enemy enemy = obj.GetComponent<Enemy>();

        // 몬스터가 추적할 플레이어 연결
        enemy.Init(data, hpScale, damageScale);
        enemy.SetTarget(player);

        // 반납 알림 구독 → 죽으면 목록에서 제거
        // (재사용할 때 중복 구독되지 않도록 먼저 빼고 다시 등록)
        enemy.OnBeforeReturn -= OnEnemyReturned;
        enemy.OnBeforeReturn += OnEnemyReturned;

        enemy.OnDied -= OnEnemyDied;
        enemy.OnDied += OnEnemyDied;

        // 현재 spawner가 관리하는 몬스터 목록에 추가
        spawnedEnemies.Add(enemy);

        if (enemy.IsBoss)
        {
            CurrentBoss = enemy;
            OnBossSpawned?.Invoke(enemy);
        }

        return enemy;
    }

    // 살아 있는 몬스터 전부 사망 처리
    public void ClearAllEnemies(bool giveReward)
    {
        Enemy[] targets = spawnedEnemies.ToArray();
        foreach (Enemy enemy in targets)
        {
            // 상자는 보스 등장에도 지우지 않음
            if (enemy.Type == MonsterType.Box) { continue; }
            enemy.Die(giveReward);
        }
    }

    // 처치 알림
    private void OnEnemyDied(Enemy enemy)
    {
        OnEnemyKilled?.Invoke(enemy);

        if (enemy == CurrentBoss)
        {
            CurrentBoss = null;          // 보스전 종료 → 일반 스폰 재개
            OnBossKilled?.Invoke(enemy);
        }
    }

    // 몬스터가 풀로 돌아가기 직전 목록에서 제거
    private void OnEnemyReturned(GameObject obj)
    {
        // 반납 알림을 보낸 GameObject에서 Enemy 컴포넌트 가져오기
        Enemy enemy = obj.GetComponent<Enemy>();

        // 스포너가 관리하는 목록에서 해당 몬스터 제거
        spawnedEnemies.Remove(enemy);

        // 보스가 처치가 아닌 이유로 반납된 경우도 보스전 상태를 풀어줌
        if (enemy == CurrentBoss) CurrentBoss = null;
    }
}
