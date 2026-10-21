# WorkPlan 07: 정규 스테이지 구성 (RegularStage)

## 목표
- 테스트용 임시 웨이브 1을 8분 정규 스테이지 구성으로 바꾼다.
- 중간 보스를 별도 역할(`MonsterType.MidBoss`)로 추가한다. 보스와 같은 전투(등장 시 정리, 일반 스폰 정지, 결계, HP 바, Melee)를 하지만, 처치해도 승리하지 않고 엘리트 드롭을 키운 드롭을 준다.
- 마지막 보스전(Boss 타입) 동안 전투 시간을 멈춰 계정 경험치와 생존 기록을 보스 처치 속도와 무관하게 맞춘다.
- 웨이브를 공유한 채 스테이지마다 특색 패턴을 바꿀 수 있게, StageRoster에 패턴 덮어쓰기 열을 둔다.
- 스폰 순간의 플레이어 위치로 직진해 지나가는 돌진 이동을 SpawnPattern 속성으로 추가한다.
- 입력: 유저와의 구성 논의(초안 → 중간 보스·시간 정지·특색 패턴·돌진·원거리 몬스터 결정, 세부 설계는 위임). 별도 기획명세는 없다.

## 스테이지 구성 (8분)
| 구간 | 시각 | 스폰 | 몬스터 | 역할 |
|---|---|---|---|---|
| 노말1 | 0:00~4:00 | 랜덤 위치, 평범한 스폰 속도 | 약한 일반 | 초반 레벨업 |
| 엘리트1 | 2:00 | 1마리 | 엘리트 | |
| 노말2 (특색) | 2:20, 3:00 | 원 포위 | 원거리 보통 일반 | |
| 노말3 (특색) | 2:20~4:00, 20초마다 | 군집 돌격 | 약한 일반 | |
| 중간 보스 | 4:00 | 1마리 | 중간 보스 | |
| 노말4 | 4:20~6:20 | 랜덤 위치, 매우 빠른 스폰 속도 | 보통 일반 | 범위 공격이 충분한지 검증 |
| 엘리트2 | 6:00 | 1마리 | 엘리트 | |
| 노말5 | 6:20~8:00 | 랜덤 위치, 보통 스폰 속도 | 강한 일반 | 피해량이 충분한지 검증 |
| 보스 | 8:00 | 1마리 | 보스 | 처치하면 클리어 |
| 상자 | 0:10~7:50, 20초마다 | 화면 안 | 상자 | |

- 2:20~4:00은 노말1·2·3이 겹친다(의도).
- 스테이지 전체 시간 제한은 없다. 8분은 보스 등장 시각이고, 클리어는 보스 처치다.
- 중간 보스전 동안 시간은 흐르고 일반 스폰은 멈춘다. 그동안 지나간 스폰은 건너뛴다(합의). 중간 보스를 20초 안에 잡지 못하면 노말4 앞부분이, 2분 넘게 잡지 못하면 엘리트2까지 사라진다.

## 범위
**포함**
- 데이터: Monster, MonsterAttack, DropTable, SpawnPattern, Wave, StageRoster 탭 변경 (시트 → SheetExporter)
- 서버(`COU_Server`): `MonsterType`, `StageRosterData`, `StaticDataService` 로드·검증, `BattleService` 스테이지 상한 계산, 문서
- 클라이언트: `MonsterData`, `SpawnPatternData`, `StageRosterData`, `JsonDataManager`, `Enemy`, `MonsterSpawner`, `WaveManager`, `BattleManager`, `JsonDataManagerEditor`, 문서
- 정적 데이터 첫째 자리 버전 3 → 4

**제외**
- 스테이지 2 이후의 Stage·StageRoster 행. 특색 패턴 덮어쓰기는 구조만 만든다
- 수치 밸런싱. 이 문서의 간격·마릿수·배율·드롭은 플레이테스트로 조정할 초기값이다
- 중간 보스 전용 연출. HUD는 보스와 같이 몬스터 이름과 HP 바를 쓰고, 그동안 경험치 바는 숨겨진다(`InGameHUD.ShowBoss`, `InGameHUD.cs:41`)
- 테스트용 웨이브(45초 보스) 유지. 웨이브 1을 덮어쓴다
- Notion 데이터 정의서 갱신(공개 페이지 API는 읽기만 된다. 유저가 갱신한다)

## 사전 조건
- **다른 담당자 파일 수정**: `Enemy.cs`, `MonsterSpawner.cs`는 승희님 담당이다(WorkPlan 06). 작업 전에 아래 변경을 알린다.

| 변경 | 이전 | 이후 |
|---|---|---|
| 보스 판정 | `Enemy.IsBoss` = `Type == Boss` | `Data.IsBoss` (Boss 또는 MidBoss) |
| 보스 등장 정리 | `MonsterSpawner.cs:37` `data.Type == MonsterType.Boss` | `data.IsBoss` |
| 이동 | `Enemy.Move`는 항상 추적 | 돌진 중이면 정해진 방향으로 직진하고, 정해진 거리를 가면 반납 |
| 공개 API | - | `Enemy.StartCharge(Vector2 direction, float distance)` 추가 |
| 프레임 처리 | `Update`: Move → Attack → Tick | Move 뒤 죽었으면 중단 |

