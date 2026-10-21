# WorkPlan 04: 웨이브 매니저 (WaveManager)

> 후속 변경(스테이지·웨이브 분리, 맨 아래 절)을 "구조 개요"와 "합의된 구현 결정"에 반영했다. 목표, 범위, 작업 항목, 검증 항목, 검증 결과는 최초 작업 기준이다.

## 목표
- WaveManager를 추가해 스테이지 타임라인 진행과 스폰 배치를 맡긴다. MonsterSpawner는 몬스터를 생성하고 관리하는 실행기로 남긴다.
- 스폰패턴(배치 형태 + 타이밍)을 재사용 가능한 테이블로 정의하고, 패턴마다 드롭 테이블을 연결한다.
- 하드코딩된 드롭을 드롭 테이블로 옮긴다. 대상은 `Enemy.GiveReward`의 ExpGem1 고정과 엘리트·보스 상자 수, `Box.dropWeights`이다.
- 입력: 유저가 직접 제시한 요구("웨이브 매니저, 스폰패턴, 패턴별 드롭아이템테이블"). 별도 기획명세는 없다.

## 범위
**포함**
- 데이터: `SpawnPattern.json`(신규), `DropTable.json`(신규), `Spawn.json`(타임라인 형식으로 재작성)과 각 데이터 클래스, 로더
- WaveManager: 타임라인 진행, 보스전 중 정지, 배치 형태 5종(Random, Ring, Line, Cluster, Screen)
- MonsterSpawner 축소: 타임라인과 위치 계산을 WaveManager로 옮긴다
- Enemy·Box: 드롭을 드롭 테이블 판정으로 교체
- DropItemManager: 드롭 테이블 판정 API
- BattleManager: `CurrentStage`를 WaveManager에서 읽도록 변경
- JsonDataManagerEditor: 축소된 SpawnEventData에 맞춰 스폰 이벤트 표시 수정
- BattleScene 배치, 검증, 데이터 문서 갱신

**제외**
- 스폰 뒤 이동 패턴(돌진, 직진 등). 모든 몬스터는 스폰 뒤 지금처럼 플레이어를 추적한다.
- 새 배치 형태(Ring/Line/Cluster)를 스테이지 타임라인에 넣는 기획. 샘플 패턴은 ContextMenu 검증용으로만 정의한다.
- 경험치잼 등급 분포 조정. 이관할 때는 현재 동작대로 ExpGem1을 유지한다.
- 스테이지 2 이후의 타임라인 데이터
- `DropItemType` enum, DropItemManager의 ECS 경로
- `Monster.json`의 monsterExp 정리
- JsonDataManagerEditor에 SpawnPattern·DropTable 표시 추가
- `Scenes/TestScenes/enemy.unity` 연결 (승희님과 협의, 미해결 참고)

## 사전 조건
- **다른 담당자 파일 수정**: `MonsterSpawner.cs`, `Enemy.cs`, `Box.cs`는 승희님 담당이다. 작업 전에 승희님에게 아래 공개 API 변경을 알린다.

| 변경 | 이전 | 이후 |
|---|---|---|
| 스폰 호출 | `MonsterSpawner.SpawnMonster(int monsterId)` | `SpawnMonster(int monsterId, Vector3 position, int dropTableId)` |
| 몬스터 초기화 | `Enemy.Init(MonsterData data)` | `Init(MonsterData data, int dropTableID)` |
| 스테이지 | `MonsterSpawner.SetupStage`, `CurrentStage`, `Elapsed` | `WaveManager`로 이동 |
| 인스펙터 | `MonsterSpawner`의 stageId, spawnRadius, minRepeatInterval | `WaveManager`로 이동 |

- 변경이 없는 것: `OnEnemyKilled`, `OnBossSpawned`, `OnBossKilled`, `SpawnedEnemies`, `CurrentBoss`, `IsBossBattle`, `ClearAllEnemies`. BossArena, PlayerLootReceiver, 스킬 쪽은 수정하지 않는다.

## 구조 개요

```
Stage.json (3차)
 stageID, waveID
        │ 1:1
        ▼
Wave.json (2차)                        SpawnPattern.json (1차)
 waveEntryID, waveID, startTime,       eventType, formation, spawnCount,
 patternID, monsterID                  spawnInterval, duration, dropTableID
        │                                     │
        └──────────────▶ WaveManager ◀────────┘
                          │ 경과 시간 진행, 보스전 중 비보스 패턴 정지
                          │ formation으로 위치 spawnCount개 계산
                          ▼
        MonsterSpawner.SpawnMonster(monsterID, position, dropTableID)
                          │ 풀, Init(data, dropTableID), 적 목록, 보스 이벤트
                          ▼
                 Enemy (DropTableID 보관)
                          │ Die(giveReward: true)
                          ▼
        DropItemManager.SpawnTable(dropTableID, position) ◀── DropTable.json
                          │ 그룹마다 가중치로 1개 추첨, None이면 건너뜀
                          ▼
                 Spawn(type, position) × count
```

