# WorkPlan 06: 스테이지·웨이브 구조 개편 (StageWaveRestructure)

## 목표
- 웨이브를 스테이지에서 떼어 여러 스테이지가 공유하는 리듬 템플릿으로 만든다. 웨이브는 몬스터 대신 슬롯을 부르고, 스테이지마다 로스터(StageRoster)가 슬롯을 채운다.
- 몬스터 스탯 난이도를 Monster 행에서 StageRoster 행의 배율로 옮긴다. 외형·속도·공격 구성이 같은 변형은 한 행으로 합친다.
- 테이블마다 한 가지 역할만 맡긴다. SpawnPattern은 모양, Wave는 시각과 반복, StageRoster는 스테이지별 몬스터와 배율, Monster는 종류와 역할(MonsterType), DropTable은 역할별 드롭을 맡는다.
- 입력: 유저와의 구조 논의(스테이지 배율, 슬롯·로스터, 부수 정리 전체 포함. 이후 리뷰에서 배율 위치를 Stage에서 StageRoster 행으로, 드롭 기준을 몬스터별 dropTableId에서 역할(MonsterType)로 변경). 별도 기획명세는 없다.

## 범위
**포함**
- 데이터: Stage, Wave, SpawnPattern, Monster, DropTable 탭 변경과 StageRoster 탭 추가 (시트 → SheetExporter)
- 서버(`COU_Server`): DTO, `StaticDataService` 로드·검증, `BattleService` 스테이지 상한 계산, 문서
- 클라이언트: 데이터 클래스, `JsonDataManager`, `WaveManager`, `MonsterSpawner`, `DropItemManager`, `Enemy`·`Box`·`Monster`·`BossMonster`, `JsonDataManagerEditor`, 문서
- 정적 데이터 첫째 자리 버전 2 → 3

**제외**
- 스테이지 2 이후의 Stage·StageRoster 행과 배율 값(기획)
- 웨이브 템플릿 추가. 이번에는 웨이브 1만 새 형식으로 옮긴다
- 스테이지 안에서 시간에 따라 오르는 배율. 슬롯(N1 → N2)으로 표현한다
- 스테이지별 드롭 양 차등(예: 뒤 스테이지 상자 Gold 증가). 필요해지면 Stage 쪽에 따로 둔다
- 같은 역할 안의 드롭 예외(특정 몬스터만 다른 드롭). 필요해지면 "0이면 역할 기본값"인 덮어쓰기 열을 그때 추가한다
- Notion 데이터 정의서 갱신(공개 페이지 API는 읽기만 된다. 유저가 갱신한다)

## 사전 조건
- **다른 담당자 파일 수정**: `MonsterSpawner.cs`, `Enemy.cs`, `Box.cs`는 승희님 담당이다. `Monster.cs`, `BossMonster.cs`도 담당자를 확인한다. 작업 전에 아래 공개 API 변경을 알린다.

| 변경 | 이전 | 이후 |
|---|---|---|
| 스폰 호출 | `MonsterSpawner.SpawnMonster(int monsterId, Vector3 position, int dropTableId)` | `SpawnMonster(int monsterId, Vector3 position, float hpScale, float damageScale)` |
| 몬스터 초기화 | `Enemy.Init(MonsterData data, int dropTableId)` | `Init(MonsterData data, float hpScale, float damageScale)` |
| 드롭 테이블 | `Enemy.DropTableId` | 삭제. `GiveReward`가 역할(`Type`)로 드롭한다 |
| 드롭 호출 | `DropItemManager.SpawnTable(int dropTableId, Vector2 position)` | `SpawnTable(MonsterType type, Vector2 position)` |
| 체력 | `Data.monsterMaxHealthPoint` | `Enemy.MaxHp` (배율 적용값) |
| 공격 피해 | `attack.monsterAttackDamage`, `Data.monsterContactDamage` 직접 사용 | `ScaleDamage(...)`를 거친다 |
| 보스 판정 | `SpawnPatternData.EventType` | 삭제. `MonsterData.Type`만 쓴다 |