- **배포 순서**: 서버 DTO가 StageRoster의 `patternId`를 필수로 읽는다(`FAIL_ON_MISSING_CREATOR_PROPERTIES`). 시트 내보내기, 서버 코드, 클라이언트 코드를 한 번에 반영한다. 버전 3 클라이언트는 앱 업데이트 요구를 받는다.

## 구조 개요

```
WaveManager.SetupStage
  Wave 행 ─ monsterSlot ─▶ StageRoster 행 ─▶ MonsterData
                              │
                              ▼ patternId = 로스터.patternId > 0 ? 로스터.patternId : 웨이브.patternId
                         SpawnPatternData (formation, movement)

WaveManager.SpawnPattern
  enemy = MonsterSpawner.SpawnMonster(monsterId, 위치, 배율)
  movement == Charge && enemy.Type == Normal → enemy.StartCharge(플레이어 위치 − 스폰 위치, 그 거리 × 2)

보스 판정: MonsterData.IsBoss (Boss | MidBoss)
  ├ MonsterSpawner : 등장 시 정리, CurrentBoss, OnBossSpawned / OnBossKilled
  ├ WaveManager    : 보스전 동안 보스 외 스폰 정지
  ├ BossArena, HUD : 이벤트 구독 그대로
  └ BattleManager
       ├ HandleBossSpawned : Type == Boss → 전투 시간 정지
       └ HandleBossKilled  : Type == Boss → 승리 대기 시작
```

## 작업 항목
| # | 작업 단위 | 산출물(클래스/파일 경로) | 의존 |
|---|---|---|---|
| 1 | 시트 붙여넣기용 TSV 작성 (Monster, MonsterAttack, DropTable, SpawnPattern, Wave, StageRoster) | 스크래치 파일 | - |
| 2 | (유저) 시트 반영, SheetExporter 내보내기. 첫째 자리 +1(4.0.0), 클라이언트 복제 체크 | 서버 `src/main/resources/data/`, 클라이언트 `Assets/Resources/Data/` | #1 |
| 3 | 서버 DTO: `MonsterType`에 `MIDBOSS`, `StageRosterData`에 `patternId` | `COU_Server/src/main/java/com/couserver/staticdata/dto/MonsterType.java`, `StageRosterData.java` | - |
| 4 | 서버 `StaticDataService`: StageRoster patternId 검증, `stageRosters` 값 타입 변경, Melee 검증 확장 | `StaticDataService.java` | #3 |
| 5 | 서버 `BattleService.computeStageLimit`: 로스터 patternId로 패턴 결정 | `BattleService.java` | #4 |
| 6 | `STATIC_DATA_MAJOR_VERSION = 4` | `Assets/Scripts/Utils/GameConstants.cs` | - |
| 7 | `MonsterType.MidBoss`, `MonsterData.IsBoss`, ID 대역·monsterType 문자열 주석 | `Assets/Scripts/Data/MonsterData.cs` | - |
| 8 | `SpawnMovement` enum, `SpawnPatternData.movement` / `StageRosterData.patternId` | `Assets/Scripts/Data/SpawnPatternData.cs`, `StageRosterData.cs` | - |
| 9 | `JsonDataManager` 파싱 검증: StageRoster patternId ≥ 0, SpawnPattern 파싱 실패 메시지 | `Assets/Scripts/Mangers/JsonDataManager.cs` | #8 |
| 10 | `Enemy`: `IsBoss`, 돌진, `Update` 중단 | `Assets/Scripts/Enemy/Enemy.cs` | #7 |
| 11 | `MonsterSpawner`: 보스 등장 정리 판정 | `Assets/Scripts/Mangers/MonsterSpawner.cs` | #7 |
| 12 | `WaveManager`: 패턴 결정 순서, 보스 판정, 돌진 시작 | `Assets/Scripts/Mangers/WaveManager.cs` | #7, #8, #10 |
| 13 | `BattleManager`: 승리 판정, 시간 정지 | `Assets/Scripts/Mangers/BattleManager.cs` | #7 |
| 14 | `JsonDataManagerEditor`: movement, patternId 표시 | `Assets/Editor/JsonDataManagerEditor.cs` | #8 |
| 15 | 검증 | 본 문서 "검증 결과" | #2, #5, #9~#14 |
| 16 | 문서 갱신 (아래 "문서 갱신 대상") | 클라이언트 `Docs/ETC/GameData_Tags.md` / 서버 `Docs/Guide/StaticData.md` | #15 |

## 구현 결정

### 1. 중간 보스 (합의: 보스와 같은 전투, 승리 없음, 엘리트 강화 드롭, 시간은 흐름)
- `MonsterType { Normal, Elite, MidBoss, Boss, Box }`. JSON과 시트는 문자열이라 순서는 값에 영향이 없다. 서버 `MonsterType`에도 같은 자리에 `MIDBOSS`를 넣는다(`ACCEPT_CASE_INSENSITIVE_ENUMS`로 "MidBoss"와 맞는다).
- `MonsterData.IsBoss => Type is MonsterType.Boss or MonsterType.MidBoss`
- 보스 판정 교체

| 위치 | 이전 | 이후 |
|---|---|---|
| `Enemy.cs:36` | `Type == MonsterType.Boss` | `Data != null && Data.IsBoss` |
| `MonsterSpawner.cs:37` | `data.Type == MonsterType.Boss` | `data.IsBoss` |
| `WaveManager.cs:148` | `state.monster.Type != MonsterType.Boss` | `!state.monster.IsBoss` |