## 작업 항목
| # | 작업 단위 | 산출물(클래스/파일 경로) | 의존 |
|---|---|---|---|
| 1 | 기존 인스펙터 값 기록 (Unity CLI): MonsterSpawner가 배치된 씬과 프리팹을 찾아 stageId, spawnRadius, minRepeatInterval 값을 적는다 | 본 문서 "이관 값" 절 | - |
| 2 | DropTableEntryData 클래스 | `Assets/Scripts/Data/DropTableEntryData.cs` | - |
| 3 | DropTable.json (기존 드롭 이관 + 샘플) | `Assets/Resources/JsonFiles/DropTable.json` | #2 |
| 4 | 경로 상수 2개 추가 | `Assets/Scripts/Utils/GameConstants.cs` | - |
| 5 | JsonDataManager: DropTable 로드·조회 | `Assets/Scripts/Mangers/JsonDataManager.cs` | #2, #4 |
| 6 | DropItemManager: `SpawnTable(int dropTableID, Vector2 position)` | `Assets/Scripts/Mangers/DropItemManager.cs` | #5 |
| 7 | SpawnPatternData 클래스, `SpawnFormation` enum | `Assets/Scripts/Data/SpawnPatternData.cs` | - |
| 8 | SpawnPattern.json (기존 이벤트 이관 + 샘플) | `Assets/Resources/JsonFiles/SpawnPattern.json` | #7 |
| 9 | JsonDataManager: SpawnPattern 로드·조회 | `JsonDataManager.cs` | #4, #7 |
| 10 | SpawnEventData 축소, `SpawnEventType` enum을 SpawnPatternData.cs로 이동 | `Assets/Scripts/Data/SpawnEventData.cs`, `SpawnPatternData.cs` | #7 |
| 11 | Spawn.json을 타임라인 형식으로 재작성 | `Assets/Resources/JsonFiles/Spawn.json` | #8, #10 |
| 12 | JsonDataManager.LoadSpawnData에서 `data.OnLoaded()` 호출 제거 | `JsonDataManager.cs` | #10 |
| 13 | JsonDataManagerEditor: `DrawSpawnEvent`와 스폰 이벤트 라벨을 새 SpawnEventData 필드로 교체 | `Assets/Editor/JsonDataManagerEditor.cs` | #10 |
| 14 | Enemy·Box: `DropTableID`, GiveReward를 `SpawnTable`로 교체 | `Assets/Scripts/Enemy/Enemy.cs`, `Assets/Scripts/Enemy/Box.cs` | #6 |
| 15 | MonsterSpawner 축소: 타임라인과 위치 계산 제거, SpawnMonster 시그니처 변경 | `Assets/Scripts/Mangers/MonsterSpawner.cs` | #14 |
| 16 | WaveManager: 스테이지 설정, 타임라인, 배치 형태, `TestSpawnPattern` ContextMenu | `Assets/Scripts/Mangers/WaveManager.cs` | #9, #10, #15 |
| 17 | BattleManager: `wave` 참조 추가, `spawner.CurrentStage` → `wave.CurrentStage` | `Assets/Scripts/Mangers/BattleManager.cs` | #16 |
| 18 | 씬 배치 (Unity CLI): MonsterSpawner와 같은 GameObject에 WaveManager 추가, spawner·player 참조와 #1 값 연결, BattleManager.wave 연결 | `Assets/Scenes/BattleScene.unity` | #1, #17 |
| 19 | BattleScene 검증 | 본 문서 하단 "검증 결과" | #18 |
| 20 | 데이터 문서 갱신 | `Docs/ETC/GameData_Tags.md`, `Docs/ETC/GameData_Issues_DevAkasha.md` | #19 |

## 합의된 구현 결정

### 1. 역할 분리 (합의: WaveManager가 타임라인, MonsterSpawner는 실행기)
| 클래스 | 맡는 일 |
|---|---|
| WaveManager | 스테이지 결정(`TryConsumePendingStageId`), 타임라인 진행, 보스전 중 정지, 배치 형태별 위치 계산 |
| MonsterSpawner | 몬스터 데이터 조회, 풀 선택·생성, `Init`·`SetTarget`, 적 목록, 보스 상태와 이벤트, `ClearAllEnemies` |
| Enemy | 스폰 때 받은 `DropTableID`를 들고 있다가 처치되면 드롭을 요청한다 |
| DropItemManager | 드롭 테이블 판정과 생성 |

- WaveManager는 싱글톤이 아니다. BattleManager처럼 `[SerializeField] MonsterSpawner spawner`로 참조한다. 종료 중에 `MonsterSpawner.Instance`를 부르면 스포너가 새로 생기는 문제(WP02)를 피하려는 것이다.
- 위치는 모두 WaveManager가 정한다. MonsterSpawner의 Box 위치 분기는 없앤다(합의: Screen 형태 추가).

### 2. 데이터 구조 (합의: 패턴 테이블 분리, 스테이지·웨이브 1:1)
- 계층: Stage(3차) → Wave(2차) → SpawnPattern·Monster(1차). 1차는 다른 데이터를 참조하지 않는 재사용 단위이고, Wave가 시각·패턴·몬스터를 조합하며, Stage는 웨이브 하나를 가리킨다.
- Wave 한 행은 시각, 패턴, 몬스터 1종이다. 여러 종은 같은 시각에 행을 여러 개 둔다. Random·Screen이면 섞어 뽑은 것과 같고, Ring·Line·Cluster는 행마다 방향을 따로 뽑으므로 무리가 따로 생긴다.
- 드롭 테이블은 패턴별로 둔다. 같은 모양이라도 드롭이 다르면 패턴을 나눈다(패턴 1, 3, 5).
- 원본은 구글 시트다. Stage, Wave, SpawnPattern, DropTable 탭을 `Tools > Google Sheets > JSON Exporter`로 내보내 `Resources/JsonFiles`에 저장한다. 탭 이름이 파일 이름이 된다. JSON만 고치면 다음 내보내기에서 덮어써진다.

