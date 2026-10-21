# WorkPlan 03: 행운열차 (LuckTrain)

## 목표
- 행운상자(`DropItemType.LuckyBox`)를 먹으면 행운열차 창을 띄운다.
  - 흐름: 룰렛 → 보유 스킬 1·3·5개 등급 상승 → 결과 카드 → 전투 복귀
- 스킬 시스템 완성 전에 창과 연출을 더미 데이터로 먼저 만든다. 추첨과 적용은 스킬 API가 도착한 뒤 BattleManager에 붙인다.
- 입력: `Assets/UIReferences/Battle/UIReferencesGuid.md` 4.LuckTrain, `LuckTrain.jpg`, `LuckTrain_Result.jpg`. 별도 기획명세는 없다.

## 범위
**포함**
- LuckTrainWindow: 슬롯 16칸, 시작 버튼, 룰렛 연출, 건너뛰기, 골드 카운터, 결과 카드 패널
- 결과 카드(LuckTrainRewardCard) 스크립트와 프리팹
- devUI 더미 검증 (HUDTestDriver 확장)
- BattleManager: 추첨, 적용, 레벨업 창과의 대기열, 테스트용 ContextMenu
- BattleUI.prefab 배치

**제외**
- 지원품. 스킬 시스템에 없으므로 무기 스킬만 다룬다.
- 슬롯·카드의 아이콘과 등급별 효과 설명. `SkillData`에 해당 필드가 없다.
- 행운열차 골드를 전투 골드에 합산. WP02의 골드 집계 작업 때 한다.
- LuckyBox 드롭 주체(어떤 몬스터가 떨어뜨리는지)
- 가운데 일러스트 에셋. 플레이스홀더 이미지를 쓴다.
- 다른 담당자 파일 수정: `Scripts/Skills/*`, `Scripts/Enemy/*`, `MonsterSpawner.cs`, `Player.prefab`

## 사전 조건
- 작업 브랜치: `feat/BattleSystem`
- WP02 #8(레벨업 선택)이 아직 구현되지 않았다. #6은 WP02 #8의 `skillPool`, 후보 규칙, 선택 창 흐름을 이어 쓴다.
- 외부 의존: 도착하기 전에는 해당 항목을 진행할 수 없다.