- 이 교체로 중간 보스는 보스의 동작을 그대로 받는다: 등장 시 일반·엘리트 정리(보상 없음, 상자는 유지), `CurrentBoss`·`OnBossSpawned`·`OnBossKilled`, 보스전 동안 보스 외 스폰 정지, 결계(`BossArena`), HP 바(`BattleManager.HandleBossSpawned`).
- 프리팹: `MonsterSpawner`의 switch가 Normal·Box 외의 타입을 `eliteBossPrefab`(`BossMonster`)으로 보내므로, MidBoss도 보스 공격(예고 상태 머신, Melee 돌진)을 쓴다. 변경 없음.
- `BattleManager.HandleBossKilled`: HP 바를 숨기는 처리는 두 타입 공통이다. `victoryTimer = bossVictoryDelay`는 `killedBoss.Type == MonsterType.Boss`일 때만 한다.
- 서버 검증 `StaticDataService.java:151`: monsterType이 `BOSS` 또는 `MIDBOSS`이면 MELEE를 허용한다. 메시지는 "보스·중간 보스가 아닌 몬스터(monsterId …)는 MELEE를 쓸 수 없습니다."
- 폭탄(`PlayerLootReceiver.cs:84`)은 Normal만 즉사시키고 나머지는 피해를 준다. 변경 없음.
- 서버 킬 수 상한은 BOX만 제외하므로 MidBoss도 킬 수에 들어간다. 변경 없음.
- ID 대역: 15001~ 중간 보스. `MonsterData.cs`의 monsterType 문자열 주석에도 "MidBoss"를 넣는다.

### 2. 마지막 보스전 시간 정지 (합의: 표시와 기록 모두, Boss 타입만)
- `BattleManager`에 `private int clockStopFrame = -1;` (마지막 보스가 나온 프레임)
- `HandleBossSpawned`: `spawnedBoss.Type == MonsterType.Boss`이면 `clockStopFrame = Time.frameCount`.
- `Update`: `victoryTimer` 처리 다음, `elapsed += Time.deltaTime` 앞에서 `if (clockStopFrame >= 0 && Time.frameCount > clockStopFrame) return;`. 보스가 나온 프레임의 시간까지는 더하고 다음 프레임부터 멈춘다. 보스 처치 후 승리 대기(`bossVictoryDelay`) 동안에도 멈춘 채 끝나고, 다시 켜지 않는다.
  - 보스가 나온 프레임에 바로 멈추면, 그 프레임에서 `WaveManager.Update`가 `BattleManager.Update`보다 먼저 돌 때 `BattleManager.elapsed`가 480에 닿기 전(479.9x)에 멈춰 07:59가 된다(검증에서 발견). 프레임 단위로 멈추면 Update 순서와 상관없이 보스 등장 시각에서 멈춘다.
- 결과 `seconds`(HUD, 결과창, `BattleResultRequest`)가 정지 시점 값이 된다. 계정 경험치(`seconds × accountExpPerSecond`)와 `bestSurvivalSeconds`가 보스전 시간만큼 줄어든다(합의). 서버는 실제 경과보다 큰 값만 자르므로(`BattleService.java:137`) 변경 없음.
- `WaveManager.elapsed`는 멈추지 않는다. 보스전 동안 보스 외 스폰은 이미 건너뛰고, 보스 처치가 곧 클리어라 영향이 없다.
- 중간 보스전 동안에는 시간이 흐른다(합의).

### 3. 스테이지별 특색 패턴 (합의: 웨이브를 나누지 않고 기존 SpawnPattern 중에서 고름)
**StageRoster**: `patternId` 열 추가

| 열 | 타입 | 의미 |
|---|---|---|
| patternId | int | SpawnPattern 참조. 0이면 웨이브 행의 patternId |

- 덮어쓰기는 슬롯 단위로, 그 슬롯을 쓰는 웨이브 행 모두에 적용된다. 특색 구간은 전용 슬롯(N2, N3)을 쓰고, 같은 몬스터라도 슬롯을 나눈다(N3 → 11001).
- 스테이지가 바꿀 수 있는 것은 모양·마릿수·배치 수치·이동(movement)이다. 시각과 반복은 웨이브가 정한다.
- 클라이언트
  - `StageRosterData.patternId`
  - 파싱 검증: `patternId >= 0`
  - `WaveManager.SetupStage`: 로스터 행 → 몬스터 → 패턴 순서로 찾는다. `int patternId = rosterRow.patternId > 0 ? rosterRow.patternId : data.patternId;` 패턴을 못 찾았을 때의 경고에는 어느 쪽의 patternId인지 적는다.
- 서버
  - `StageRosterData`에 `int patternId` (필수 열)
  - 검증: `patternId == 0 || spawnPatterns.containsKey(patternId)`. SpawnPattern은 StageRoster보다 먼저 로드된다(`StaticDataService.java:113`, `:126`).
  - `stageRosters`: `Map<Integer, Map<String, StageRosterData>>`(stageId → monsterSlot → 행). `getStageRosters()` 반환 타입, `:128`의 `rosterMap`, `:136`의 `putIfAbsent` 값(`row.monsterId()` → `row`), `:228`의 지역 변수 `roster` 선언 타입도 같이 바뀐다. Stage × Wave 슬롯 검사(`:230`)는 `containsKey`라 로직은 그대로다.
  - `computeStageLimit`: `StageRosterData row = roster.get(entry.monsterSlot())`, `int patternId = row.patternId() > 0 ? row.patternId() : entry.patternId()`, 몬스터는 `row.monsterId()`로 찾는다.

