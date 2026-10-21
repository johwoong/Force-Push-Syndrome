# WorkPlan 02: 배틀 루프 오케스트레이션 (BattleManager)

## 목표
- 스킬(관엽님), 몬스터(승희님), 드롭, 플레이어, UI를 한 판의 전투 루프로 잇는다.
  - 흐름: 전투 시작 → 타이머·킬 수·EXP 집계 → 레벨업 스킬 선택 → 승리·패배 결과
- `Time.timeScale` 변경을 BattleManager 한 곳으로 모은다. 레벨업 창과 일시정지 창이 겹쳐도 게임이 잘못 재개되지 않게 한다.
- 입력: 유저가 준 배틀 루프 담당 메모, `Docs/Guide/MVPGuide_관엽님_Skill.md`, `Docs/Guide/MVPGuide_승희님_Monster.md`. 별도 기획명세는 없다.

## 범위
**포함**
- BattleManager: 일시정지 요청 관리, 타이머, 킬 수, EXP·레벨, 레벨업 선택 흐름, 승리·패배 처리
- PauseWindow가 `Time.timeScale`을 직접 바꾸는 코드 제거
- 레벨업 선택 창(SkillSelectWindow) 스크립트와 프리팹
- 전투 씬 조립

**제외**
- 보스 등장·처치, AlarmView, HUD 보스 바(`ShowBoss`/`SetBossHp`). MVP 승리 조건은 15분 생존이며, 보스는 후속 작업이다.
- 골드 집계와 모든 `SetGold`
- 결과창의 `SetChapter`/`SetBestTime`/`SetBoxCount`/`SetExp`
- 결과창 이후 흐름(로비 복귀 등)
- ExpGem2~4 수치, `monsterExp`에 따른 잼 등급 분기
- 스킬 설명·아이콘 표시(`SkillData`에 해당 필드가 없다)
- 다른 담당자 파일 수정: `Scripts/Skills/*`, `Scripts/Enemy/*`, `MonsterSpawner.cs`, `Player.prefab`, `UIManager.prefab`
- Build Settings 등록

## 사전 조건
- 현재 브랜치(feat/DropItem)에는 Skills 코드가 없다. dev 기반 브랜치에서 작업한다.
- 외부 의존: 도착하기 전에는 해당 항목을 진행할 수 없다.

| 담당 | 필요한 것 | 쓰는 항목 |
|---|---|---|
| 승희님 | `MonsterSpawner.OnEnemyKilled` (`event Action<Enemy>`) | #4 |
| 승희님 | Enemy 사망 시 `ExpGem1` 드롭 | #11 |
| 관엽님 | `SkillBase.MaxLevel`, `SkillController.LevelUpSkill(int)` (가이드 8번) | #8 |
| 관엽님 | ID를 통일한 SkillData 에셋 (가이드 2번) | #8, #9 |
| 관엽님 | `SkillController.MaxSkillSlots`를 `public const`로 변경 (가이드 8번, 추가 반영됨) | #8 |
| 관엽님 | 실제로 발사되는 스킬 ID 공유. `startingSkill`로 쓴다 (가이드 5번) | #9 |
| 진영님 | `Player.prefab`에 `SkillController` 부착 | #9 |
| Thedume | 플레이어 ILootReceiver의 `OnLooted` | #11 |

## 구조 개요

```
[PlayerHealth]    ──OnDied──────────────▶ BattleManager ──SetTime/SetKillCount/SetLevel/SetExp──▶ InGameHUD
[MonsterSpawner]  ──OnEnemyKilled───────▶      │        ──SetKillCount──────────────────────▶ PauseWindow
[플레이어 리시버] ──OnLooted(ExpGem1)───▶      │        ──SetTime/SetKillCount/Show───────────▶ BattleResultWindow
                                               │ ◀──OnSelected(index)── SkillSelectWindow
                                               ├─ EquipSkill / LevelUpSkill ──▶ SkillController
                                               └─ Heal (대체 보상) ──▶ PlayerHealth

[PauseWindow] ──RequestPause / ReleasePause──▶ BattleManager.pauseRequests (HashSet) ──▶ Time.timeScale
```