- **배포 순서**: 서버 DTO가 새 열을 필수로 읽는다(`FAIL_ON_MISSING_CREATOR_PROPERTIES`). 시트 내보내기, 서버 코드, 클라이언트 코드를 한 번에 반영한다. 버전 2 클라이언트는 앱 업데이트 요구를 받는다.

## 구조 개요

```
이전                                          이후
Stage ──1:1──▶ Wave(monsterId)                Stage ──N:1──▶ Wave(시각·반복·슬롯)
                 │                              │                │
                 ▼                              ▼                ▼
           SpawnPattern                     StageRoster      SpawnPattern(모양)
           (모양+타이밍+드롭+eventType)       (슬롯 → monsterId, 배율)
                                                │
Monster(스테이지별 변형 49행)                      ▼
                                             Monster(종류 30행, monsterType)
DropTable(dropTableId)                           │
                                                 ▼
                                             DropTable(monsterType)

WaveManager.SetupStage
  Wave 행 ─ monsterSlot ─▶ StageRoster 행(stageId, 슬롯) ─▶ MonsterData
         ─ patternId ───▶ SpawnPatternData
  스폰: MonsterSpawner.SpawnMonster(monsterId, 위치, 로스터.monsterHpScale, 로스터.monsterDamageScale)
  처치: DropItemManager.SpawnTable(Type)
```

스테이지를 하나 추가할 때 Stage 1행과 StageRoster 슬롯 수만큼의 행을 넣는다. Wave와 Monster는 재사용한다.

## 작업 항목
| # | 작업 단위 | 산출물(클래스/파일 경로) | 의존 |
|---|---|---|---|
| 1 | 기준값 기록: 변경 전 서버로 스테이지 1에 입장하고 kills·gold·rewardBoxes를 큰 값으로 제출해 `clamp` 경고 로그의 상한을 적는다 | 본 문서 "기준값" 절 | - |
| 2 | 시트 붙여넣기용 TSV 작성 (Stage, StageRoster, Wave, SpawnPattern, Monster, DropTable) | 스크래치 파일 | - |
| 3 | (유저) 시트 반영, SheetExporter 내보내기. 첫째 자리 +1, 클라이언트 복제 체크 | 서버 `src/main/resources/data/`, 클라이언트 `Assets/Resources/Data/` | #1, #2 |
| 4 | 서버 DTO: `StageData`, `WaveEntryData`, `SpawnPatternData`, `DropTableEntryData` 변경, `StageRosterData` 추가, `SpawnEventType` 삭제 | `COU_Server/src/main/java/com/couserver/staticdata/dto/` | - |
| 5 | 서버 `StaticDataService`: StageRoster 로드, `getStageRosters()`, 검증 규칙 이동·추가 | `StaticDataService.java` | #4 |
| 6 | 서버 `BattleService`: `computeStageLimit`, `maxSpawnCount` | `BattleService.java` | #5 |
| 7 | 경로 상수 `StageRosterData_Json_Path = "Data/StageRoster"`, `STATIC_DATA_MAJOR_VERSION = 3` | `Assets/Scripts/Utils/GameConstants.cs` | - |
| 8 | `StageRosterData` 클래스 | `Assets/Scripts/Data/StageRosterData.cs` | - |
| 9 | 데이터 클래스 변경: `StageData`, `WaveEntryData`, `SpawnPatternData`, `DropTableEntryData` | `Assets/Scripts/Data/` | - |
| 10 | `JsonDataManager`: StageRoster 로드·갱신·조회, DropTable 키를 `MonsterType`으로, 파싱 검증 변경 / `DropItemManager.SpawnTable` 시그니처 변경 | `Assets/Scripts/Mangers/JsonDataManager.cs`, `DropItemManager.cs` | #7, #8, #9 |
| 11 | `Enemy`·`Box`: 배율 적용, `DropTableId` 삭제, 역할로 드롭 | `Assets/Scripts/Enemy/Enemy.cs`, `Box.cs` | #9, #10 |
| 12 | `Monster`·`BossMonster`: 공격 피해 7곳에 `ScaleDamage` | `Monster.cs:89,107,126`, `BossMonster.cs:273,306,330,354` | #11 |
| 13 | `MonsterSpawner.SpawnMonster` 시그니처 변경 | `Assets/Scripts/Mangers/MonsterSpawner.cs` | #11 |
| 14 | `WaveManager`: 로스터 해석, 타이밍·배치 수치·보스 판정 출처 변경 | `Assets/Scripts/Mangers/WaveManager.cs` | #10, #13 |
| 15 | `JsonDataManagerEditor`: 바뀐 필드 표시, StageRoster 목록 | `Assets/Editor/JsonDataManagerEditor.cs` | #10 |
| 16 | 검증 | 본 문서 "검증 결과" | #3, #6, #14, #15 |
| 17 | 문서 갱신 | 클라이언트 `Docs/ETC/StaticDataClient.md`, `GameData_Tags.md`, `GameData_Issues_DevAkasha.md` / 서버 `Docs/Guide/StaticData.md` | #16 |