### 4. 돌진 (합의: 돌진 동작 신규, 속도는 이동 속도 × 2, 세부는 위임)
**SpawnPattern**: `movement` 열 추가

| 열 | 타입 | 의미 |
|---|---|---|
| movement | string | `Chase`(플레이어 추적) / `Charge`(돌진) |

- 몬스터가 아니라 패턴 속성으로 둔다. 돌진하는 몬스터는 일반 몬스터 그대로이고, 3의 패턴 덮어쓰기로 스테이지마다 돌진 여부도 함께 바뀐다.
- 클라이언트 `SpawnPatternData`
  ```csharp
  public enum SpawnMovement { Chase, Charge }

  public string movement;   // "Chase" / "Charge"
  public SpawnMovement Movement => parsedMovement;
  public bool OnLoaded() => Enum.TryParse(formation, out parsedFormation) && Enum.TryParse(movement, out parsedMovement);
  ```
  - `JsonDataManager.cs:546-547`의 실패 메시지에 formation과 movement 값을 함께 적는다. 빈 값도 실패다(모든 행을 채운다).
- 서버: SpawnPattern DTO는 patternId, spawnCount만 읽으므로 변경 없음.
- `WaveManager.SpawnPattern`
  ```csharp
  Enemy enemy = spawner.SpawnMonster(monsterId, position, hpScale, damageScale);
  if (enemy != null && enemy.Type == MonsterType.Normal && pattern.Movement == SpawnMovement.Charge)
  {
      Vector2 toTarget = center - position;
      enemy.StartCharge(toTarget, toTarget.magnitude * 2f);
  }
  ```
  - 방향: 스폰 순간의 플레이어 위치(`center`)를 향한다. 마리마다 자기 위치에서 계산하므로 Cluster는 거의 나란히 날아가고, Ring은 안쪽으로 모인다.
  - 거리: 스폰 거리의 2배. 플레이어 자리를 지나 같은 거리만큼 더 간 뒤 반납된다.
  - 형태 제한: 거리가 스폰 거리에 비례하므로 Charge는 플레이어에서 떨어진 곳에 스폰하는 형태(Random, Ring, Line, Cluster)에만 쓴다. Screen에 붙이면 플레이어 근처에 스폰된 몬스터가 거의 바로 사라진다(거리가 정확히 0이면 추적으로 남는다). 검증은 넣지 않고 규칙을 서버 `StaticData.md`에 적는다("문서 갱신 대상").
  - 대상: Normal만 돌진한다. 엘리트·보스(`BossMonster`)도 Chase 상태에서 `base.Move()`를 부르므로, 돌진을 받으면 거리를 다 쓴 뒤 `Die(false)`로 반납된다. 보스라면 `OnBossKilled` 없이 사라져 승리할 수 없고, 시간 제한이 없어 스테이지가 끝나지 않는다. 그래서 슬롯 규칙이 아니라 이 조건으로 막는다.
  - `TestSpawnPattern`도 같은 메서드를 거치므로 패턴 10으로 돌진을 바로 확인할 수 있다.
- `Enemy`
  ```csharp
  [Header("Charge")]
  [SerializeField] private float chargeSpeedMultiplier = 2f;   // 돌진 속도 = monsterMoveSpeed × 배율

  private Vector3 chargeDirection;
  private float chargeRemaining;                                // 남은 돌진 거리. 0이면 추적

  public void StartCharge(Vector2 direction, float distance)
  // direction이 0이면 Vector2.right (BossMonster.cs:199와 같은 대비)
  ```
  - `Init`: `chargeRemaining = 0f` (풀 재사용 시 이전 돌진이 남지 않게)
  - `Move`: `chargeRemaining > 0`이면 `chargeDirection`으로 `monsterMoveSpeed × chargeSpeedMultiplier × deltaTime`만큼 이동하고 남은 거리를 줄인다. 0 이하가 되면 `Die(false)`(보상·킬 수 없이 반납). 아니면 지금처럼 추적한다.
  - `Update`: `Move()` 다음에 `if (isDead) return;`. 반납된 프레임에 `Monster.Tick`이 공격하지 않게 한다(`ObjectPoolManager.ReturnObject`는 `OnDespawn`을 부르지 않아 공격 목록이 남아 있다).
  - 접촉 피해(`Attack`)와 MonsterAttack 공격은 돌진 중에도 그대로다.
  - `BossMonster.Move`는 `base.Move()`를 부르지만 `WaveManager`의 Normal 조건 때문에 `StartCharge`를 받지 않으므로 추적만 한다.
  - `chargeSpeedMultiplier`는 일반 몬스터 프리팹의 인스펙터 값이다.

### 5. 데이터 값
**Monster** 추가 (13002를 복사. 스테이지 1 보스 13001(skull_archer)과 외형이 다르다)

| monsterId | monsterName | monsterType | monsterMaxHealthPoint | monsterMoveSpeed | monsterContactDamage | monsterAsset |
|---|---|---|---|---|---|---|
| 15001 | 중간보스1 | MidBoss | 130 | 1.5 | 12 | Enemy/skull_warrior |

**MonsterAttack** 추가 (130021, 130022를 복사)