**SpawnPattern.json**: 재사용하는 패턴 정의
| 열 | 타입 | 의미 |
|---|---|---|
| patternID | int | 1부터 |
| eventType | string | `Normal` / `Elite` / `Horde` / `Boss` → `SpawnEventType` |
| formation | string | `Random` / `Ring` / `Line` / `Cluster` / `Screen` → `SpawnFormation` |
| spawnCount | int | 한 번 스폰할 때의 마릿수 (1 이상) |
| spawnInterval | float | 반복 간격(초). duration이 0이면 쓰지 않는다 |
| duration | float | 반복 지속 시간(초). 0이면 startTime에 한 번 |
| dropTableID | int | DropTable.json의 테이블. 0이면 드롭 없음 |

**Wave.json**: 웨이브 (옛 Spawn.json)
| 열 | 타입 | 의미 |
|---|---|---|
| waveEntryID | int | 1부터 |
| waveID | int | Stage.json의 waveID가 참조 |
| startTime | float | 시작 시각(초) |
| patternID | int | SpawnPattern.json 참조 |
| monsterID | int | Monster.json 참조 |

- 이전 열 `eventType`, `endTime`, `spawnInterval`, `spawnCount`, `repeat`는 없앤다. eventType은 패턴으로 옮기고, endTime은 `startTime + duration`으로, repeat는 `duration > 0`으로 대신한다.
- 옛 `spawnEventID`, `stageID`는 `waveEntryID`, `waveID`로 바꾼다. 행이 스테이지를 가리키지 않고 스테이지가 웨이브를 가리킨다.

**Stage.json**: 추가 열
| 열 | 타입 | 의미 |
|---|---|---|
| waveID | int | Wave.json의 웨이브. 스테이지와 1:1 |

**DropTable.json** (합의: 그룹 가중치)
| 열 | 타입 | 의미 |
|---|---|---|
| dropTableID | int | 1부터 |
| group | int | 0부터. 같은 테이블 안에서 그룹마다 한 번씩 추첨한다 |
| dropItemType | string | `DropItemType` 이름 또는 `None` |
| weight | int | 그룹 안의 가중치 (1 이상) |
| count | int | 뽑혔을 때 생성할 개수 (1 이상) |

### 3. 데이터 클래스
```csharp
// SpawnPatternData.cs
public enum SpawnEventType { Normal, Elite, Horde, Boss }       // 옛 SpawnEventData.cs에서 이동
public enum SpawnFormation { Random, Ring, Line, Cluster, Screen }

[Serializable]
public class SpawnPatternData
{
    public int patternID;
    public string eventType;
    public string formation;
    public int spawnCount;
    public float spawnInterval;
    public float duration;
    public int dropTableID;

    public SpawnEventType EventType { get; }   // [NonSerialized] 파싱 값
    public SpawnFormation Formation { get; }

    public bool OnLoaded();                    // 둘 중 하나라도 알 수 없으면 false
}

// WaveEntryData.cs (옛 SpawnEventData.cs)
[Serializable]
public class WaveEntryData
{
    public int waveEntryID;
    public int waveID;
    public float startTime;
    public int patternID;
    public int monsterID;
}

// StageData.cs: 추가 필드
public int waveID;

// DropTableEntryData.cs
[Serializable]
public class DropTableEntryData
{
    public int dropTableID;
    public int group;
    public string dropItemType;
    public int weight;
    public int count;

    public DropItemType Type { get; }          // [NonSerialized] 파싱 값
    public bool IsNone { get; }                // dropItemType == "None"

    public bool OnLoaded();                    // None이거나 DropItemType으로 파싱되면 true
}
```
- `OnLoaded()`가 bool을 돌려주는 방식은 DropItemData와 같다. 로더는 false이면 예외를 던져 로드 실패 로그를 남긴다.
- WaveEntryData는 파싱할 값이 없어 `OnLoaded()`가 없다.

### 4. JsonDataManager
- Awake에서 `LoadSpawnPatternData()`, `LoadDropTableData()`를 추가로 호출한다.
- **Wave**: 옛 `LoadSpawnData`를 `LoadWaveData`로 바꾼다. `Dictionary<int, WaveEntryData>`(키 waveEntryID), `WaveEntryDataDic`, `GetWaveEntryDataFromJson(int waveEntryID)`. ID 중복이면 예외를 던진다.
- **SpawnPattern**: `Dictionary<int, SpawnPatternData>`. ID 중복, `spawnCount < 1`, `OnLoaded()` false이면 예외를 던진다.
  - 조회: `SpawnPatternData GetSpawnPatternDataFromJson(int patternID)` (기존 Get 메서드와 같은 null·미존재 처리)
- **DropTable**: `Dictionary<int, List<DropTableEntryData>>`. 행을 dropTableID로 묶고 group 오름차순으로 정렬한다.
  - `dropTableID <= 0`, `group < 0`, `weight < 1`, `count < 1`, `OnLoaded()` false이면 예외를 던진다.
  - 조회: `IReadOnlyList<DropTableEntryData> GetDropTableFromJson(int dropTableID)`