## 구현 결정

### 1. 데이터 계층 (합의: 슬롯·로스터, 배율, 부수 정리 전체)
| 테이블 | 맡는 일 | 참조 |
|---|---|---|
| Stage | 스테이지 정보, 웨이브 선택 | Wave(waveId) |
| StageRoster | 스테이지의 슬롯 → 몬스터, 그 몬스터의 배율 | Stage, Monster |
| Wave | 시각, 반복, 패턴, 슬롯 | SpawnPattern |
| SpawnPattern | 배치 형태, 마릿수, 배치 수치 | - |
| Monster | 종류 정의, 역할(monsterType) | - |
| DropTable | 역할별 드롭 | MonsterType(enum) |

- Stage ↔ Wave는 N:1이 된다. 여러 스테이지가 같은 waveId를 가리킬 수 있다.
- 웨이브가 쓰는 슬롯은 그 웨이브를 가리키는 모든 스테이지의 로스터에 있어야 한다(서버 검증).
- 스테이지에 따라 달라지는 몬스터 속성은 StageRoster 행에 둔다. StageRoster 행 하나가 "이 스테이지의 이 몬스터"다.
- 드롭의 종류와 개수는 역할(MonsterType)이 정하고, 보상의 질은 Stage(`rewardBoxGradeWeights`, `clearAccountExp`)가 정한다. 역할은 코드가 이미 쓰는 기준(프리팹 선택, 보스전 정리, 상자 킬 제외)과 같다.

### 2. 테이블 형식
**Stage**: `stageDuration` 삭제(합의)

**StageRoster** (신규)
| 열 | 타입 | 의미 |
|---|---|---|
| stageRosterId | int | 1부터 |
| stageId | int | Stage 참조 |
| monsterSlot | string | 슬롯 이름. 같은 stageId 안에서 중복 없음 |
| monsterId | int | Monster 참조 |
| monsterHpScale | float | 몬스터 최대 체력 배율 (0 초과) |
| monsterDamageScale | float | 접촉 피해와 MonsterAttack 피해 배율 (0 초과) |

- 슬롯 이름 규칙: 일반 `N1`, `N2`…, 엘리트 `E1`…, 보스 `B1`…, 상자 `BOX`. 코드는 문자열 일치만 본다.

**Wave**: `monsterId` 삭제, 타이밍을 SpawnPattern에서 옮김
| 열 | 타입 | 의미 |
|---|---|---|
| waveEntryId | int | 그대로 |
| waveId | int | 그대로 |
| patternStartTime | float | 그대로 |
| patternDuration | float | SpawnPattern에서 이동. 0이면 시작 시각에 한 번 |
| spawnInterval | float | SpawnPattern에서 이동. patternDuration이 0보다 크면 0보다 커야 한다 |
| patternId | int | 그대로 |
| monsterSlot | string | StageRoster의 슬롯 |