**레벨업 흐름**
```
AddExp ─▶ while (exp ≥ 필요량) { level++, pending++ } ─▶ HUD 갱신
          └ pending > 0 이고 선택 창이 닫혀 있으면 ShowSelection
ShowSelection ─▶ 후보 선정 ─▶ RequestPause(창) ─▶ 창 표시 (후보 0개면 회복 1칸)
OnSelected    ─▶ 적용 ─▶ pending-- ─▶ pending > 0 ? ShowSelection : 창 닫기 + ReleasePause(창)
```

## 작업 항목
| # | 작업 단위 | 산출물(클래스/파일 경로) | 의존 |
|---|---|---|---|
| 1 | BattleManager 골격: static `Instance`, 일시정지 요청 관리, `OnDestroy`에서 timeScale 복구 | `Assets/Scripts/Mangers/BattleManager.cs` | - |
| 2 | PauseWindow의 `Time.timeScale` 대입을 BattleManager 요청으로 교체 | `Assets/Scripts/UI/PauseWindow.cs` | #1 |
| 3 | 전투 흐름: 타이머 → `HUD.SetTime`, 15분 생존 승리, `PlayerHealth.OnDied` 패배, `EndBattle`(결과창 표시 + 정지) | `BattleManager.cs` | #1 |
| 4 | 킬 수: `OnEnemyKilled` 구독 → HUD·PauseWindow `SetKillCount` | `BattleManager.cs` | #3, 승희님 |
| 5 | EXP·레벨: `AddExp`, 필요 EXP 곡선, HUD `SetLevel`·`SetExp`, pending 카운터, `TestAddExp` ContextMenu | `BattleManager.cs` | #1 |
| 6 | SkillSelectWindow 스크립트 (UIView) | `Assets/Scripts/UI/SkillSelectWindow.cs` | - |
| 7 | SkillSelectWindow 프리팹 (Unity CLI): 전체 화면 배경 blocker, 버튼 3개, 제목·상세 텍스트 | `Assets/Prefabs/UI/SkillSelectWindow.prefab` | #6 |
| 8 | 레벨업 선택: 후보 선정, 적용(Equip/LevelUp), 회복 대체 보상, 연속 표시, 시작 스킬 장착, `EndBattle` 시 선택 창 정리 | `BattleManager.cs` | #3, #5, #6, 관엽님 |
| 9 | 전투 씬 조립 (Unity CLI) | `Assets/Scenes/GameScenes/BattleScene.unity` | #2, #4, #7, #8, 진영님 |
| 10 | 검증 (ContextMenu로 EXP 주입) | 본 문서 하단 "검증 결과" | #9 |
| 11 | EXP 연결: 플레이어 리시버의 `OnLooted` 구독, ExpGem1 → `AddExp` | `BattleManager.cs`, `BattleScene.unity`(참조 연결) | #10, Thedume, 승희님 |
| 12 | EXP 곡선 보정: 1판 플레이 후 킬 수·레벨을 기록하고 a·b 조정 | BattleScene의 BattleManager 인스펙터 값, 본 문서 "검증 결과" | #11 |

## 합의된 구현 결정

### 1. 일시정지: BattleManager 내부에서 요청자 단위로 관리
```csharp
private readonly HashSet<object> pauseRequests = new();

public void RequestPause(object requester);   // Add 후 timeScale = 0
public void ReleasePause(object requester);   // Remove 후 비었으면 timeScale = 1
```
- **요청자 키**
  - PauseWindow: `this`
  - 레벨업: SkillSelectWindow 인스턴스
  - 전투 종료: BattleResultWindow 인스턴스
- HashSet을 쓰므로 같은 요청자가 두 번 요청하거나 두 번 해제해도 상태가 어긋나지 않는다. 카운터 방식은 짝이 한 번만 어긋나도 영구 정지가 된다.
- `OnDestroy`에서 `Time.timeScale = 1`로 되돌린다. 정지 상태로 씬을 떠나도 다음 씬이 멈춰 있지 않다.
- **PauseWindow 수정**
  - `Open`/`Close`의 `Time.timeScale` 대입을 `BattleManager.Instance.RequestPause(this)` / `ReleasePause(this)`로 바꾼다.
  - `Instance`가 null이면 건너뛴다. BattleManager가 없는 UI 테스트 씬(devUI)에서 예외가 나지 않게 하려는 것이다. 대신 그 씬에서는 시간이 멈추지 않는다.