- 경로 상수: `SpawnPatternData_Json_Path = "JsonFiles/SpawnPattern"`, `DropTableData_Json_Path = "JsonFiles/DropTable"`, `WaveData_Json_Path = "JsonFiles/Wave"`(옛 `SpawnData_Json_Path`)
- 새 메서드는 기존 `#region` 안에 둔다. 새 region은 만들지 않는다.
- **JsonDataManagerEditor**: `DrawWaveEntry`(옛 `DrawSpawnEvent`)는 waveEntryID, waveID, startTime, patternID, monsterID를 표시한다. 목록 제목은 "웨이브 데이터", 라벨은 `웨이브 {waveID} / 패턴 {patternID}`다. `DrawStage`에 웨이브 ID를 표시한다. 없어진 필드 표시를 지우지 않으면 Editor 어셈블리 컴파일이 깨진다.

### 5. 드롭 판정: `DropItemManager.SpawnTable(int dropTableID, Vector2 position)`
1. `dropTableID <= 0`이면 아무것도 하지 않는다.
2. `GetDropTableFromJson`이 null이면 반환한다.
3. 정렬된 행을 group 단위로 끊어서 그룹마다:
   - 가중치 합을 구하고 `Random.Range(0, 합)`으로 한 행을 고른다.
   - 고른 행이 `IsNone`이면 건너뛰고, 아니면 `Spawn(Type, position)`을 count번 호출한다.
- DropItemManager는 `Unity.Mathematics`를 using하므로 `UnityEngine.Random.Range`로 명시한다.
- 같은 위치에 여러 개가 생성되는 것은 지금 엘리트·보스 드롭과 같다.

### 6. Enemy·Box
- `Enemy`
  - `public int DropTableID { get; private set; }`
  - `Init(MonsterData data, int dropTableID)`에서 저장한다.
  - `GiveReward()`: `GameManager.DropItem.SpawnTable(DropTableID, transform.position)` 후 `OnDied?.Invoke(this)`
  - `DropBoxes`, 타입별 switch, ExpGem1 TODO 주석을 없앤다.
- `Box`
  - `GiveReward()`: `SpawnTable(DropTableID, transform.position)`만 호출한다. 지금처럼 킬 수 집계(OnDied)는 하지 않는다.
  - `dropTypes`, `dropWeights`, `RollDrop`을 없앤다.
- `Die(giveReward: false)`(보스 등장 정리)는 지금처럼 드롭이 없다.

### 7. MonsterSpawner 축소
- **없애는 것**: `stageId`, `spawnRadius`, `minRepeatInterval`, `CurrentStage`, `Elapsed`, `SpawnEventState`, `eventStates`, `elapsed`, `Start`, `SetupStage`, `Update`, `ProcessEvent`, `GetInterval`, `SpawnGroup`, `GetRandomScreenPosition`, `GetRandomSpawnPosition`, 쓰지 않게 되는 `Random` 별칭
- **SpawnMonster(int monsterId, Vector3 position, int dropTableId)**: 위치를 계산하지 않고 받은 position에 꺼낸다. `enemy.Init(data, dropTableId)`. 보스 등장 정리, 풀 선택, 구독, 목록 추가, 보스 이벤트는 그대로 둔다.
- `player`는 `SetTarget`에 계속 쓰므로 남긴다.

### 8. WaveManager
```csharp
public class WaveManager : MonoBehaviour
{
    [SerializeField] private MonsterSpawner spawner;
    [SerializeField] private Transform player;
    [SerializeField] private int stageId = 1;                // 메인 씬을 거치지 않을 때의 기본값
    [SerializeField] private float spawnRadius = 10f;        // 플레이어로부터의 거리
    [SerializeField] private float minRepeatInterval = 0.1f; // 매 프레임 스폰되지 않게
    [SerializeField] private float lineLength = 6f;          // Line 전체 길이
    [SerializeField] private float clusterRadius = 1.5f;     // Cluster 퍼짐 반경
    [SerializeField] private int testPatternID = 7;
    [SerializeField] private int testMonsterID = 1001;

    public StageData CurrentStage { get; private set; }
    public float Elapsed => elapsed;

    public void SetupStage(int newStageId);
}
```
- lineLength와 clusterRadius는 모든 패턴이 공통으로 쓴다(합의: WaveManager 인스펙터).
- **Start**: MonsterSpawner.Start의 스테이지 결정 로직(`GameManager.Scene.TryConsumePendingStageId`)을 그대로 옮긴 뒤 `SetupStage`를 호출한다.
- **SetupStage**: `CurrentStage`를 읽고 경과 시간을 0으로 둔다. 스테이지가 없으면 에러를 남기고 시작하지 않는다(waveID를 읽을 수 없다). `CurrentStage.waveID`의 Wave 행마다 패턴을 찾아 상태 목록에 넣는다. 패턴이 없으면 경고를 남기고 그 이벤트를 건너뛴다. 패턴의 dropTableID가 1 이상인데 `GetDropTableFromJson`이 null이면(조회 메서드가 경고를 남긴다) 그 이벤트도 건너뛴다. 처치할 때마다 같은 경고가 반복되지 않게 시작할 때 한 번 거르는 것이다. 행이 0개이면 경고를 남긴다(`stageID {n}의 waveID {m}에 항목이 없습니다`).
- 상태 항목: `WaveEntryData data`, `SpawnPatternData pattern`, `float nextSpawnTime`(초기값 startTime), `bool finished`
- **Update**: player가 null이면 반환하고, 경과 시간을 더한 뒤 끝나지 않은 항목을 처리한다.
- **이벤트 처리**: 지금 ProcessEvent와 같은 규칙이다. 간격은 `Mathf.Max(pattern.spawnInterval, minRepeatInterval)`이다.
  1. `spawner.IsBossBattle`이고 패턴의 eventType이 Boss가 아니면: 시각이 지났을 때 반복 패턴은 `nextSpawnTime = elapsed + 간격`, 1회 패턴은 끝낸다. 그리고 반환한다.
  2. `elapsed < nextSpawnTime`이면 반환한다.
  3. `duration <= 0`이면 한 번 스폰하고 끝낸다.
  4. `elapsed >= startTime + duration`이면 끝낸다.
  5. 한 번 스폰하고 `nextSpawnTime += 간격`