**SpawnPattern**: `eventType`, `spawnInterval`, `patternDuration`, `dropTableId` 삭제, 배치 수치 추가
| 열 | 타입 | 의미 |
|---|---|---|
| patternId | int | 그대로 |
| formation | string | 그대로 |
| spawnCount | int | 그대로 |
| spawnRadius | float | 플레이어로부터의 거리. 0이면 `WaveManager` 인스펙터 기본값 |
| formationSize | float | Line 전체 길이 / Cluster 퍼짐 반경. 0이면 인스펙터 기본값(lineLength, clusterRadius) |

- "0이면 인스펙터 기본값"은 MonsterAttack(`monsterAttackCount` 등)과 같은 규칙이다. 인스펙터 필드 `spawnRadius`, `lineLength`, `clusterRadius`는 기본값으로 남는다.

**Monster**: 열 변경 없음

**DropTable**: `dropTableId`를 `monsterType`으로 바꿈
| 열 | 타입 | 의미 |
|---|---|---|
| monsterType | string | `Normal` / `Elite` / `Boss` / `Box`. Monster의 monsterType과 같은 값 |
| dropGroup, dropItemType, weight, count | | 그대로 |

- 모든 MonsterType에 행이 있어야 한다(서버 검증). 드롭이 없는 역할은 `None` 한 행을 둔다.

### 3. 데이터 값
**Monster 통합**: 타입·외형·속도·공격 구성(MonsterAttack의 피해 외 열)이 같은 행을 가장 작은 ID로 합친다. 49행 → 30행이다. 웨이브 1이 쓰는 11001, 11002, 12001, 12002, 13001, 14001은 모두 남는다. 합쳐지는 행은 공격이 없어서 MonsterAttack(50행)은 바뀌지 않는다.

| 남는 행 | 삭제 행 | 삭제 행 체력 / 접촉 피해 (배율을 정할 때 참고) |
|---|---|---|
| 11001 goblin | 11004, 11007, 11011, 11015 | 10·24·38·62·90 / 1·2·3·5·7 |
| 11003 orange_slime | 11006, 11009, 11013, 11017 | 20·34·50·75·108 / 2·3·4·6·8 |
| 11010 goblin_archer | 11014, 11018 | 56·82·120 / 4·6·9 |
| 12001 goblin_green_warrior | 12004, 12007, 12010, 12013 | 40·70·100·130·165 / 4·7·10·13·16 |
| 12002 orange_slime_king | 12005, 12008, 12011, 12014 | 50·80·110·140·180 / 5·8·11·14·17 |
| 12003 skull | 12006 | 60·90 / 6·9 |

- 슬라임(11002, 11005, 11008, 11012, 11016), 공격을 가진 엘리트(12009, 12012, 12015), 보스 13001~13015는 공격 구성이 서로 달라 그대로 둔다.

**SpawnPattern**: ID를 유지하고, 모양만 남기면 1번과 같아지는 3(엘리트), 5(보스)를 삭제한다. `testPatternId` 7(Ring)이 그대로 유효하다.
| patternId | formation | spawnCount | spawnRadius | formationSize |
|---|---|---|---|---|
| 1 | Random | 1 | 0 | 0 |
| 2 | Random | 2 | 0 | 0 |
| 4 | Random | 3 | 0 | 0 |
| 6 | Screen | 1 | 0 | 0 |
| 7 | Ring | 12 | 0 | 0 |
| 8 | Line | 8 | 0 | 0 |
| 9 | Cluster | 6 | 0 | 0 |

**Wave** (웨이브 1, 동작은 지금과 같다)
| waveEntryId | waveId | patternStartTime | patternDuration | spawnInterval | patternId | monsterSlot |
|---|---|---|---|---|---|---|
| 1 | 1 | 0 | 10 | 1.0 | 1 | N1 |
| 2 | 1 | 10 | 10 | 2.0 | 2 | N2 |
| 3 | 1 | 15 | 0 | 0 | 1 | E1 |
| 4 | 1 | 20 | 10 | 0.5 | 4 | N1 |
| 5 | 1 | 25 | 0 | 0 | 1 | E2 |
| 6 | 1 | 45 | 0 | 0 | 1 | B1 |
| 7 | 1 | 10 | 140 | 20.0 | 6 | BOX |