- **정지 대상**: timeScale을 따르는 것은 모두 멈춘다.
  - 몬스터 이동·스폰(`Time.deltaTime`), 몬스터 공격 간격(`Time.time`)
  - 스킬 쿨다운, 발사체, 물리
  - 드롭 ECS(`SystemAPI.Time.DeltaTime`)
  - uGUI 입력은 timeScale과 관계없이 동작한다.

### 2. BattleManager 형태
- 일반 MonoBehaviour로 만든다. `public static BattleManager Instance { get; private set; }`는 UIManager와 같은 방식이다. Awake에서 대입하고, OnDestroy에서 자기 자신일 때만 null로 되돌린다.
- **MonoSingleton을 쓰지 않는 이유**: MonoSingleton의 `Instance`는 대상이 없으면 새로 생성한다. PauseWindow가 devUI 씬에서 접근하면 참조가 비어 있는 BattleManager가 생겨 예외가 난다.
- **씬 오브젝트 참조**: SerializeField로 받는다.
  - 이벤트 구독은 이 참조를 써서 OnEnable/OnDisable에서 한다(`PlayerMovement`와 같은 방식).
  - 종료 중에 `MonsterSpawner.Instance`를 부르면 스포너가 새로 생성되므로 `Instance`는 쓰지 않는다.
- **UI 참조**: Start에서 `UIManager.Instance.Get<T>()`로 가져온다. 이 시점에는 UIManager.Awake가 끝나 있다. SkillSelectWindow의 `OnSelected`도 Start에서 구독한다.

| SerializeField | 타입 | 초기값 | 용도 |
|---|---|---|---|
| `playerHealth` | PlayerHealth | - | 사망 구독, 회복 보상 |
| `skillController` | SkillController | - | 후보 선정, 스킬 적용 |
| `spawner` | MonsterSpawner | - | 킬 구독 |
| `startingSkill` | SkillData | - | 시작 스킬 |
| `skillPool` | SkillData[] | 3종 | 후보 대상 전체. `startingSkill`도 여기에 넣는다 |
| `victorySeconds` | int | 900 | 승리 시간(초) |
| `expGem1Value` | int | 10 | ExpGem1 하나의 EXP |
| `baseRequiredExp` | int | 20 | 곡선 a |
| `requiredExpIncrement` | int | 6 | 곡선 b |
| `healRewardRatio` | float | 0.3 | 대체 보상의 회복 비율 |
| `testExp` | int | - | `TestAddExp` 주입량 |

- `skillPool`을 SerializeField로 두는 이유
  - `SkillDataBase`에는 전체를 조회하는 API가 없고, `SkillFactory`의 키 목록은 private이다.
  - 다른 담당자의 코드에 API를 추가하지 않아도 된다. 표시에 쓸 `SkillName`도 함께 얻는다.

### 3. 전투 흐름
- **Start**
  - HUD를 초기화한다: `SetTime(0)`, `SetKillCount(0)`, `SetLevel(1)`, `SetExp(0)`
  - `skillController.EquipSkill(startingSkill.SkillId)`를 호출한다. 시작 스킬이 없으면 몬스터를 잡을 수 없어 레벨업이 오지 않는다.
- **Update**
  - `ended`이면 바로 반환한다.
  - `elapsed += Time.deltaTime`을 누적한다.
  - 정수 초가 바뀔 때만 `hud.SetTime`을 호출한다(HUDTestDriver와 같은 방식).
  - `seconds >= victorySeconds`이면 `EndBattle(true)`를 호출한다.