- **한 번 스폰**: formation에 따라 위치 spawnCount개를 정하고, 위치마다 `spawner.SpawnMonster(monsterID, 위치, pattern.dropTableID)`를 호출한다.
- **`[ContextMenu("TestSpawnPattern")]`**: testPatternID 패턴을 testMonsterID로 즉시 한 번 스폰한다. 타임라인과 무관하다.

### 9. 배치 형태 (합의: Random, Ring, Line, Cluster + Screen)
중심 P는 플레이어 위치, R은 spawnRadius, n은 spawnCount이다. 무작위 값은 한 번 스폰할 때마다 새로 뽑는다.
| 형태 | 위치 |
|---|---|
| Random | 마리마다 무작위 방향 d를 따로 뽑아 `P + d × R` (지금 방식) |
| Ring | 무작위 시작각 θ를 한 번 뽑고, i번째는 각도 `θ + 360° × i / n` 방향으로 `P + d × R` |
| Line | 무작위 방향 d를 한 번 뽑고, 중심 `C = P + d × R`, d에 수직인 단위 벡터 e로 `C − e × lineLength/2`부터 `C + e × lineLength/2`까지 양 끝을 포함해 간격 `lineLength / (n − 1)`로 n개를 둔다. n이 1이면 C |
| Cluster | 무작위 방향 d를 한 번 뽑고, 중심 `C = P + d × R`에서 마리마다 `C + insideUnitCircle × clusterRadius` |
| Screen | 마리마다 카메라 화면 안의 무작위 위치 (지금 상자 방식, `Camera.main` 기준) |

- 상자 위치는 이제 몬스터 타입이 아니라 패턴의 formation이 정한다. 상자(`MonsterType.Box`)는 움직이지 않으므로 **Screen 패턴으로만 스폰한다.** 코드로 강제하지 않는 데이터 규칙이다. 다른 형태에 연결하면 상자가 화면 밖에 생겨 보이지 않는다.

### 10. 데이터 (시트 기준)
최초 작업은 기존 7개 이벤트와 하드코딩 드롭을 같은 동작으로 옮겼다. 후속 변경에서 시트를 원본으로 정하면서 Wave와 SpawnPattern 1~6을 시트의 옛 Spawn 탭 값으로 바꿨다. 7~9번 패턴과 5번 테이블은 검증용 샘플이다.

- 변환 규칙: 옛 `endTime − startTime`이 duration이 되고, `repeat FALSE`는 duration 0이 된다. spawnInterval, spawnCount는 패턴으로 옮긴다.
- 최초 작업 때 Spawn.json 값과 달라진 것: 패턴 1, 2, 4의 spawnCount와 duration, Wave의 startTime(보스 150초 → 45초)

**SpawnPattern.json**
| patternID | eventType | formation | spawnCount | spawnInterval | duration | dropTableID | 출처(옛 시트 이벤트) |
|---|---|---|---|---|---|---|---|
| 1 | Normal | Random | 1 | 1.0 | 10 | 1 | 1 |
| 2 | Normal | Random | 2 | 2.0 | 10 | 1 | 2 |
| 3 | Elite | Random | 1 | 0 | 0 | 2 | 3, 5 |
| 4 | Horde | Random | 3 | 0.5 | 10 | 1 | 4 |
| 5 | Boss | Random | 1 | 0 | 0 | 3 | 6 |
| 6 | Normal | Screen | 1 | 20.0 | 140 | 4 | 7 (상자) |
| 7 | Horde | Ring | 12 | 0 | 0 | 5 | 샘플 |
| 8 | Horde | Line | 8 | 0 | 0 | 5 | 샘플 |
| 9 | Horde | Cluster | 6 | 0 | 0 | 5 | 샘플 |

**Wave.json** (waveEntryID는 옛 spawnEventID와 같다)
| waveEntryID | waveID | startTime | patternID | monsterID |
|---|---|---|---|---|
| 1 | 1 | 0 | 1 | 1001 |
| 2 | 1 | 10 | 2 | 1002 |
| 3 | 1 | 15 | 3 | 2001 |
| 4 | 1 | 20 | 4 | 1001 |
| 5 | 1 | 25 | 3 | 2002 |
| 6 | 1 | 45 | 5 | 3001 |
| 7 | 1 | 10 | 6 | 4001 |

**Stage.json** waveID: 스테이지 1 → 1, 스테이지 2 → 2. 웨이브 2는 행이 없다.