**StageRoster** (스테이지 1)
| stageRosterId | stageId | monsterSlot | monsterId | monsterHpScale | monsterDamageScale |
|---|---|---|---|---|---|
| 1 | 1 | N1 | 11001 | 1.0 | 1.0 |
| 2 | 1 | N2 | 11002 | 1.0 | 1.0 |
| 3 | 1 | E1 | 12001 | 1.0 | 1.0 |
| 4 | 1 | E2 | 12002 | 1.0 | 1.0 |
| 5 | 1 | B1 | 13001 | 1.0 | 1.0 |
| 6 | 1 | BOX | 14001 | 1.0 | 1.0 |

**배율 값을 정하는 규칙** (스테이지 2 이후 기획에서 사용)
- 배율은 가리키는 Monster 행 수치에 대한 상대값이다. Monster 행을 기준값으로 환산하지 않는다.
- 통합으로 삭제한 행의 수치를 옮기려면 `삭제 행 수치 / 남는 행 수치`를 쓴다. 예: 11004 자리에 11001을 쓰면 체력 24/10 = 2.4, 피해 2/1 = 2.0
- 스테이지 전용으로 만든 행(보스 13001~13015 등)과 상자는 1.0으로 둔다.
- 스테이지 단위로 한 번에 조절하려면 시트에서 로스터 배율 칸을 수식(예: 스테이지 기준 배율 × 타입 계수)으로 채운다. SheetExporter는 TSV로 받으므로 수식이 아니라 표시값이 나간다. 셀 표시 형식이 소수 자리를 잘라내지 않게 둔다.

**DropTable**: 테이블 1~4의 키를 Normal, Elite, Boss, Box로 바꾸고 행 내용은 그대로 둔다. 지금 패턴별 드롭과 같은 결과다. 테이블 5(Ring·Line·Cluster 샘플 패턴용)는 삭제한다. 샘플 패턴의 드롭은 `testMonsterId` 몬스터의 역할을 따른다.

### 4. 배율 적용 (합의: 배율 도입 + 변형 통합, 배율 위치는 StageRoster 행)
```csharp
// Enemy
public int MaxHp { get; private set; }
public float HpRatio => MaxHp <= 0 ? 0f : (float)currentHp / MaxHp;
private float damageScale;

public void Init(MonsterData data, float hpScale, float damageScale)
// MaxHp = Mathf.Max(1, Mathf.RoundToInt(data.monsterMaxHealthPoint * hpScale))
// currentHp = MaxHp

protected int ScaleDamage(int baseDamage)
    => baseDamage <= 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(baseDamage * damageScale));
```
- 접촉 피해(`Enemy.Attack`)와 MonsterAttack 피해 7곳이 `ScaleDamage`를 거친다.
- 반올림: 체력은 최소 1, 피해는 원래 값이 0보다 크면 최소 1이다. 배율이 1보다 작아도 피해가 0이 되지 않는다.
- 이동 속도, 쿨다운, 사거리, 발 수는 배율을 받지 않는다.
- `GiveReward`(Enemy, Box)는 `DropItemManager.SpawnTable(Type, transform.position)`을 부른다. `DropTableId` 속성은 삭제한다.
- `DropItemManager.SpawnTable(MonsterType type, Vector2 position)`: `GetDropTableFromJson(type)`으로 행을 찾는다. `dropTableId <= 0` 검사는 없어진다. 추첨 규칙은 그대로다.
- `MonsterSpawner.SpawnMonster(int monsterId, Vector3 position, float hpScale, float damageScale)`는 받은 배율을 `Init`에 넘긴다. 보스 정리, 풀 선택, 구독은 그대로다.