- **킬**: `spawner.OnEnemyKilled`가 오면 `kills++` 후 `hud.SetKillCount`와 `pauseWindow.SetKillCount`를 호출한다.
- **패배**: `playerHealth.OnDied`에서 `EndBattle(false)`를 호출한다. `OnDied`는 목숨이 모두 소진됐을 때만 발생한다.
- **`EndBattle(bool victory)`**
  1. `ended = true`로 둔다. 이후의 AddExp와 킬은 무시한다.
  2. 선택 창이 열려 있으면 닫고 `ReleasePause(창)`를 호출한 뒤 `pending = 0`으로 둔다. 이 단계는 #8에서 추가한다.
  3. `RequestPause(resultWindow)`를 호출한다.
  4. `resultWindow.SetTime(seconds)`, `SetKillCount(kills)`, `Show(victory)` 순으로 호출한다.

### 4. EXP·레벨
- **곡선**: Lv L에서 L+1로 오르는 데 필요한 EXP는 `baseRequiredExp + requiredExpIncrement × (L − 1)`이다. Lv50까지의 누적량은 `49a + 1176b`다.
- **초기값 근거**
  - 현재 스포너는 `spawnInterval = 1초`라서 15분 동안 최대 약 900킬이 나오고, 킬마다 ExpGem1이 1개 떨어진다.
  - ExpGem1 = 10, a = 20, b = 6으로 두면 잼 회수율 80~100%에서 Lv47~53이 된다.
- **보정**
  - 1판을 플레이한 뒤 결과창의 킬 수 K와 HUD 레벨을 기록한다.
  - 목표가 Lv50이면 `b = (K × expGem1Value − 49a) / 1176`로 다시 계산한다.
  - 스포너 간격이 바뀌면 다시 보정한다.
- **HUD**: `SetLevel(level)`, `SetExp(exp / (float)필요량)`. 한 번에 여러 레벨이 오르면 최종 레벨을 바로 표시한다.
- **테스트**: `[ContextMenu("TestAddExp")]`로 `AddExp(testExp)`를 호출한다. OnLooted를 연결하기 전 레벨업 검증에 쓴다(PlayerHealth의 테스트 방식과 같다).
- **호출 시점**
  - `AddExp`는 `DropItemManager.LateUpdate`의 OnLoot 루프 안에서 불릴 수 있다.
  - 첫 레벨업에서 timeScale이 0이 돼도, 같은 루프에 남은 잼은 계속 AddExp된다. 이때는 pending만 늘고, 창이 이미 열려 있으므로 다시 띄우지 않는다.

### 5. 레벨업 선택
**후보 규칙** (`skillPool` 순회)
- 장착한 스킬: `Level < SkillBase.MaxLevel`일 때만 후보다.
- 장착하지 않은 스킬: `ActiveSkills.Count < SkillController.MaxSkillSlots`일 때만 후보다.
- 후보 중 무작위로 최대 3개를 고른다. 부분 Fisher–Yates를 쓰고 리스트는 재사용한다.
- 후보가 0개면 대체 보상 "회복" 1칸을 띄운다. 선택하면 `playerHealth.Heal(Mathf.RoundToInt(MaxHealth × healRewardRatio))`를 호출한다.

**표시 문자열**
| 대상 | 제목 | 상세 |
|---|---|---|
| 장착하지 않은 스킬 | `SkillName` | `NEW` |
| 장착한 스킬 | `SkillName` | `Lv.{Level + 1}` |
| 회복 | `회복` | `HP 30%` (`healRewardRatio`로 생성) |

**적용**: 장착 여부에 따라 `LevelUpSkill(id)` 또는 `EquipSkill(id)`를 호출한다.

**연속 레벨업**
- pending 카운터로 처리한다.
- 선택할 때마다 후보를 다시 뽑아, 방금 올린 레벨을 반영한다.
- 창은 닫지 않고 내용만 바꾼다. 정지 요청도 유지한다.