| monsterAttackId | monsterId | monsterAttackType | monsterAttackCooldown | monsterAttackTriggerRange | monsterAttackDamage | monsterAttackCount | monsterAttackAngle | monsterAttackDuration |
|---|---|---|---|---|---|---|---|---|
| 150011 | 15001 | Melee | 5.0 | 5.0 | 18 | 0 | 0 | 0 |
| 150012 | 15001 | Area | 7.0 | 6.0 | 18 | 1 | 0 | 0 |

**DropTable** 추가 (엘리트 드롭을 키운 것)

| monsterType | dropGroup | dropItemType | weight | count |
|---|---|---|---|---|
| MidBoss | 0 | ExpGem3 | 1 | 1 |
| MidBoss | 1 | LuckyBox | 1 | 1 |
| MidBoss | 2 | RewardBox | 1 | 3 |

- 엘리트는 ExpGem1 ×1, LuckyBox ×1, RewardBox ×2다. 경험치 보석 등급과 보상상자 수를 올렸다. 행운상자는 하나가 행운열차 한 번이라 1개로 둔다.

**SpawnPattern**: 모든 행에 movement를 채우고 10번 행을 추가한다

| patternId | formation | spawnCount | spawnRadius | formationSize | movement |
|---|---|---|---|---|---|
| 1 | Random | 1 | 0 | 0 | Chase |
| 2 | Random | 2 | 0 | 0 | Chase |
| 4 | Random | 3 | 0 | 0 | Chase |
| 6 | Screen | 1 | 0 | 0 | Chase |
| 7 | Ring | 12 | 0 | 0 | Chase |
| 8 | Line | 8 | 0 | 0 | Chase |
| 9 | Cluster | 6 | 0 | 0 | Chase |
| 10 | Cluster | 8 | 0 | 0 | Charge |

**Wave** (웨이브 1 교체)

| waveEntryId | waveId | patternStartTime | patternDuration | spawnInterval | patternId | monsterSlot | 구간 |
|---|---|---|---|---|---|---|---|
| 1 | 1 | 0 | 240 | 1.5 | 2 | N1 | 노말1 |
| 2 | 1 | 120 | 0 | 0 | 1 | E1 | 엘리트1 |
| 3 | 1 | 140 | 0 | 0 | 7 | N2 | 노말2 원 포위 |
| 4 | 1 | 180 | 0 | 0 | 7 | N2 | 노말2 원 포위 |
| 5 | 1 | 140 | 100 | 20 | 10 | N3 | 노말3 군집 돌격 |
| 6 | 1 | 240 | 0 | 0 | 1 | M1 | 중간 보스 |
| 7 | 1 | 260 | 120 | 1.0 | 4 | N4 | 노말4 |
| 8 | 1 | 360 | 0 | 0 | 1 | E2 | 엘리트2 |
| 9 | 1 | 380 | 100 | 1.5 | 2 | N5 | 노말5 |
| 10 | 1 | 480 | 0 | 0 | 1 | B1 | 보스 |
| 11 | 1 | 10 | 470 | 20 | 6 | BOX | 상자 |

- 노말3은 [140, 240) 동안 20초 간격이라 2:20, 2:40, 3:00, 3:20, 3:40에 다섯 번 나온다. 구간 끝(240)은 포함하지 않으므로 4:00에는 나오지 않는다.
- 노말4는 초당 3마리로 노말1(초당 약 1.3마리)의 약 2.3배다.

**StageRoster** (스테이지 1, 기존 6행 교체)

| stageRosterId | stageId | monsterSlot | monsterId | monsterHpScale | monsterDamageScale | patternId |
|---|---|---|---|---|---|---|
| 1 | 1 | N1 | 11001 | 1.0 | 1.0 | 0 |
| 2 | 1 | N2 | 11008 | 0.5 | 0.5 | 0 |
| 3 | 1 | N3 | 11001 | 1.0 | 1.0 | 0 |
| 4 | 1 | N4 | 11003 | 1.0 | 1.0 | 0 |
| 5 | 1 | N5 | 11010 | 1.0 | 1.0 | 0 |
| 6 | 1 | E1 | 12001 | 1.0 | 1.0 | 0 |
| 7 | 1 | E2 | 12002 | 1.0 | 1.0 | 0 |
| 8 | 1 | M1 | 15001 | 0.6 | 0.6 | 0 |
| 9 | 1 | B1 | 13001 | 1.0 | 1.0 | 0 |
| 10 | 1 | BOX | 14001 | 1.0 | 1.0 | 0 |

- 강도 기준(체력): 약함 ≈ 10(11001), 보통 ≈ 20(11003), 강함 ≈ 56(11010).
- N2(11008, 체력 44, 원거리 피해 3)는 배율 0.5로 보통에 맞춘다. 체력 22, 접촉 피해 2, 원거리 피해 2가 된다.
- M1은 체력 78, 접촉 피해 7, Melee 11, Area 11이다. 보스 13001(체력 100)보다 약하다.
- N3의 돌진 속도는 11001의 이동 속도 1.0 × 2 = 2.0이다.
- 슬롯 이름 규칙에 중간 보스 `M1`…을 추가한다. 규칙은 지금 WorkPlan 06에만 있으므로 서버 `StaticData.md`에 적는다("문서 갱신 대상").
- 다른 스테이지는 N2·N3 행의 patternId만 바꿔 특색을 준다(예: N2 → 8 Line, N3 → 9 Cluster 추적).