### 5. WaveManager
- `SetupStage`
  1. `CurrentStage`가 없으면 지금처럼 에러 후 반환한다.
  2. `GetStageRosterFromJson(stageId)`가 null이면 에러를 남기고 반환한다.
  3. 웨이브 행마다 패턴을 찾고, 로스터에서 `monsterSlot`의 행을 찾고, 그 행의 monsterId로 `GetMonsterDataFromJson`을 호출한다. 하나라도 없으면 경고를 남기고 그 행을 건너뛴다.
  - 지금의 드롭 테이블 존재 검사(`WaveManager.cs:87`)는 삭제한다. 모든 역할에 드롭 행이 있는지는 서버가 시작할 때 검사한다.
- 상태 항목: `WaveEntryData data`, `SpawnPatternData pattern`, `StageRosterData roster`, `MonsterData monster`, `float nextSpawnTime`, `bool finished`
- `ProcessEvent`: 반복 여부와 간격은 `data.patternDuration`, `data.spawnInterval`에서 읽는다. 보스전 중 정지 예외는 `monster.Type == MonsterType.Boss`로 판정한다. 나머지 규칙은 그대로다.
- `SpawnPattern(int monsterId, SpawnPatternData pattern, float hpScale, float damageScale)`: 거리는 `pattern.spawnRadius > 0 ? pattern.spawnRadius : spawnRadius`, Line 길이와 Cluster 반경은 `formationSize`에 같은 규칙을 적용한다. `ProcessEvent`는 `roster.monsterHpScale`, `roster.monsterDamageScale`을 넘긴다.
- `TestSpawnPattern`은 그대로 `testMonsterId`와 `testPatternId`를 쓰고, 배율 1, 1을 넘긴다.

### 6. 클라이언트 JsonDataManager
- StageRoster: `Dictionary<int, List<StageRosterData>>`(키 stageId). DropTable과 같이 파싱할 때 묶는다.
  - 조회: `IReadOnlyList<StageRosterData> GetStageRosterFromJson(int stageId)` (기존 Get 메서드와 같은 null·미존재 처리)
  - 에디터용: `IReadOnlyDictionary<int, List<StageRosterData>> StageRosterDic`
  - `Awake`와 `ApplyStaticDataResponse`에 추가해 13개 테이블이 된다.
- DropTable: 키를 `MonsterType`으로 바꾼다. `Dictionary<MonsterType, List<DropTableEntryData>>`, `GetDropTableFromJson(MonsterType type)`, `DropTableDic`도 같은 키다.
  - `DropTableEntryData`: `dropTableId` 대신 `monsterType` 문자열. `OnLoaded()`가 dropItemType과 함께 MonsterType으로 파싱한다.
- 파싱 검증 (위반하면 예외)
  | 테이블 | 규칙 |
  |---|---|
  | StageRoster | ID 1 이상·중복 없음, stageId > 0, monsterId > 0, monsterSlot이 비어 있지 않음, 같은 stageId 안에서 monsterSlot 중복 없음, monsterHpScale > 0, monsterDamageScale > 0 |
  | Wave | patternStartTime ≥ 0, patternDuration ≥ 0, patternDuration > 0이면 spawnInterval > 0, monsterSlot이 비어 있지 않음 |
  | SpawnPattern | spawnCount ≥ 1(그대로), formation 파싱, spawnRadius ≥ 0, formationSize ≥ 0 |
  | DropTable | `dropTableId > 0` 대신 monsterType 파싱. 나머지 그대로 |
- 테이블 사이의 참조는 서버가 시작할 때 검사하고, 클라이언트는 `WaveManager.SetupStage`에서 경고 후 건너뛴다(지금 방식).
- `SpawnEventType` enum과 `SpawnPatternData.EventType`을 삭제한다. `OnLoaded()`는 formation만 파싱한다.