### 6. SkillSelectWindow: UIView로 구현 (UIPopup 아님)
```csharp
public class SkillSelectWindow : UIView
{
    [SerializeField] private Button[] optionButtons;   // 3개
    [SerializeField] private TMP_Text[] titleTexts;
    [SerializeField] private TMP_Text[] detailTexts;

    public event Action<int> OnSelected;               // 버튼 index

    public void SetOption(int index, string title, string detail);
    public void Show(int count);                       // 앞의 count개 버튼만 켜고 Open
}
```
- **Esc 제외 방식**: UIManager의 Esc 닫기(`CloseTopPopup`)는 UIPopup 스택만 대상으로 한다. UIView로 만들면 UIManager를 고치지 않아도 Esc 대상에서 빠진다. PauseWindow와 BattleResultWindow도 UIView다.
- **역할 분리**
  - 창은 문자열만 받는 수동 뷰다.
  - 후보 리스트와 index → SkillData 대응은 BattleManager가 가진다(InGameHUD의 Set* 방식과 같다).
- 버튼 리스너는 Awake에서 한 번만 등록한다(PauseWindow 방식).
- **프리팹**
  - 화면 전체를 덮는 반투명 배경(Raycast Target 켬)을 둔다. 창이 떠 있는 동안 HUD의 일시정지 버튼과 조이스틱 입력을 막는다.
  - 연출 애니메이션을 넣는다면 unscaled time을 써야 한다. 창이 떠 있는 동안은 timeScale이 0이다.

### 7. 전투 씬
`Assets/Scenes/GameScenes/BattleScene.unity`는 Unity CLI로 조립하며, 본인만 수정한다.

| 오브젝트 | 설정 |
|---|---|
| Player | `Player.prefab` 인스턴스 (SkillController가 부착된 버전) |
| Main Camera | Player 인스턴스의 자식으로 둔다. 스크립트 없이 플레이어를 따라간다 |
| MonsterSpawner | enemyPrefab = `Enemy.prefab`, player = Player |
| DropItemManager | prefabs 13종을 DropItemType 순서로 연결 (DropItemTestScene과 같게) |
| ObjectPoolManager | 빈 오브젝트 + 컴포넌트 |
| UIManager | `UIManager.prefab` 인스턴스. Popup 캔버스의 마지막 자식으로 SkillSelectWindow를 추가하고 비활성으로 둔다. 조이스틱은 InGameHUD 안에 이미 들어 있다 |
| EventSystem | InputSystemUIInputModule. UIManager.prefab에 없으면 추가한다 |
| BattleManager | 결정 2의 SerializeField 연결 |

- SkillSelectWindow는 공유 프리팹인 UIManager.prefab이 아니라 씬 인스턴스에 추가하므로 다른 씬에 영향이 없다.
- UIManager.Register는 Awake에서 캔버스 자식을 비활성 오브젝트까지 수집하므로, 씬 오버라이드로 추가해도 등록된다.

### 8. EXP 입력: Thedume 작업을 기다린다
- 오전에는 대체 리시버를 만들지 않는다. 레벨업 흐름은 `TestAddExp`로 검증한다.
- Thedume의 작업이 머지된 뒤
  - 플레이어 리시버 컴포넌트를 SerializeField로 받아 `OnLooted`를 구독한다.
  - `ExpGem1`이면 `AddExp(expGem1Value)`를 호출하고, 나머지 타입은 무시한다.
- 기대하는 시그니처는 `public event Action<DropItemType> OnLooted;`다. 오전에 Thedume과 확정한다.
- 리시버가 없으면 드롭 ECS에 LootTarget이 생기지 않아 잼이 흡수되지 않는다. 그래서 Thedume의 작업 전에는 실제 플레이로 EXP가 쌓이지 않는다.

## 의존 순서 / 병렬 가능 그룹
- **외부 의존 없이 바로 시작**
  - #1이 끝나면 #2, #3, #5를 동시에 진행할 수 있다.
  - #6 → #7은 #1과 별개로 진행할 수 있다.
- **외부 대기**
  - #4: 승희님
  - #8: 관엽님
  - #9: 진영님(SkillController 부착)
  - #11: Thedume
- **직렬**: #8 → #9 → #10 → #11 → #12