| 담당 | 필요한 것 | 쓰는 항목 |
|---|---|---|
| 관엽님 | `SkillBase.MaxLevel`, `SkillController.LevelUpSkill(int)` (WP02와 같은 요청) | #6 |
| Thedume | 플레이어 리시버의 `OnLooted` (WP02 #11과 같은 요청) | #8 |

## 구조 개요

```
[플레이어 리시버] ──OnLooted(LuckyBox)──▶ BattleManager.AddLuckTrain ─▶ pendingLuckTrains++
[ContextMenu TestLuckTrain] ────────────▶        │
                                                 ▼
                                          ShowNextReward ─▶ 레벨업 대기 우선, 없으면 ShowLuckTrain
                                                 │
            ┌────────────────────────────────────┘
            ▼
ShowLuckTrain ─▶ 후보 선정 ─▶ 16칸 채우기 ─▶ 패턴 추첨 ─▶ 골드 결정
              ─▶ SetSlot×16 / SetReward×n ─▶ RequestPause(창) ─▶ LuckTrainWindow.Show

LuckTrainWindow (수동 뷰)
  시작 ─▶ 회전·감속 ─▶ 첫 선택 칸 정지 ─▶ 나머지 점등 ─▶ 결과 패널 ─▶ 계속하기 ─▶ OnFinished
          └──────── 탭하면 건너뛰기 ────────────────────▶┘

OnFinished ─▶ LevelUpSkill×n ─▶ pendingLuckTrains-- ─▶ ShowNextReward 또는 ReleasePause(창)
```

## 작업 항목
| # | 작업 단위 | 산출물(클래스/파일 경로) | 의존 |
|---|---|---|---|
| 1 | 결과 카드 스크립트: 이름, 등급 별 5개, 설명 | `Assets/Scripts/UI/LuckTrainRewardCard.cs` | - |
| 2 | 행운열차 창 스크립트 (UIView): 슬롯, 시작, 룰렛 연출, 건너뛰기, 골드 카운트업, 결과 패널, `OnFinished` | `Assets/Scripts/UI/LuckTrainWindow.cs` | #1 |
| 3 | 프리팹 (Unity CLI): 결과 카드, 행운열차 창 | `Assets/Prefabs/UI/LuckTrainRewardCard.prefab`, `Assets/Prefabs/UI/LuckTrainWindow.prefab` | #2 |
| 4 | devUI 더미 검증: HUDTestDriver 3키로 1·3·5 패턴 순환 | `Assets/Scripts/Dev/HUDTestDriver.cs`, `Assets/Scenes/TestScenes/devUI.unity`(창 인스턴스 추가) | #3 |
| 5 | BattleUI.prefab Popup 캔버스에 창 배치 (비활성) | `Assets/Prefabs/UI/BattleUI.prefab` | #3 |
| 6 | BattleManager: `AddLuckTrain`, `ShowNextReward` 대기열, 추첨, 적용, `EndBattle` 정리, `TestLuckTrain` ContextMenu | `Assets/Scripts/Mangers/BattleManager.cs` | #5, WP02 #8, 관엽님 |
| 7 | BattleScene 검증 (ContextMenu) | 본 문서 하단 "검증 결과" | #6 |
| 8 | LuckyBox 연결: `OnLooted` 핸들러에 `LuckyBox` → `AddLuckTrain` 분기 추가 | `BattleManager.cs` | #7, WP02 #11, Thedume |

## 합의된 구현 결정

### 1. 역할 분리
- **창은 수동 뷰다.** 슬롯 문자열, 선택 칸, 골드, 카드 내용을 받아 연출만 한다. 확률과 규칙은 모른다.
- **BattleManager가 추첨·적용·대기열을 가진다.** SkillSelectWindow와 같은 방식이다.
- 스킬 아이콘·설명이 추가돼도 창은 `SetSlot`/`SetReward` 시그니처만 바꾸면 된다.

### 2. 슬롯 순서 규약
- 창의 슬롯 배열 16칸은 **반시계 순서**이고, index 0은 좌상단 모서리다.

```
 0  15  14  13  12
 1              11
 2   (일러스트)  10
 3               9
 4   5   6   7   8
```

- 인접은 index ±1 (mod 16)이다. BattleManager는 레이아웃을 모르고 index 산술만으로 패턴 규칙을 처리한다.

### 3. 추첨 규칙
**후보와 슬롯**
- 후보: `ActiveSkills` 중 `Level < SkillBase.MaxLevel`인 스킬
- 후보가 0개면 창을 열지 않고 요청 하나를 소모한다(`pendingLuckTrains--`).
- 16칸은 후보에서 균등 무작위로 채운다. 중복을 허용한다.

**패턴**
- 1개 70%, 3개 20%, 5개 10%. 코드 상수로 둔다.
- 스킬별 남은 등급 `remain = MaxLevel − Level`을 넘겨 선택하지 않는다(합의: 추첨에서 제외).
- **5개**: 시작 칸 s 중에서 s..s+4 (mod 16) 구간의 스킬별 등장 수가 모두 `remain` 이하인 것을 무작위로 고른다. 없으면 3개로 내린다.
- **3개·1개**: 칸 순서를 셔플하고 차례로 검사한다. 이미 확정한 칸과 인접하지 않고 해당 스킬의 `remain > 0`이면 확정하고 `remain--`한다. n개를 못 채우면 3개는 1개로 내린다. 후보가 1개 이상이므로 1개는 항상 성공한다.
- 결과 개수는 항상 1·3·5 중 하나다.

**선택 칸 순서**
- 5개는 구간 순서(s, s+1, …), 3개·1개는 index 오름차순이다.
- 결과 카드도 이 순서로 나열한다. 같은 스킬이 여러 번 뽑히면 카드의 등급이 차례로 오른다(쿠나이 2성 → 3성).

### 4. 골드
- `Random.Range(luckTrainGoldMin, luckTrainGoldMax + 1) × 선택 개수`
- 배수는 강등 후 최종 개수(1·3·5) 기준이다.
- 창에 표시만 한다. 전투 골드 합산은 골드 집계 작업 때 한다.

### 5. 룰렛 연출 (합의: 회전 후 감속 정지)
- 창이 떠 있는 동안 timeScale이 0이므로 `Time.unscaledDeltaTime`으로 진행한다.
- 코루틴 대신 Update에서 상태(대기 → 회전 → 점등 → 결과)를 진행한다. 건너뛰기는 상태를 결과로 바꾸기만 하면 된다.
- **회전**
  - 하이라이트는 index 0에서 출발해 반시계(index 증가 방향)로 돈다.
  - 총 스텝 = `spinLaps × 16 + 첫 선택 칸`, 진행률 `t = 경과 / spinDuration`
  - 현재 스텝 = `floor(총 스텝 × (1 − (1 − t)²))`, 하이라이트 칸 = 스텝 % 16
  - 골드 카운터도 같은 비율로 0에서 최종값까지 올린다.
- **점등**: 첫 선택 칸에서 멈춘 뒤 나머지 선택 칸을 `revealInterval`마다 하나씩 켠다. 마지막 점등 후 `revealInterval` 뒤에 결과 패널을 연다.
- **건너뛰기**: 회전·점등 중 화면 아무 곳을 탭하면 모든 선택 칸을 켜고, 골드를 최종값으로 두고, 결과 패널을 바로 연다.
- **초기값**: `spinDuration` 2.5, `spinLaps` 2, `revealInterval` 0.3

### 6. LuckTrainWindow: UIView로 구현
```csharp
public class LuckTrainWindow : UIView
{
    [SerializeField] private TMP_Text[] slotNames;              // 16, 반시계 순서
    [SerializeField] private GameObject[] slotHighlights;       // 16
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject skipHint;
    [SerializeField] private Button skipArea;                   // 전체 화면 투명 버튼
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private LuckTrainRewardCard[] rewardCards; // 5
    [SerializeField] private Button continueButton;
    [SerializeField] private float spinDuration = 2.5f;
    [SerializeField] private int spinLaps = 2;
    [SerializeField] private float revealInterval = 0.3f;

    public event Action OnFinished;

    public void SetSlot(int index, string name);
    public void SetReward(int index, string name, int grade, string description);
    public void Show(int[] selected, int count, int gold);     // selected의 앞 count개 사용
}

public class LuckTrainRewardCard : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject[] filledStars;          // 5
    [SerializeField] private TMP_Text descriptionText;

    public void Set(string name, int grade, string description);
}
```
- **UIView인 이유**: UIPopup 스택에 들어가지 않으므로 Esc로 닫히지 않는다. SkillSelectWindow와 같다.
- **결과는 같은 프리팹의 자식 패널이다.** 행운열차에서만 열리고 함께 닫히므로 별도 UIView로 등록하지 않는다.
- **Show**: 하이라이트를 모두 끄고, 골드 0, 시작 버튼 켬, 건너뛰기 안내·영역 끔, 결과 패널 끔, 카드는 앞의 count개만 켠 뒤 `Open`
- **버튼**: 리스너는 Awake에서 한 번만 등록한다.
  - 시작: 시작 버튼을 끄고 건너뛰기 안내·영역을 켠 뒤 회전을 시작한다.
  - 계속하기: `Close` 후 `OnFinished`
- 결과 패널이 열려도 건너뛰기 안내는 남긴다. 레퍼런스에서 배경의 안내는 뒤에 깔린 행운열차 창의 것이다.
- 카드는 RewardSlot처럼 자기 요소만 갖는 작은 뷰다.

### 7. 프리팹 구조 (Unity CLI)
```
LuckTrainWindow            (전체 화면, LuckTrainWindow)
├ Dim                      (반투명 검정, Raycast Target 켬)
├ Panel
│ ├ Banner                 (RibbonBanner.prefab, "행운 열차")
│ ├ Slots                  (Slot ×16: 프레임, NameText, Highlight)
│ ├ Illustration           (플레이스홀더)
│ └ GoldCounter            (코인 아이콘, GoldText)
├ StartButton              ("계속하기")
├ SkipHint                 ("아무데나 탭하여 건너뛰기", 시작 버튼 자리)
├ SkipArea                 (전체 화면 투명 Button)
└ ResultPanel
  ├ Dim                    (반투명 검정)
  ├ Cards                  (VerticalLayoutGroup, LuckTrainRewardCard.prefab ×5)
  └ ContinueButton         ("계속하기")
```
- Dim은 창이 떠 있는 동안 HUD의 일시정지 버튼과 조이스틱 입력을 막는다.
- Slots의 자식 순서가 2번 규약의 index 순서와 같아야 한다.
- **배치**: BattleUI.prefab의 Popup 캔버스에서 BattleResultWindow보다 앞 형제로 둔다. HUD 위, 결과창 아래에 그려진다.

### 8. 대기열: 레벨업 창과 겹치지 않게
- `pendingLuckTrains` 카운터를 둔다. WP02의 `pendingLevelUps`와 같은 방식이다.
- **`ShowNextReward()`**
  1. `ended`이거나 선택 창·행운열차 창 중 하나라도 열려 있으면 반환한다.
  2. `pendingLevelUps > 0`이면 `ShowSelection`
  3. 아니고 `pendingLuckTrains > 0`이면 `ShowLuckTrain`
- **호출 지점**
  - `AddExp`: WP02의 "선택 창이 닫혀 있으면 ShowSelection"을 `ShowNextReward`로 바꾼다.
  - `AddLuckTrain`: `pendingLuckTrains++` 후 호출한다.
  - 선택 창 종료(pending 0): 창을 닫고 `ShowNextReward`를 호출한 뒤 `ReleasePause(선택 창)`
  - 행운열차 종료: 적용 후 창이 닫힌 상태에서 `ShowNextReward`를 호출한 뒤 `ReleasePause(행운열차 창)`
- 다음 창의 `RequestPause`를 먼저 하고 이전 창을 해제하므로 요청이 비는 순간이 없다.
- 레벨업을 우선한다. 같은 LateUpdate 루프에서 잼과 LuckyBox가 함께 흡수돼도 창이 하나씩 순서대로 뜬다.

### 9. 적용과 전투 종료
- **적용 시점**: 결과 패널의 계속하기(`OnFinished`)
  - 선택 칸 순서대로 `skillController.LevelUpSkill(id)`를 호출한다.
  - `pendingLuckTrains--` 후 8번의 종료 처리를 한다.
- **EndBattle**: 행운열차 창이 열려 있으면 닫고 `ReleasePause(창)`를 호출하고 `pendingLuckTrains = 0`으로 둔다. 적용하지 않는다. WP02 #8의 선택 창 정리와 같은 단계에 둔다.

### 10. 표시 문자열 (더미)
| 위치 | 값 |
|---|---|
| 슬롯 | `SkillName` |
| 카드 이름 | `SkillName` |
| 카드 별 | 선택 후 도달 등급 |
| 카드 설명 | `Lv.{도달 등급}` |
- `SkillName`은 WP02의 `skillPool`에서 `SkillId`로 찾는다.

### 11. BattleManager 추가 필드
| SerializeField | 타입 | 초기값 | 용도 |
|---|---|---|---|
| `luckTrainGoldMin` | int | 100 | 골드 무작위 하한 |
| `luckTrainGoldMax` | int | 300 | 골드 무작위 상한 |

- 창은 Start에서 `UIManager.Instance.Get<LuckTrainWindow>()`로 가져오고 `OnFinished`를 구독한다. 씬 참조 연결은 필요 없다.
- `[ContextMenu("TestLuckTrain")]`는 `AddLuckTrain()`을 호출한다.
- 16칸 스킬, 선택 칸, 남은 등급 버퍼는 필드로 두고 재사용한다.

### 12. devUI 더미 검증 (#4)
- HUDTestDriver의 1·2키(결과창) 옆에 3키를 추가한다.
- 슬롯 16칸은 더미 이름으로 채운다.
- 3키를 누를 때마다 1 → 3 → 5 패턴을 순환하며 고정 선택 칸을 넘긴다.
  - 1개: {2}
  - 3개: {1, 6, 11}
  - 5개: {14, 15, 0, 1, 2} (한 바퀴 경계를 넘는 경우)
- 골드는 200 × 개수, 카드는 더미 이름과 등급 1~5로 채운다.
- devUI에는 BattleManager가 없으므로 시간은 멈추지 않는다. 정지 중 연출은 #7에서 확인한다.

## 의존 순서 / 병렬 가능 그룹
- **외부 의존 없이 바로 시작**: #1 → #2 → #3 → #4, #5 (#4와 #5는 병렬)
- **외부 대기**
  - #6: WP02 #8, 관엽님
  - #8: WP02 #11, Thedume
- **직렬**: #6 → #7 → #8

## 검증 항목
**연출 (#4, devUI)**
- 시작을 누르면 하이라이트가 반시계로 돌다가 감속해 첫 선택 칸에서 멈추고, 나머지 선택 칸이 차례로 켜진 뒤 결과 패널이 뜬다.
- 5개 패턴 {14, 15, 0, 1, 2}가 14부터 순서대로 켜진다.
- 회전 중 화면을 탭하면 바로 결과 패널이 뜨고 골드가 최종값이다.
- 결과 카드 수가 패턴 개수와 같고 별 수가 등급과 같다.
- 계속하기를 누르면 창이 닫힌다. 다시 열면 하이라이트·골드·버튼이 초기 상태다.
- Esc를 눌러도 창이 닫히지 않는다.

**흐름 (#7, BattleScene)**
- `TestLuckTrain`을 실행하면 창이 뜨고 timeScale이 0이며, 연출은 정상 속도로 진행된다.
- 계속하기 후 선택된 스킬의 Level이 뽑힌 수만큼 오르고 timeScale이 1로 돌아온다.
- 스킬 하나만 Lv4이고 나머지가 Lv5이면 선택은 항상 1개이고 그 스킬은 Lv5에서 멈춘다.
- 모든 스킬이 Lv5이면 창이 뜨지 않는다.
- 골드가 100~300 × 개수 범위에 있다.
- `TestAddExp`(2레벨분) 후 `TestLuckTrain`을 실행하면 레벨업 창 2번 뒤 행운열차가 뜨고, 그 사이 timeScale이 1이 되지 않는다.
- 행운열차가 열린 상태에서 `TestAddExp`를 실행하면 레벨업 창은 행운열차가 닫힌 뒤에 뜬다.
- 창이 떠 있는 동안 HUD의 일시정지 버튼과 조이스틱이 막힌다.
- 행운열차가 열린 상태에서 `TestDamage`로 사망하면 창이 닫히고 결과창만 남으며, 스킬 레벨은 오르지 않는다.

## 미해결 / 실행 시 확인 필요
- **LuckyBox 드롭 주체**: 어떤 몬스터가 언제 떨어뜨리는지 승희님과 정한다. 그 전에는 ContextMenu로만 검증한다.
- **아이콘·등급별 설명**: `SkillData`에 필드가 생기면 `SetSlot`/`SetReward`에 Sprite와 설명을 추가하고, 슬롯의 이름 텍스트는 아이콘으로 바꾼다.
- **지원품**: 스킬 시스템에 생기면 후보에 포함한다.
- **골드 합산**: 골드 집계 작업 때 전투 골드에 더한다.
- **스킬 3종 한계**: 후보가 적어 16칸이 같은 스킬로 반복된다. 동작 검증에는 문제가 없다.
- **3개 패턴 강등**: 셔플 1회 탐색이라 가능한 조합이 있어도 1개로 내려갈 수 있다. 후보가 적고 남은 등급이 작을 때만 생긴다. 플레이해 보고 거슬리면 재시도를 넣는다.
- **SkillSelectWindow 배치**: BattleScene 씬 오버라이드로 들어가 있다. 행운열차는 BattleUI.prefab에 넣으므로 두 창의 그리기 순서를 조립 때 확인한다.

## 검증 결과
(실행 후 기록)