**DropTable.json**
| dropTableID | group | dropItemType | weight | count | 출처 |
|---|---|---|---|---|---|
| 1 | 0 | ExpGem1 | 1 | 1 | 일반: `Enemy.GiveReward` |
| 2 | 0 | ExpGem1 | 1 | 1 | 엘리트 |
| 2 | 1 | LuckyBox | 1 | 1 | |
| 2 | 2 | RewardBox | 1 | 2 | |
| 3 | 0 | ExpGem1 | 1 | 1 | 보스 |
| 3 | 1 | RewardBox | 1 | 5 | |
| 4 | 0 | Gold1 | 31 | 1 | 상자: `Box.dropWeights` |
| 4 | 0 | Gold2 | 5 | 1 | |
| 4 | 0 | Gold3 | 3 | 1 | |
| 4 | 0 | Gold4 | 1 | 1 | |
| 4 | 0 | Bomb | 20 | 1 | |
| 4 | 0 | Potion | 20 | 1 | |
| 4 | 0 | Magnet | 20 | 1 | |
| 5 | 0 | ExpGem1 | 1 | 1 | 샘플: 잼 확정 |
| 5 | 1 | None | 50 | 1 | 샘플: None 검증 |
| 5 | 1 | Potion | 50 | 1 | |

- 경험치잼은 테이블에 포함한다(합의). 이관 값은 지금 동작대로 ExpGem1이다.
- 새 JSON 파일의 .meta는 Unity가 생성한다. 저장 후 에디터에서 에셋을 새로고침한다.

### 11. BattleManager
- `[SerializeField] private WaveManager wave;`를 추가한다.
- EndBattle의 `spawner.CurrentStage` 두 곳을 `wave.CurrentStage`로 바꾼다.
- 킬·보스 구독은 그대로 spawner를 쓴다.

## 의존 순서 / 병렬 가능 그룹
- **그룹 A, 드롭 테이블**: #2 → #3, #4 → #5 → #6. 기존 코드를 깨지 않으므로 단독으로 컴파일을 확인할 수 있다.
- **그룹 B, 패턴 데이터**: #7 → #8, #9. A와 병렬로 진행할 수 있다.
- **그룹 C, 타임라인 이전**: #10 → #11, #12, #13, #14 → #15 → #16 → #17
  - #10부터 #17까지는 중간에 컴파일이 깨진다. 한 묶음으로 진행하고 끝에 OmniSharp 진단과 Unity 콘솔로 확인한다.
  - #14는 #6(A)이 끝나야 한다.
- **#1은 #15 전에 끝낸다.** 필드를 지우면 직렬화된 값을 잃는다.
- **직렬**: C 완료 → #18 → #19 → #20

## 이관 값
#1 결과. MonsterSpawner는 프리팹 `Assets/Prefabs/Enemy/MonsterSpawner.prefab`(guid 83a0eec2…)에 있고, 씬은 이 프리팹의 인스턴스를 쓴다.

| 위치 | stageId | spawnRadius | minRepeatInterval | 비고 |
|---|---|---|---|---|
| `MonsterSpawner.prefab` | 1 | 10 | 0.1 | 기본값. 세 값을 오버라이드한 씬은 없다 |
| `BattleScene.unity` | (프리팹 값) | (프리팹 값) | (프리팹 값) | `player`만 오버라이드 |
| `enemy.unity` | (프리팹 값) | (프리팹 값) | (프리팹 값) | `player`만 오버라이드 |
| `TestPlayerScene.unity` | (프리팹 값) | (프리팹 값) | (프리팹 값) | MonsterSpawner 컴포넌트 오버라이드 없음 |
| `ManagersTestScene.unity` | - | 10 | - | 프리팹이 아닌 옛 직렬화 데이터(`enemyPrefab`, `spawnInterval`). 지금 필드와 맞지 않아 이관 대상이 아니다 |

- 이관 값은 WaveManager 기본값(stageId 1, spawnRadius 10, minRepeatInterval 0.1)과 같다.
- 프리팹이므로 #18에서 WaveManager를 **프리팹에** 추가한다. BattleScene·enemy·TestPlayerScene이 함께 받는다. 씬마다 `player`는 MonsterSpawner처럼 인스턴스 오버라이드로 연결한다.

## 검증 항목
최초 작업 기준이다(옛 Spawn.json 값). 후속 변경 뒤의 검증은 "후속 변경" 절에 있다.

**타임라인 이관 (BattleScene, 스테이지 1)**
- 0~30초에는 1001이 1초마다 1마리, 30~60초에는 1002가 2초마다 1마리, 60~90초에는 1001이 0.5초마다 2마리 나온다.
- 45초와 75초에 엘리트(2001, 2002)가 한 번씩 나온다.
- 10초부터 20초마다 상자가 화면 안에 나온다.
- 150초에 보스가 나오면 남은 몬스터가 드롭 없이 사라지고, 보스전 동안 일반·상자 스폰이 멈춘다. 보스를 잡은 뒤 지나간 스폰이 몰려나오지 않는다.
- 결과창의 스테이지 ID와 클리어 보상이 이전과 같다(`wave.CurrentStage`).
- 콘솔에 로드 에러나 경고가 없다.

**드롭 (BattleScene)**
- 일반 몬스터는 ExpGem1 1개를 떨어뜨린다.
- 엘리트는 ExpGem1 1개, LuckyBox 1개, RewardBox 2개를 떨어뜨린다.
- 보스는 ExpGem1 1개와 RewardBox 5개를 떨어뜨린다.
- 상자는 7종 중 정확히 1개를 떨어뜨리고 킬 수가 오르지 않는다.