### 7. 서버
- DTO
  ```java
  record StageData(int stageId, int waveId, int clearAccountExp, List<Integer> rewardBoxGradeWeights)
  record StageRosterData(int stageRosterId, int stageId, String monsterSlot, int monsterId,
                         double monsterHpScale, double monsterDamageScale)
  record WaveEntryData(int waveEntryId, int waveId, double patternStartTime, double patternDuration,
                       double spawnInterval, int patternId, String monsterSlot)
  record SpawnPatternData(int patternId, int spawnCount)
  record DropTableEntryData(MonsterType monsterType, int dropGroup, DropItemType dropItemType, int weight, int count)
  ```
  - `SpawnEventType.java` 삭제. `MonsterData`는 그대로다.
- `StaticDataService`: `Map<Integer, Map<String, Integer>> stageRosters` (stageId → 슬롯 → monsterId), `Map<MonsterType, List<DropTableEntryData>> dropTables`
- 검증 (이동·추가분)
  | 대상 | 규칙 |
  |---|---|
  | StageRoster | ID 규칙, stageId가 Stage에, monsterId가 Monster에, 같은 stageId 안에서 monsterSlot 중복 없음, monsterHpScale > 0, monsterDamageScale > 0 |
  | Wave | patternStartTime ≥ 0, patternDuration ≥ 0, patternDuration > 0이면 spawnInterval > 0 (SpawnPattern에서 이동), patternId가 SpawnPattern에 |
  | Stage × Wave | 스테이지 웨이브의 monsterSlot이 모두 그 스테이지 로스터에 있음 |
  | SpawnPattern | spawnCount ≥ 1 (dropTableId 참조 검사는 삭제) |
  | DropTable | `dropTableId > 0` 검사 삭제. 모든 MonsterType에 행이 있음 |
- `BattleService.computeStageLimit`: 웨이브 행마다 로스터로 몬스터를 찾고, 스폰 수는 `maxSpawnCount(entry, pattern)`(행의 patternDuration·spawnInterval × 패턴의 spawnCount), 드롭은 `dropTables.get(monster.monsterType())`로 센다. 스테이지 1의 상한은 #1 기준값과 같아야 한다.
- 상한은 배율과 관계없다(킬 수·드롭 수만 센다).

### 8. JsonDataManagerEditor
- Wave: 라벨 `웨이브 {waveId} / 패턴 {patternId} / {monsterSlot}`. 지속 시간, 반복 간격, 슬롯을 표시하고 몬스터 ID를 지운다.
- SpawnPattern: 라벨 `{formation} × {spawnCount}`. 이벤트 종류, 반복 간격, 지속 시간, 드롭 테이블을 지우고 거리, 형태 크기를 표시한다.
- Stage: 진행 시간을 지운다.
- DropTable: 키가 `MonsterType`으로 바뀐다(`expandedDropTables`도 같은 키). 행 표시는 그대로다.
- StageRoster: 라벨 `슬롯 {n}개`, 펼치면 행마다 슬롯, 몬스터 ID, 체력 배율, 피해 배율을 표시한다(DropTable 방식).

## 의존 순서 / 병렬 가능 그룹
- **#1을 가장 먼저 한다.** 내보내기(#3) 뒤에는 변경 전 서버를 실행할 수 없다.
- **그룹 A, 데이터**: #2 → #3. #3은 유저가 한다.
- **그룹 B, 서버**: #4 → #5 → #6. 새 데이터(#3) 없이는 서버가 시작되지 않으므로 #3 뒤에 빌드·실행으로 확인한다.
- **그룹 C, 클라이언트**: #7, #8, #9 → #10 → #11 → #12, #13 → #14, #15
  - #9부터 #14까지는 중간에 컴파일이 깨진다. 한 묶음으로 진행하고 끝에 OmniSharp 진단과 Unity 콘솔로 확인한다.
- B와 C는 병렬로 진행할 수 있다.
- **직렬**: A, B, C 완료 → #16 → #17

## 기준값
#1 결과를 적는다.

| 스테이지 | kills | gold | rewardBoxes |
|---|---|---|---|
| 1 | | | |

