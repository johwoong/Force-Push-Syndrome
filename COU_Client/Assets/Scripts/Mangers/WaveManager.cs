using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 스테이지의 웨이브(Wave.json)를 진행하고, 패턴(SpawnPattern.json)의 배치 형태로 위치를 정해
// MonsterSpawner에 스폰을 요청한다. 웨이브의 슬롯은 스테이지 로스터(StageRoster.json)가 몬스터와 배율로 채운다.
public class WaveManager : MonoBehaviour
{
    [SerializeField] private MonsterSpawner spawner;
    [SerializeField] private Transform player;

    [Header("Stage")]
    [SerializeField] private int stageId = 1;                // 메인 씬을 거치지 않을 때의 기본값

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 10f;        // player로부터의 거리
    [SerializeField] private float minRepeatInterval = 0.1f; // 매 프레임 스폰되지 않게
    [SerializeField] private float lineLength = 6f;          // Line 전체 길이
    [SerializeField] private float clusterRadius = 1.5f;     // Cluster 퍼짐 반경

    [Header("Test")]
    [SerializeField] private int testPatternId = 7;
    [SerializeField] private int testMonsterId = 11001;

    public StageData CurrentStage { get; private set; }
    public float Elapsed => elapsed;

    // 스폰 이벤트 진행 상태
    private class SpawnEventState
    {
        public WaveEntryData data;
        public SpawnPatternData pattern;
        public StageRosterData roster;
        public MonsterData monster;
        public float nextSpawnTime;
        public bool finished;
    }
    private readonly List<SpawnEventState> eventStates = new();

    // 스테이지 경과 시간
    private float elapsed;

    private void Start()
    {
        int targetStageId = stageId;
        if (GameManager.Scene != null && GameManager.Scene.TryConsumePendingStageId(out int selectedStageId))
        {
            targetStageId = selectedStageId;
        }

        SetupStage(targetStageId);
    }

    // 스테이지 시작
    public void SetupStage(int newStageId)
    {
        stageId = newStageId;
        GameManager.Sound.PlayBGM(GameConstants.Bgm.ForStage(stageId));   // 선택한 스테이지의 BGM (Stage{id})
        CurrentStage = GameManager.JsonData.GetStageDataFromJson(stageId);
        elapsed = 0f;

        eventStates.Clear();

        if (CurrentStage == null)   // 조회 실패 시 null (JsonDataManager가 경고 로그를 이미 찍음)
        {
            Debug.LogError($"[WaveManager] stageId {stageId}가 없어 웨이브를 시작할 수 없습니다.");
            return;
        }

        IReadOnlyList<StageRosterData> roster = GameManager.JsonData.GetStageRosterFromJson(stageId);
        if (roster == null)   // 조회 실패 시 null (JsonDataManager가 로그를 이미 찍음)
        {
            Debug.LogError($"[WaveManager] stageId {stageId}의 로스터가 없어 웨이브를 시작할 수 없습니다.");
            return;
        }
        int waveId = CurrentStage.waveId;

        // 전체를 훑으면서 이 스테이지 웨이브의 항목만 골라 담는다
        var allWaveEntries = GameManager.JsonData.WaveEntryDataDic;
        if (allWaveEntries == null)   // 로드 실패 시 null (JsonDataManager가 에러 로그를 이미 찍음)
        {
            Debug.LogError("[WaveManager] Wave 데이터가 없어 스테이지를 시작할 수 없습니다.");
            return;
        }
        foreach (WaveEntryData data in allWaveEntries.Values)
        {
            // 다른 웨이브 항목은 건너뜀
            if (data.waveId != waveId) { continue; }

            // 슬롯을 이 스테이지 로스터의 몬스터로 바꾼다
            StageRosterData rosterRow = roster.FirstOrDefault(row => row.monsterSlot == data.monsterSlot);
            if (rosterRow == null)
            {
                Debug.LogWarning($"[WaveManager] waveEntryId {data.waveEntryId}의 monsterSlot {data.monsterSlot}이(가) stageId {stageId} 로스터에 없어 건너뜁니다.");
                continue;
            }

            MonsterData monster = GameManager.JsonData.GetMonsterDataFromJson(rosterRow.monsterId);
            if (monster == null)
            {
                Debug.LogWarning($"[WaveManager] waveEntryId {data.waveEntryId}의 monsterSlot {data.monsterSlot} 몬스터 {rosterRow.monsterId}를 찾을 수 없어 건너뜁니다.");
                continue;
            }

            // 로스터의 patternId가 있으면 스테이지 특색 패턴, 0이면 웨이브 행의 패턴
            int patternId = rosterRow.patternId > 0 ? rosterRow.patternId : data.patternId;
            SpawnPatternData pattern = GameManager.JsonData.GetSpawnPatternDataFromJson(patternId);
            if (pattern == null)
            {
                string source = rosterRow.patternId > 0 ? $"stageRosterId {rosterRow.stageRosterId}" : $"waveEntryId {data.waveEntryId}";
                Debug.LogWarning($"[WaveManager] {source}의 patternId {patternId}를 찾을 수 없어 waveEntryId {data.waveEntryId}을(를) 건너뜁니다.");
                continue;
            }

            eventStates.Add(new SpawnEventState
            {
                data = data,
                pattern = pattern,
                roster = rosterRow,
                monster = monster,
                nextSpawnTime = data.patternStartTime,
                finished = false
            });
        }

        if (eventStates.Count == 0)
            Debug.LogWarning($"[WaveManager] stageId {stageId}의 waveId {waveId}에 항목이 없습니다.");
    }