### 6. JsonDataManagerEditor
- SpawnPattern: 라벨을 `{formation} × {spawnCount} / {movement}`로 한다. 펼치면 이동 문자열과 파싱된 `Movement`를 표시한다.
- StageRoster 행: "패턴 ID (0이면 웨이브 값)"을 표시한다.
- Monster의 종류 EnumPopup은 enum에 값이 추가되어 그대로 표시된다.

### 7. 버전
- 클라이언트 `STATIC_DATA_MAJOR_VERSION = 4`, 서버 `data/version.txt` 4.0.0 (SheetExporter 첫째 자리 +1).
- 버전 3 클라이언트는 DropTable의 "MidBoss"를 파싱하지 못하고, StageRoster의 `patternId`와 SpawnPattern의 `movement`를 쓰지 않는다. 그래서 첫째 자리를 올린다.

### 8. 문서 갱신 대상
- 클라이언트 `Docs/ETC/GameData_Tags.md`
  - `:177`, `:178`, `:201`, `:203`: "ExpGem2~4는 떨어뜨리는 코드가 없다", "ExpGem1로 고정", "ExpGem1만 쓴다"를 고친다. MidBoss가 ExpGem3를 떨어뜨린다.
- 서버 `Docs/Guide/StaticData.md`
  - `:38`: StageRoster 열에 patternId
  - `:141`: `getStageRosters()` 반환 타입 `Map<Integer, Map<String, StageRosterData>>`
  - `:221`: StageRoster 검증에 "patternId가 0이거나 SpawnPattern에 있음"
  - `:224`: MonsterAttack 검증 "BOSS·MIDBOSS가 아니면 MELEE가 아님"
  - `:225`: DropTable 검증의 monsterType 목록에 MidBoss
  - `:234` "스테이지를 추가하려면?": 슬롯 이름 규칙(일반 `N1`…, 엘리트 `E1`…, 중간 보스 `M1`…, 보스 `B1`…, 상자 `BOX`)과 patternId로 특색 패턴을 바꾸는 방법, Charge는 Random·Ring·Line·Cluster에만 쓴다는 규칙

## 의존 순서 / 병렬 가능 그룹
- **그룹 A, 데이터**: #1 → #2. #2는 유저가 한다.
- **그룹 B, 서버**: #3 → #4·#5. #4의 `stageRosters` 타입 변경으로 `BattleService.java:229`가 #5 전까지 컴파일되지 않으므로 #4와 #5는 같이 한다. 새 데이터(#2) 없이는 서버가 시작되지 않으므로 #2 뒤에 빌드·실행으로 확인한다.
- **그룹 C, 클라이언트**: #6, #7, #8 → #9, #10, #11, #13, #14 → #12. 모두 필드·멤버를 추가하는 변경이라 이 순서면 중간에 컴파일이 깨지지 않는다. 끝에 OmniSharp 진단과 Unity 콘솔로 확인한다.
- B와 C는 병렬로 진행할 수 있다.
- **직렬**: A, B, C 완료 → #15 → #16