**배치 (TestSpawnPattern)**
- 7: 12마리가 플레이어를 중심으로 한 원 위에 같은 간격으로 놓인다.
- 8: 8마리가 한쪽에 플레이어 방향과 수직인 직선으로 놓인다.
- 9: 6마리가 원 위의 한 지점 주변에 뭉친다.
- 6: testMonsterID를 4001로 바꾸고 실행하면 상자가 화면 안에 놓인다. 확인 후 1001로 되돌린다.
- 1: 플레이어 주변 원 위의 무작위 위치에 놓인다.
- 7~9로 나온 몬스터를 잡으면 ExpGem1은 항상 나오고, Potion은 몇 번에 한 번만 나온다(None 그룹).

**데이터 오류**
- DropTable.json에 알 수 없는 dropItemType을 넣으면 로드 실패 로그가 남는다. 확인 후 되돌린다.

## 미해결 / 실행 시 확인 필요
- **enemy.unity, TestPlayerScene**: 두 씬은 `MonsterSpawner.prefab` 인스턴스를 쓴다. 프리팹에 WaveManager를 추가했으므로 두 씬에도 WaveManager가 생겼지만 `player`가 비어 있어(Update가 반환한다) 몬스터가 나오지 않는다. 씬 파일은 수정하지 않았다. 승희님과 협의해 두 씬의 WaveManager에 `player`를 연결할지 정한다.
- **ManagersTestScene**: 프리팹이 아닌 옛 MonsterSpawner 직렬화 데이터(`enemyPrefab`, `spawnInterval`)가 남아 있다. 이번 변경과 관계없이 이미 지금 필드와 맞지 않는다.
- **경험치잼 분포**: ExpGem2~4를 쓰려면 DropTable의 잼 그룹 가중치만 바꾸면 된다. 기획 수치가 정해지면 반영한다. 이때 monsterExp는 드롭에 쓰이지 않는다.
- **새 형태의 웨이브 배치**: Ring/Line/Cluster를 어느 웨이브 몇 초에 넣을지는 기획 결정이다. 그 전에는 ContextMenu로만 검증한다.
- **Stage.duration**: 150초로 웨이브 1의 보스 등장(45초)과 다르다. 읽는 코드는 없지만 `StageData` 주석에는 보스 등장 시각과 맞춘 값이라고 적혀 있다.
- **웨이브 2**: 스테이지 2의 waveID 2에 행이 없어 시작하면 경고만 남는다. 변경 전에도 스테이지 2에는 이벤트가 없었다.
- **Boss 패턴의 spawnCount > 1**: 보스가 나올 때마다 `ClearAllEnemies`가 불려 먼저 나온 보스가 정리된다. 지금 데이터에는 없다. 필요해지면 그때 규칙을 정한다.
- **lineLength, clusterRadius 초기값**: 6, 1.5는 임시값이다. 검증할 때 화면을 보고 조정한다.

## 검증 결과
최초 작업 기준이다(옛 Spawn.json 값). BattleScene을 Play 모드로 실행해 확인했다(Unity CLI `eval`로 상태 조회). 전체 플레이 동안 콘솔 에러·경고는 0건이다. 컴파일도 에러가 없다.

**타임라인 이관 (스테이지 1)**: 통과
- 5배속으로 진행해 각 구간을 누적 개수로 대조했다(71초까지 1001 76마리, 1002 15마리, 2001 1마리, 상자 4개).
  - 0~30초 1001 1마리/초 = 30마리, 60~90초 1001 4마리/초(2마리 × 0.5초), 30~60초 1002 2초 간격 = 15마리
  - 엘리트 2001, 2002가 각각 1마리씩 나왔다
  - 상자 4001이 10, 30, …, 130초에 7개 나왔다. 150초에는 나오지 않는다(`startTime + duration`). 모두 화면 안이다
  - 150초 이후 `boss=True`, 보스 3001 1마리
- 보스전 정지: 스테이지를 0초로 재시작하고 보스를 즉시 스폰했다. 8초 동안 1001 생존 수가 0이고, 상자도 늘지 않았다
- 보스 처치 뒤: 약 2초 동안 1001이 2마리(초당 1마리 재개)이고 지나간 스폰이 몰려나오지 않았다
- 결과창: `stage=1, victory=True, accountExp=530`(킬 1 + 29초 + clearAccountExp 500). `wave.CurrentStage`로 읽는다

**드롭**: 통과 (같은 프레임에서 ECS `DropItem` 엔티티 증가분으로 확인)
- 일반 1001: ExpGem1 ×1, 킬 +1
- 엘리트 2001: ExpGem1 ×1, LuckyBox ×1, RewardBox ×2, 킬 +1
- 보스 3001: ExpGem1 ×1, RewardBox ×5, 킬 +1
- 상자 4001: 200번 모두 정확히 1개, 킬 수 0. 1000번 분포는 Gold1 312, Gold2 57, Gold3 32, Gold4 15, Bomb 184, Potion 195, Magnet 205로 31·5·3·1·20·20·20과 맞는다
- 샘플 테이블 5: 200번에 ExpGem1 ×200, Potion ×103으로 None 그룹이 동작한다

**배치 (TestSpawnPattern)**: 통과. ContextMenu는 `menu`로 실행되지 않아 `SendMessage("TestSpawnPattern")`로 호출하고 스폰 직후 위치를 계산했다.
- 7 Ring: 12마리, 플레이어까지 거리 10.00, 각도 간격 30.0도
- 8 Line: 8마리, 중심 거리 10.00, 축 수직 편차 0.000, 길이 6.00, 간격 0.857(= 6/7)
- 9 Cluster: 6마리, 거리 9.10~11.21, 서로 최대 거리 2.79(clusterRadius 1.5 안)
- 6 Screen: testMonsterID를 4001로 바꿔 실행했고 화면 안에 놓였다
- 1 Random: 거리 10.00, 호출마다 각도가 다르다(261, 268, 38)