## 검증 항목
**컴파일·로드**
- 클라이언트 컴파일 에러가 없다(OmniSharp 진단, Unity 콘솔).
- 서버가 새 데이터로 빌드·시작된다(`CouServerApplicationTests` 포함). `data/version.txt`가 3.0.0이다.
- `persistentDataPath/StaticData`를 비우고 서버 없이 LogInScene을 실행하면 13개 테이블이 로드된다(Monster 30, Wave 7, SpawnPattern 7, StageRoster 6, Stage 1). 로드 에러가 없다.
- 서버를 켜면 `정적 데이터를 3.0.0(으)로 갱신했습니다.`가 남고 `StageRoster.json`이 저장된다.

**동작 동일성 (스테이지 1, WorkPlan 04 후속 검증 결과와 같아야 한다)**
- 16.2초: 11001 10마리, 11002 8마리, 12001 1마리, 상자 1개
- 30.5초: 11001 70마리, 11002 10마리, 12001·12002 각 1마리, 상자 2개
- 45초에 보스 13001이 나오고, 일반·엘리트는 정리되고 상자는 남는다. 보스전 동안 일반 스폰이 멈추고, 처치 뒤 몰려나오지 않는다.
- 드롭: 일반 ExpGem1 ×1, 엘리트 ExpGem1·LuckyBox ×1·RewardBox ×2, 보스 ExpGem1·RewardBox ×5, 상자 7종 중 1개(킬 수 0)
- 서버 상한이 기준값과 같다(#1과 같은 방법으로 확인).

**배율**
- 읽히는 쪽 `StageRoster.json`에서 N1, B1 행의 배율을 2.0, 2.0으로 바꾸면 11001의 `MaxHp` 20, 접촉 피해 2, 13001 Ranged 피해 12(6 × 2)가 된다. 바꾸지 않은 E1(12001)은 체력 40, 접촉 피해 4 그대로다. 보스 HP 바 비율이 `MaxHp` 기준이다. 확인 후 되돌린다.
- N1 행의 monsterDamageScale을 0.3으로 바꾸면 11001의 접촉 피해가 0이 아니라 1이다. 확인 후 되돌린다.
- `TestSpawnPattern`으로 나온 몬스터는 로스터 배율과 관계없이 원래 수치다.

**배치 수치**
- `TestSpawnPattern` 7, 8, 9가 WorkPlan 04 결과와 같다(거리 10, Line 길이 6, Cluster 반경 1.5).
- 패턴 7의 spawnRadius를 5로 바꾸면 거리 5에 놓인다. 확인 후 되돌린다.

**데이터 오류**
- 클라이언트: StageRoster에 같은 stageId·슬롯을 두 번 넣으면 `StageRoster 데이터 로드 실패` 로그가 남는다. 확인 후 되돌린다.
- 클라이언트: 웨이브 행의 슬롯을 로스터에 없는 이름으로 바꾸면 `WaveManager`가 경고하고 그 행만 건너뛴다. 확인 후 되돌린다.
- 서버: 같은 변경으로 시작 검증이 실패하고 메시지에 웨이브 항목과 슬롯이 나온다. 확인 후 되돌린다.
- 서버: DropTable에서 Elite 행을 모두 지우면 시작 검증이 실패한다. 확인 후 되돌린다.

## 미해결 / 실행 시 확인 필요
- **이미 단계가 오른 행**: 남는 행 중 슬라임 11005·11008·11012·11016, 11010, 엘리트 12009·12012·12015, 보스 13002~13015는 원래 뒤 스테이지용 수치와 공격 구성을 가진다. 배율이 행 수치에 대한 상대값이므로 수치를 고치지 않고 로스터에서 1.0 근처로 쓴다("배율 값을 정하는 규칙").
- **Notion 데이터 정의서**: 열 변경을 유저가 반영한다.
- **기기 저장본**: 버전 2 저장본은 첫째 자리가 달라 쓰이지 않는다(`LoadUsableSavedVersion`). 따로 지우지 않는다.

## 검증 결과
작업 후 기록한다.