## 기준값
스테이지 1 서버 상한의 기대값이다(`computeStageLimit`, #5 후). WorkPlan 06 #1과 같은 방법(큰 값으로 결과를 제출하고 `clamp` 경고 로그의 상한 확인)으로 맞는지 본다.

| 행 | 최대 스폰 | 킬 | 골드 | 보상상자 | 행운상자 |
|---|---|---|---|---|---|
| N1 | (240 / 1.5 + 1) × 2 = 322 | 322 | 0 | 0 | 0 |
| N2 ×2 | 12 + 12 = 24 | 24 | 0 | 0 | 0 |
| N3 | (100 / 20 + 1) × 8 = 48 | 48 | 0 | 0 | 0 |
| N4 | (120 / 1.0 + 1) × 3 = 363 | 363 | 0 | 0 | 0 |
| N5 | (⌊100 / 1.5⌋ + 1) × 2 = 134 | 134 | 0 | 0 | 0 |
| E1, E2 | 1 + 1 | 2 | 0 | 2 + 2 | 1 + 1 |
| M1 | 1 | 1 | 0 | 3 | 1 |
| B1 | 1 | 1 | 0 | 5 | 0 |
| BOX | ⌊470 / 20⌋ + 1 = 24 | 0 | 24 × 300 = 7200 | 0 | 0 |
| 행운열차 | | | 3 × 300 × 5 = 4500 | | |
| **합계** | | **895** | **11700** | **12** | |

## 검증 항목
**컴파일·로드**
- 클라이언트 컴파일 에러가 없다(OmniSharp 진단, Unity 콘솔).
- 서버가 새 데이터로 빌드·시작된다(`CouServerApplicationTests` 포함). `data/version.txt`가 4.0.0이다.
- `persistentDataPath/StaticData`를 비우고 서버 없이 LogInScene을 실행하면 16개 테이블이 로드된다(Monster 31, MonsterAttack 52, DropTable 16, SpawnPattern 8, Wave 11, StageRoster 10). 로드 에러가 없다.
- `DropItemManager.prefabs`의 2번 칸(`DropItemType.ExpGem3`)에 `Prefabs/DropItem/ExpGem3.prefab`이 연결돼 있다(Unity CLI). ExpGem3는 이 작업에서 처음 드롭된다.
- 서버를 켠 상태로 LogInScene을 실행하면 클라이언트 콘솔에 `정적 데이터를 4.0.0(으)로 갱신했습니다.`가 남는다(`JsonDataManager.cs:176`).

**스테이지 1 진행**
- 0:00~4:00: 11001이 1.5초마다 2마리 나온다.
- 2:00: 12001이 나온다.
- 2:20, 3:00: 체력 22인 11008 12마리가 플레이어를 원형으로 둘러싸고 원거리 공격을 한다.
- 2:20부터 20초마다(다섯 번): 11001 8마리가 한쪽에 뭉쳐 나와, 스폰 순간의 플레이어 위치로 속도 2.0으로 직진한다. 피하면 그대로 지나가서 스폰 거리의 2배를 간 뒤 사라진다. 사라질 때 드롭과 킬 수가 없다.
- 4:00: 중간 보스 15001(체력 78)이 나온다. 일반·엘리트가 정리되고 상자는 남는다. 결계와 HP 바가 나오고(경험치 바는 숨겨짐), Melee 돌진·Area 공격을 한다. 보스전 동안 일반 스폰이 멈추고 HUD 시간은 흐른다.
- 중간 보스 처치: 승리하지 않는다. 결계와 HP 바가 사라지고 일반 스폰이 다시 시작된다. ExpGem3, LuckyBox(행운열차), RewardBox ×3이 떨어진다.
- 4:20~6:20: 11003이 1초마다 3마리 나온다. 6:00에 12002가 나온다.
- 6:20~8:00: 11010이 1.5초마다 2마리 나온다.
- 8:00: 보스 13001이 나오면 HUD 시간이 멈춘다. 처치 후 승리 대기 동안에도 멈춰 있고, 결과창 시간과 서버에 보낸 `seconds`가 보스 등장 시점 값이다.
- 상자는 0:10부터 20초마다 나오고, 두 보스전 동안에는 나오지 않는다.

**중간 보스전 건너뛰기**
- 중간 보스를 4:20 넘게 살려 두면, 노말4는 처치 시점부터 나오고 그 전 구간의 스폰이 몰려나오지 않는다.

**돌진 단독**
- `TestSpawnPattern`(testPatternId 10, testMonsterId 11001): 8마리가 한 방향에서 뭉쳐 나와 직진하고, 약 20(스폰 거리의 2배. Cluster 퍼짐 1.5 때문에 마리마다 약 17~23)을 간 뒤 사라진다.
- 돌진하던 몬스터가 풀에 돌아간 뒤 일반 패턴으로 다시 꺼내지면 추적한다(`Init` 초기화).
- `TestSpawnPattern`(testPatternId 10, testMonsterId 12001): 엘리트는 돌진하지 않고 추적하며 사라지지 않는다(Normal 조건).

**특색 패턴 덮어쓰기**
- 읽히는 쪽 `StageRoster.json`에서 N2의 patternId를 8로 바꾸면 2:20, 3:00에 11008 8마리가 Line으로 나온다. N3을 9로 바꾸면 군집 6마리가 추적한다. 확인 후 되돌린다.
- 서버: N1의 patternId를 4로 바꾸면 킬 상한이 895 → 1056(N1이 161 × 3 = 483)이 된다. 확인 후 되돌린다.

**데이터 오류**
- 클라이언트: SpawnPattern 한 행의 movement를 비우면 SpawnPattern 로드가 실패하고 메시지에 movement 값이 나온다. 확인 후 되돌린다.
- 클라이언트: StageRoster 한 행의 patternId를 -1로 바꾸면 StageRoster 로드가 실패한다. 확인 후 되돌린다.
- 서버: StageRoster 한 행의 patternId를 없는 ID(99)로 바꾸면 시작 검증이 실패하고 메시지에 행과 patternId가 나온다. 확인 후 되돌린다.
- 서버: DropTable에서 MidBoss 행을 모두 지우면 시작 검증이 실패한다. 확인 후 되돌린다.
- 서버: 15001의 Melee 행은 통과하고, 일반 몬스터에 Melee 행을 넣으면 시작 검증이 실패한다(기존 규칙 유지).

## 미해결 / 실행 시 확인 필요
- **수치**: 간격·마릿수·배율·드롭은 초기값이다. 노말4의 동시 개체 수(초당 3마리 × 2분)가 성능에 문제없는지 플레이 중 확인한다.
- **8:00까지 살아 있는 중간 보스**: 마지막 보스 등장 정리로 보상 없이 사라지고(`Die(false)`, `OnBossKilled` 없음) 마지막 보스로 바뀐다. `BattleManager`와 `BossArena`는 새 보스로 갱신되므로 별도 처리하지 않는다.
- **`OnDespawn` 미호출**: `ObjectPoolManager.ReturnObject`가 `IPoolable.OnDespawn`을 부르지 않는다. 이 작업은 `Update`의 `isDead` 검사로 피하고 고치지 않는다.
- **테스트 웨이브 제거**: 웨이브 1을 덮어써서 45초 보스 테스트 웨이브가 없어진다. 보스 확인은 `TestSpawnPattern`(testPatternId 1, testMonsterId 13001 또는 15001)으로 한다. testPatternId 기본값 7은 Ring 12마리라 보스가 연달아 나오고 서로 정리된다. WorkPlan 06 "동작 동일성" 항목은 더 이상 맞지 않는다.
- **Notion 데이터 정의서**: Monster(MidBoss 타입, 15001~ 대역), StageRoster(patternId), SpawnPattern(movement) 열 변경을 유저가 반영한다.
- **기기 저장본**: 버전 3 저장본은 첫째 자리가 달라 쓰이지 않는다(`LoadUsableSavedVersion`). 따로 지우지 않는다.

## 검증 결과
2026-10-13. 스테이지 진행은 에디터 하네스로 5배속 실행했다(플레이어 매 프레임 회복, 스킬 선택 자동, 중간 보스는 4:50·마지막 보스는 8:20까지 체력 유지 후 처치). 시각은 `WaveManager.Elapsed` 기준이다.

**컴파일·로드**
- 클라이언트 컴파일 에러 없음(`recompile_status` completed, `compilationFailed: false`).
- 서버 `gradlew test` 통과(`CouServerApplicationTests` 포함, `.env` 환경변수와 Redis 필요). 4.0.0 데이터로 기동하고 `/api/static-data`가 4.0.0을 응답한다.
- 서버 없이(기기 저장본 3.0.0은 첫째 자리가 달라 무시됨) LogInScene: 16개 테이블 로드, Monster 31 / MonsterAttack 52 / DropTable 16 / SpawnPattern 8 / Wave 11 / StageRoster 10, 로드 에러 없음.
- `DropItemManager.prefabs[2]` = `Prefabs/DropItem/ExpGem3.prefab`.

**스테이지 1 진행** (1회차, 콘솔 에러 0)
- 11001: 1.5초 간격(중앙값 1.50) 2마리, 마지막 238.56. 노말3 포함 총 360마리(320 + 40).
- 12001 120.07, 12002 360.06에 1마리씩.
- 11008: 140.06, 180.06에 12마리, 체력 22.
- 노말3: 140~220에 20초마다 11001 8마리. 처치되지 않은 돌진 몬스터는 스폰 거리의 1.97~2.02배를 간 뒤 킬 수 없이 사라졌다. 풀에서 다시 꺼내진 돌진 몬스터 26마리는 모두 플레이어에게 다가갔다(`Init` 초기화).
- 중간 보스 240.06: 체력 78, 등장 시 일반·엘리트 정리, 결계·HP 바 켜짐, 경험치 바 숨김, HUD 04:00(시간 흐름).
- 중간 보스 처치(290.06): 승리 없음, 결계·HP 바 꺼짐, 경험치 바 복귀. ExpGem3 1, LuckyBox 1, RewardBox 3 드롭.
- 건너뛰기: 11003은 290.64부터 1초 간격 3마리(몰려나옴 없음, 379.64까지 90회). 상자는 250, 270이 건너뛰어졌다(230.07 → 290.23).
- 11010: 380.06~479.05, 1.5초 간격 2마리 134마리.
- 보스 13001 480.05: 체력 100. 보스전 동안 보스 외 스폰 0건. 처치 후 승리.
- 처음 구현(보스가 나온 프레임에 바로 정지)에서는 HUD와 결과창이 07:59에서 멈췄다. 구현 결정 2처럼 다음 프레임부터 멈추게 고친 뒤 다시 실행(8배속)하니 480.08에 08:00이 되고 처치(486)와 승리 대기 동안 그대로였으며, 결과창도 08:00이다.
- 재실행에서 마지막 보스 등장 순간 살아 있던 상자 1개가 정리 후에도 남았다(alive 2 = 보스 + 상자).
- 서버로 보낸 `seconds`는 결과창 값과 같은 변수다. 가속 실행이라 서버는 실제 경과 + 5초로 잘랐다.

**특색 패턴 덮어쓰기** (2·3회차, 메모리의 로스터 값을 바꿔 실행. 로그인 후에는 서버 저장본이 읽혀 파일 대신 이 방법을 썼다)
- N2 → 8: 140.03, 180.03에 11008 8마리. N3 → 9: 11001 6마리가 추적하고 킬 없이 사라진 개체 0.
- E1 → 10(Charge): 엘리트 8마리를 체력 유지로 살려 두면 15.5초 뒤에도 8마리 모두 플레이어 0.5~2.1 거리에 있다(돌진하지 않고 추적).
- 서버 상한 기준값(8081에서 test7777로 큰 값 제출): kills 895, gold 11700, rewardBoxes 12. 기대값과 일치.
- 서버 N1 → 4: kills 999999·seconds 0·패배로 제출하면 grantedExp 1056(= 킬 상한 1056, 전투 15). 되돌린 뒤 같은 제출은 상한 895(전투 16).

**데이터 오류**
- 클라이언트 movement 빈 값: `SpawnPattern 4의 formation "Random" 또는 movement ""을(를) 알 수 없습니다.`로 로드 실패.
- 클라이언트 patternId -1: `StageRoster 3의 patternId는 0 이상이어야 합니다.`로 로드 실패.
- 서버 15001의 Melee 행은 기동 검증 통과.
- 서버 patternId 99: `StageRoster 2의 patternId 99을(를) SpawnPattern에서 찾을 수 없습니다.`로 기동 실패.
- 서버 일반 몬스터 Melee(110011, 11001): `MonsterAttack 110011: 보스·중간 보스가 아닌 몬스터(monsterId 11001)는 MELEE를 쓸 수 없습니다.`로 기동 실패.
- 서버 MidBoss DropTable 삭제: `DropTable에는 모든 monsterType([NORMAL, ELITE, MIDBOSS, BOSS, BOX])의 행이 있어야 합니다.`로 기동 실패.
- 서버 검증은 첫 오류에서 멈추므로, Melee와 DropTable은 오류 하나만 남긴 데이터 폴더를 클래스패스 앞에 두고 따로 기동해 확인했다.