**데이터 오류**: 통과. DropTable.json의 LuckyBox를 LuckyBoxX로 바꾸면 `DropTable 데이터 로드 실패: DropTable 2의 dropItemType "LuckyBoxX"을(를) 알 수 없습니다.`가 남는다. 확인 후 원래대로 되돌렸다

**환경 메모**
- 에디터가 비활성이면 플레이 루프가 멈춰서 검증하는 동안에만 `Application.runInBackground`를 켰다. 끝나고 원래 값(false)으로 되돌렸고 ProjectSettings는 바뀌지 않았다
- 검증을 끝낸 뒤 에디터는 원래 열려 있던 LogInScene으로 되돌렸다

## 후속 변경: 스테이지·웨이브 분리
**배경**: Spawn.json 행이 stageID를 직접 가져 스테이지와 스폰 타임라인이 묶여 있었다. 데이터를 Stage(3차) → Wave(2차) → SpawnPattern·Monster(1차)로 나눈다. JSON은 구글 시트에서 내보내므로 시트도 함께 고친다.

**합의**
| 항목 | 결정 |
|---|---|
| Stage ↔ Wave | 1:1. Stage가 waveID를 가진다 |
| 이름 | Spawn.json → Wave.json, SpawnEventData → WaveEntryData. Unity에서 이동해 .meta와 GUID를 유지한다 |
| Wave 한 행 | 시각, 패턴, 몬스터 1종. 여러 종은 같은 시각에 행을 여러 개 둔다 |
| 드롭 테이블 | 패턴별로 둔다 |
| SpawnPattern 열 | 바꾸지 않는다. spawnInterval, duration, eventType이 남고, 배치 값(spawnRadius, lineLength, clusterRadius)도 WaveManager 인스펙터에 남는다 |
| 시트 | Stage 탭에 waveID 열을 추가하고, Spawn 탭을 Wave로 바꾸고, SpawnPattern·DropTable 탭을 추가한다. Wave 값은 시트의 옛 Spawn 값을 새 형식으로 옮긴다 |

**작업**
| # | 작업 단위 | 산출물 |
|---|---|---|
| 21 | Spawn.json → Wave.json, SpawnEventData.cs → WaveEntryData.cs 이동 (Unity CLI `AssetDatabase.MoveAsset`) | `Assets/Resources/JsonFiles/Wave.json`, `Assets/Scripts/Data/WaveEntryData.cs` |
| 22 | WaveEntryData: spawnEventID → waveEntryID, stageID → waveID | `WaveEntryData.cs` |
| 23 | StageData.waveID | `StageData.cs` |
| 24 | 경로 상수 `WaveData_Json_Path`, JsonDataManager `LoadWaveData`·`WaveEntryDataDic`·`GetWaveEntryDataFromJson` | `GameConstants.cs`, `JsonDataManager.cs` |
| 25 | WaveManager.SetupStage: `CurrentStage.waveID`로 행 선택, 스테이지가 없으면 에러 후 반환 | `WaveManager.cs` |
| 26 | JsonDataManagerEditor: `DrawWaveEntry`, 스테이지에 웨이브 ID 표시 | `JsonDataManagerEditor.cs` |
| 27 | 시트 붙여넣기용 TSV 작성, `SheetJsonConverter`로 변환 확인. 붙여넣기와 JSON 내보내기는 유저가 했다 | 구글 시트 Stage, Wave, SpawnPattern, DropTable 탭 |
| 28 | 검증 | 아래 "검증 결과" |
| 29 | 데이터 문서 갱신 | `Docs/ETC/GameData_Tags.md`, `Docs/ETC/GameData_Issues_DevAkasha.md` |

- `SpawnPatternData.cs`의 주석과 `WaveManager.cs` 머리 주석의 파일 이름도 Wave.json으로 바꿨다.
- 승희님 담당 파일(`MonsterSpawner.cs`, `Enemy.cs`, `Box.cs`)은 수정하지 않았다.

**검증 결과**: 통과. 시트에서 내보낸 JSON으로 BattleScene을 3배속 실행했다. 컴파일 에러가 없고 콘솔 에러·경고는 0건이다.
- 내보낸 Stage.json, Wave.json, SpawnPattern.json, DropTable.json이 시트에 붙여넣은 내용과 같다. 시트 탭 목록에 Wave, SpawnPattern, DropTable이 있다.
- 16.2초: 1001 10마리, 1002 8마리, 2001 1마리, 상자 1개. 1001은 0~9초에 1마리씩, 1002는 10·12·14·16초에 2마리씩이다
- 30.5초: 1001 70마리(Horde 20~30초 0.5초마다 3마리 = 60, 여기에 패턴 1의 10), 1002 10마리(2마리 × 5번), 2001·2002 각 1마리, 상자 2개(10, 30초)
- 보스 3001이 45.04초에 나왔다(`OnBossSpawned` 구독으로 측정). 일반·엘리트는 정리되고 상자는 남는다. `ClearAllEnemies`가 상자를 건너뛰기 때문이다
- 스테이지 2로 `SetupStage`하면 `stageID 2의 waveID 2에 항목이 없습니다` 경고가 남는다
- 검증하는 동안만 `runInBackground`와 `timeScale`을 바꿨고, 끝나고 원래 값으로 되돌렸다. 에디터는 LogInScene으로 되돌렸다