## 검증 항목 (#10)
**시간 제어**
- 레벨업 창이 열린 상태에서 PauseWindow를 열었다 닫아도 timeScale이 0으로 유지된다. HUD 버튼은 배경에 가려지므로 Unity CLI eval로 `Open`/`Close`를 호출한다. 선택을 마치면 1로 돌아온다.
- PauseWindow만 열었다 닫으면 0 → 1로 바뀐다.
- 레벨업 창에서 Esc(UI/Cancel)를 눌러도 창이 닫히지 않는다.
- 정지 중에는 몬스터 이동·스폰, 스킬 발사, 드롭 흡수, 타이머가 모두 멈춘다. 재개하면 정상으로 돌아온다.
- 정지 상태에서 플레이를 종료하고 다시 Play하면 timeScale 1로 시작한다(이 프로젝트는 도메인 리로드가 꺼져 있다).
- devUI 씬에서 PauseWindow를 열고 닫아도 예외가 나지 않는다.

**레벨업**
- 전투를 시작하면 시작 스킬이 Lv1로 장착된다(ActiveSkills 1개).
- `testExp`를 3레벨분으로 두고 주입하면 창이 3번 연속으로 뜨고, 매번 후보가 갱신되며, 마지막 선택 뒤 재개된다. HUD 레벨은 최종값으로 표시된다.
- 슬롯이 3개 미만이면 장착하지 않은 스킬이 후보에 나온다. 슬롯이 3개 모두 차면 나오지 않는다.
- Lv5인 스킬은 후보에서 빠진다.
- 스킬 3개가 모두 장착되고 모두 Lv5이면 회복 1칸만 뜨고, 선택하면 HP가 오른다.
- 새 스킬을 선택하면 ActiveSkills가 1개 늘고 그 스킬은 Lv1이다. 장착한 스킬을 선택하면 Level이 1 오른다.

**전투 흐름**
- HUD 타이머가 초 단위로 오르고, 정지 중에는 멈춘다.
- `victorySeconds`를 인스펙터에서 짧게(예: 20) 바꾸면 그 시간에 승리 결과창이 뜨고 게임이 정지한다.
- PlayerHealth의 `TestDamage` ContextMenu로 목숨을 모두 소진하면 실패 결과창이 뜨고 게임이 정지한다.
- 결과창이 뜬 뒤에는 AddExp와 킬이 무시된다.
- 레벨업 창이 열린 상태에서 사망하거나 승리하면 선택 창이 닫히고 결과창만 남는다.
- (#4 이후) 몬스터를 처치할 때마다 HUD와 PauseWindow의 킬 수가 오른다.

## 미해결 / 실행 시 확인 필요
- **OnLooted**: 시그니처와 컴포넌트 타입을 오전에 Thedume과 확정한다.
- **스킬 3종 한계**
  - 장착 3회와 레벨업 12회가 끝나는 약 Lv16(잼 약 93개, 1.5~2분)부터는 회복 창만 뜬다.
  - 스킬 종류가 늘면 자연히 해소된다. 플레이해 보고 거슬리면 다시 논의한다.
- **UI 캔버스 배치**: PauseWindow와 BattleResultWindow가 어느 캔버스에 있는지 조립 때 확인한다. SkillSelectWindow가 HUD보다 위, 결과창보다 아래에 그려져야 한다.
- **EventSystem**: UIManager.prefab에 들어 있는지 확인한다.
- **조이스틱**: InGameHUD 안의 VirtualJoystick control path(`<Gamepad>/leftStick`)가 Player의 Move 액션에 바인딩돼 있는지 확인한다.
- **카메라 추적**: Player의 자식으로 두는 것은 임시다. 추적 스크립트나 Cinemachine은 후속 작업이다.
- **보스**: 15분 보스 등장과 처치 승리는 후속 작업이다. Monster.json 보스 행, 보스 프리팹, 스포너 API가 필요하므로 승희님과 협의한다.
- **결과창 이후 흐름**: 정해지지 않았다(결과창에 버튼이 없다).
- **ExpGem1 고정**: 몬스터별 `monsterExp`(3~5)는 쓰지 않는다. 잼 등급 분기를 도입할 때 곡선을 다시 설계한다.
- **devUI 씬 PauseWindow**: BattleManager가 없어 시간을 멈추지 않게 된다. UI 담당자에게 공유한다.
- **UIManager.prefab**: SkillSelectWindow를 씬 오버라이드로 추가한다는 점을 소유자에게 공유한다.

## 검증 결과
(실행 후 기록)