    private void Update()
    {
        if (player == null) { return; }

        elapsed += Time.deltaTime;

        for (int i = 0; i < eventStates.Count; i++)
        {
            if (!eventStates[i].finished)
                ProcessEvent(eventStates[i]);
        }
    }

    // 이벤트 하나 처리(시간이 됐으면 스폰)
    private void ProcessEvent(SpawnEventState state)
    {
        WaveEntryData data = state.data;
        bool repeat = data.patternDuration > 0f;
        float interval = Mathf.Max(data.spawnInterval, minRepeatInterval);

        // 보스전(중간 보스 포함) 중에는 보스 몬스터가 아닌 스폰을 멈춤
        // 보스를 잡은 뒤 한꺼번에 몰려나오지 않게 보스전 동안 지나간 스폰은 건너뜀
        if (spawner.IsBossBattle && !state.monster.IsBoss)
        {
            if (elapsed >= state.nextSpawnTime)
            {
                if (repeat) state.nextSpawnTime = elapsed + interval;
                else state.finished = true;
            }
            return;
        }

        if (elapsed < state.nextSpawnTime) { return; }

        // 한 번만 나오는 이벤트(엘리트/보스)
        if (!repeat)
        {
            SpawnPattern(state.roster.monsterId, state.pattern, state.roster.monsterHpScale, state.roster.monsterDamageScale);
            state.finished = true;
            return;
        }

        // 반복 이벤트 : [patternStartTime, patternStartTime + patternDuration) 구간 동안 interval마다
        if (elapsed >= data.patternStartTime + data.patternDuration)
        {
            state.finished = true;
            return;
        }

        SpawnPattern(state.roster.monsterId, state.pattern, state.roster.monsterHpScale, state.roster.monsterDamageScale);
        state.nextSpawnTime += interval;
    }

    // 배치 형태에 따라 위치를 정해 spawnCount마리 스폰. hpScale, damageScale은 스테이지 로스터 배율
    private void SpawnPattern(int monsterId, SpawnPatternData pattern, float hpScale, float damageScale)
    {
        Vector3 center = player.position;
        int count = pattern.spawnCount;

        // 시트 값이 있으면 쓰고, 0이면 인스펙터 기본값
        float radius = pattern.spawnRadius > 0f ? pattern.spawnRadius : spawnRadius;
        float length = pattern.formationSize > 0f ? pattern.formationSize : lineLength;
        float spread = pattern.formationSize > 0f ? pattern.formationSize : clusterRadius;

        // Ring, Line, Cluster는 한 번 스폰할 때 방향을 하나만 뽑아 마리들이 나눠 쓴다
        Vector2 baseDir = RandomDirection();

        for (int i = 0; i < count; i++)
        {
            Vector3 position = pattern.Formation switch
            {
                SpawnFormation.Ring => center + Quaternion.Euler(0f, 0f, 360f * i / count) * baseDir * radius,
                SpawnFormation.Line => GetLinePosition(center, baseDir, i, count, radius, length),
                SpawnFormation.Cluster => center + (Vector3)(baseDir * radius + Random.insideUnitCircle * spread),
                SpawnFormation.Screen => GetRandomScreenPosition(),
                _ => center + (Vector3)(RandomDirection() * radius)
            };

            Enemy enemy = spawner.SpawnMonster(monsterId, position, hpScale, damageScale);

            // 돌진: 스폰 순간의 플레이어 위치로 직진해 스폰 거리의 2배를 간다
            // 엘리트·보스는 거리를 다 쓰면 OnBossKilled 없이 반납되므로 일반 몬스터만
            if (enemy != null && enemy.Type == MonsterType.Normal && pattern.Movement == SpawnMovement.Charge)
            {
                Vector2 toTarget = center - position;
                enemy.StartCharge(toTarget, toTarget.magnitude * 2f);
            }
        }
    }

    // 반지름 1인 원 위의 임의의 방향
    private static Vector2 RandomDirection()
    {
        return Random.insideUnitCircle.normalized;
    }

    // 중심 C = center + dir * radius에서 dir에 수직인 방향으로 양 끝을 포함해 같은 간격으로 length 안에 배치
    private Vector3 GetLinePosition(Vector3 center, Vector2 dir, int index, int count, float radius, float length)
    {
        Vector3 lineCenter = center + (Vector3)(dir * radius);
        if (count == 1) { return lineCenter; }

        Vector3 along = new Vector3(-dir.y, dir.x, 0f);
        float t = index / (float)(count - 1) - 0.5f;
        return lineCenter + along * (t * length);
    }

    // 카메라 화면 안의 랜덤 위치
    private Vector3 GetRandomScreenPosition()
    {
        Camera cam = Camera.main;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        Vector3 center = cam.transform.position;

        return new Vector3(
            center.x + Random.Range(-halfWidth, halfWidth),
            center.y + Random.Range(-halfHeight, halfHeight),
            0f);
    }

    // 타임라인과 무관하게 testPatternId 패턴을 testMonsterId로 즉시 한 번 스폰(배율 없이 원래 수치)
    [ContextMenu("TestSpawnPattern")]
    private void TestSpawnPattern()
    {
        SpawnPatternData pattern = GameManager.JsonData.GetSpawnPatternDataFromJson(testPatternId);
        if (pattern == null) { return; }

        SpawnPattern(testMonsterId, pattern, 1f, 1f);
    }
}
