# WorkPlan 08: 통합 디버그 툴 (DevPanel)

## 목표
- 흩어진 디버그 도구 대신, 모든 씬에서 같은 방식으로 여는 디버그 패널 하나를 만든다.
- 패널 틀(열고 닫기, 창, 배율, 빌드 가드)은 하나로 두고, 기능은 씬별 섹션으로 나눈다. 섹션은 조작 대상이 있을 때만 그린다.
- 배틀씬: 배속(1·2·4·8), 스킬 장착·레벨업, 레벨업, 자석, 폭탄, 골드 증가, 킬 수 증가, 플레이어 체력 감소
- 메인씬: 스태미나 회복, 장비 지급 (서버 개발용 API)
- 입력: 유저와의 논의(흩어진 도구 조사 → 틀 공유·기능 씬별 분리 합의 → 기능 목록). 별도 기획명세는 없다.

## 범위
**포함**
- 서버(`COU_Server`): 개발용 API(스태미나 회복, 장비 지급), 설정값 가드
- 클라이언트: `DevPanel`(IMGUI), `DevApi`, `BattleManager` 배속·레벨업, `PlayerInventory` 서버 장비 추가
- 정리: `SkillController` 디버그 키(1~6) 삭제, `InventoryTestDriver` 삭제(MainScene, devUI_Main, Equip에서 컴포넌트 제거)

**제외**
- 테스트 씬 도구(`PlayerDebug`, `DropItemTestDriver`, `HUDTestDriver`, `DummyPlayer`)와 Inspector ContextMenu는 그대로 둔다
- 단축키. 패널은 화면 버튼으로만 조작한다
- uGUI 패널, 프리팹

## 합의된 결정
| 항목 | 결정 |
|---|---|
| 구조 | 틀은 하나, 기능은 씬별 섹션 |
| UI | IMGUI(`OnGUI`). 프리팹·씬 수정 없이 코드만, 클래스 전체를 `#if`로 뺀다 |
| 스킬 디버그 | 등록 스킬 목록을 자동으로 나열하고 스킬마다 장착/레벨업 버튼 |
| 스태미나 회복 | 서버 개발용 API |
| 장비 추가 | 서버 개발용 API로 지급(기존 로컬 `AddItem`은 서버가 모르는 inventoryId라 합성·레벨업 요청에 쓸 수 없음) |
| 서버 가드 | `dev-tools.enabled=${DEV_TOOLS_ENABLED:false}`. 기본 꺼짐, 환경변수로 켬 |
| 기존 도구 정리 | 게임 씬에 붙은 것(SkillController 디버그 키, InventoryTestDriver)만 흡수하고 삭제 |

## 사전 조건
- **다른 담당자 파일 수정**: `SkillController.cs`는 스킬 담당자 파일이다. 디버그 키(`Update`의 `#if` 블록, `UpdateDebugInput`) 삭제를 미리 알린다. 같은 기능은 패널 스킬 섹션으로 옮긴다.
- **서버 실행 환경**: 클라이언트에서 메인 섹션을 확인하려면 서버 `.env`에 `DEV_TOOLS_ENABLED=true`를 넣는다(유저).

## 구조 개요

```
[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]  ── #if UNITY_EDITOR || DEVELOPMENT_BUILD
  └ new GameObject("DevPanel") + DontDestroyOnLoad + DevPanel

DevPanel (partial, OnGUI)
  ├ DevPanel.cs         틀: 토글 버튼, 창, 스크롤, 배율, 결과 메시지 한 줄
  ├ DevPanel.Battle.cs  BattleManager.Instance != null 일 때
  │    ├ 배속   → BattleManager.SetSpeed(1/2/4/8)
  │    ├ 레벨업 → BattleManager.LevelUp()
  │    ├ 골드·킬 → BattleManager.AddGold / AddKill
  │    ├ 자석·폭탄 → PlayerManager.Loot.OnLoot(Magnet / Bomb)
  │    ├ 체력 감소 → PlayerManager.Health.GetDamage
  │    └ 스킬   → PlayerManager.Skills.EquipSkill / LevelUpSkill
  └ DevPanel.Main.cs    PlayerInventory.Instance != null 이고 플레이어 데이터가 로드됐을 때
       ├ 스태미나 회복 → DevApi.RefillStamina → currentData.currency 교체 → NotifyPlayerDataChanged
       └ 장비 지급     → DevApi.GrantItems → PlayerInventory.AddServerItems

서버 /api/dev/**  ── @ConditionalOnProperty(dev-tools.enabled=true), 꺼져 있으면 404 NOT_FOUND
  ├ POST /api/dev/stamina → CurrencyResponse
  └ POST /api/dev/items   → List<EquipmentResponse>
```

## 작업 항목
| # | 작업 단위 | 산출물(클래스/파일 경로) | 의존 |
|---|---|---|---|
| 1 | 설정값 `dev-tools.enabled=${DEV_TOOLS_ENABLED:false}`, 공유 양식에 `DEV_TOOLS_ENABLED` 추가 | `COU_Server/src/main/resources/application.properties`, `COU_Server/.env.example` | - |
| 2 | 에러 코드 `ITEM_NOT_FOUND` | `COU_Server/src/main/java/com/couserver/dev/exception/DevErrorCode.java` | - |
| 3 | 요청 DTO `DevItemGrantRequest(itemId, count)` | `COU_Server/src/main/java/com/couserver/dev/dto/DevItemGrantRequest.java` | - |
| 4 | `DevService`: `refillStamina`, `grantItems` | `COU_Server/src/main/java/com/couserver/dev/service/DevService.java` | #2, #3 |
| 5 | `DevController`: `POST /api/dev/stamina`, `POST /api/dev/items` | `COU_Server/src/main/java/com/couserver/dev/controller/DevController.java` | #1, #4 |
| 6 | `BattleManager`: `Speed`, `SetSpeed`, `ReleasePause` 복원값, `LevelUp` | `Assets/Scripts/Mangers/BattleManager.cs` | - |
| 7 | `PlayerInventory.AddServerItems` | `Assets/Scripts/Inventory/PlayerInventory.cs` | - |
| 8 | `DevApi`: `RefillStamina`, `GrantItems` | `Assets/Scripts/Dev/DevApi.cs` | - |
| 9 | 패널 틀 | `Assets/Scripts/Dev/DevPanel.cs` | - |
| 10 | 배틀 섹션 | `Assets/Scripts/Dev/DevPanel.Battle.cs` | #6, #9 |
| 11 | 메인 섹션 | `Assets/Scripts/Dev/DevPanel.Main.cs` | #7, #8, #9 |
| 12 | `SkillController` 디버그 키 삭제 | `Assets/Scripts/Skills/Controller/SkillController.cs` | #10 |
| 13 | `InventoryTestDriver` 컴포넌트 제거(Unity CLI: MainScene, devUI_Main, Equip) 후 스크립트 삭제 | `Assets/Scripts/Dev/InventoryTestDriver.cs`(+ .meta) 삭제, 씬 3개 | #11 |
| 14 | 검증 | 본 문서 "검증 결과" | #5, #10~#13 |

## 구현 결정

### 1. 서버 개발용 API (#1~#5)
- 패키지 `com.couserver.dev`. `DevController`와 `DevService`에 `@ConditionalOnProperty(name = "dev-tools.enabled", havingValue = "true")`. 꺼져 있으면 빈이 없어 `/api/dev/**`가 404다. 응답 본문은 `GlobalExceptionHandler.handleExceptionInternal`이 만든 `{"status":404,"code":"NOT_FOUND","message":"No static resource ..."}`이다(로컬 서버에서 확인).
- `.env.example`에 `DEV_TOOLS_ENABLED=false`를 추가한다(팀 공유 양식이라 켜는 방법을 알 수 있게).
- 인증은 다른 API와 같다(`SecurityConfig`의 `anyRequest().authenticated()`). 대상은 `@AuthenticationPrincipal AuthAccount`의 playerId다.
- `POST /api/dev/stamina`
  - `@Transactional`. 변경 감지로 저장하므로 빠지면 DB에 반영되지 않는다(`open-in-view=false`).
  - `currencyRepository.findById(playerId)` → `currency.update(gold, gem, staticDataService.getAccountConst().maxStamina(), Instant.now(clock))`(기존 `Currency.update` 사용) → `new CurrencyResponse(currency)`
- `POST /api/dev/items` body `{ "itemId": 1001, "count": 2 }`
  - `count`: `@Min(1) @Max(20)`
  - `staticDataService.getItems()`에 없는 itemId면 `BusinessException(DevErrorCode.ITEM_NOT_FOUND)` (404, "존재하지 않는 장비입니다.")
  - `equipmentRepository.saveAll(Equipment.create(playerId, item) × count)` → `EquipmentResponse` 목록(`ShopService.drawEquipment`와 같은 변환)

### 2. BattleManager (#6)
- `public float Speed { get; private set; } = 1f;` 패널이 현재 값을 표시하려고 읽는다.
- `public void SetSpeed(float value)`: `Speed = value;` 일시정지 요청이 없으면(`pauseRequests.Count == 0`) `Time.timeScale = Speed`.
- `ReleasePause`: 마지막 요청이 풀리면 `Time.timeScale = Speed` (지금은 `1f`). 스킬 선택·일시정지창·행운열차가 끝나도 배속이 유지된다.
- `OnDestroy`의 `Time.timeScale = 1f`는 그대로다. 배속은 전투마다 1로 시작한다.
- `public void LevelUp() => AddExp(RequiredExp - exp);` 한 번에 한 레벨. 실제 레벨업처럼 선택창이 열린다.
- 두 메서드는 `#if`로 감싸지 않는다. 호출하는 쪽(`DevPanel`)만 빌드에서 빠진다.

### 3. PlayerInventory (#7)
- `public List<OwnedItem> AddServerItems(List<InventoryData> saved)`: 각 항목에 기존 `AddServerItem`을 부르고, 끝에 `SyncAndNotify()`. 추가된 장비 목록을 돌려준다.
- `ApplyPurchase`는 그대로 둔다.

### 4. DevApi (#8)
- `ShopApi`와 같은 정적 클래스. 파일 전체를 `#if UNITY_EDITOR || DEVELOPMENT_BUILD`로 감싼다.
- `RefillStamina(Action<ApiResult<CurrencyData>>)` → `ApiClient.Post("/api/dev/stamina", null, ...)`
- `GrantItems(long itemId, int count, Action<ApiResult<List<InventoryData>>>)` → `ApiClient.Post("/api/dev/items", new DevItemGrantRequest { itemId, count }, ...)`
- `DevItemGrantRequest`는 같은 파일에 둔다.
- 응답 타입은 기존 `CurrencyData`, `InventoryData`를 그대로 쓴다(서버 `CurrencyResponse`, `EquipmentResponse`와 필드 이름이 같다. `BattleResultResponse`, `PurchaseResponse`가 이미 이렇게 받는다).

### 5. DevPanel 틀 (#9)
- `public partial class DevPanel : MonoBehaviour`. 파일 3개 모두 `#if UNITY_EDITOR || DEVELOPMENT_BUILD`로 감싼다. 씬·프리팹이 참조하지 않으므로 빌드에서 빠져도 Missing Script가 생기지 않는다.
- 생성: `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]`에서 한 번 만들고 `DontDestroyOnLoad`.
- 토글: 화면 우상단의 작은 `DEV` 버튼. 그릴 섹션이 하나도 없으면(LogInScene, 로그인 없이 실행한 테스트 씬) 버튼도 그리지 않는다.
- 창: `GUILayout` + 스크롤 뷰. 섹션마다 제목 한 줄.
- 배율: `GUI.matrix`로 화면 높이에 맞춰 키운다(모바일 해상도에서 버튼을 누를 수 있게). 기준 높이 960(세로 1920 화면에서 2배).
- 결과 메시지: 창 아래에 마지막 동작의 결과 한 줄(예: "스태미나 30/30", "장비 2개 지급", 실패 메시지).
- 서버 요청 중에는 메인 섹션 버튼을 끈다(`GUI.enabled = false`).
- 입력은 IMGUI 버튼만 받는다. `Keyboard.current`를 읽지 않는다.

### 6. 배틀 섹션 (#10)
`BattleManager.Instance != null`이고 `PlayerManager.Instance != null`일 때 그린다.

| 줄 | 버튼 | 호출 |
|---|---|---|
| 배속 | 1x / 2x / 4x / 8x (현재 값 표시) | `BattleManager.SetSpeed` |
| 성장 | 레벨업 | `BattleManager.LevelUp()` |
| 아이템 | 자석 / 폭탄 | `PlayerManager.Instance.Loot.OnLoot(DropItemType.Magnet / Bomb)` |
| 수치 | 골드 +1000 / 킬 +100 | `BattleManager.AddGold(1000)` / `AddKill()` × 100 |
| 플레이어 | 체력 -10% | `PlayerManager.Instance.Health.GetDamage(최대 체력의 10%, 최소 1)` |
| 스킬 | 스킬마다 `이름 Lv` + 버튼 | 아래 |

- 자석·폭탄은 실제 획득과 같은 경로(`OnLoot`)를 탄다. `OnLooted`로 넘어가지 않으므로 업적 pickups에 세지 않는다.
- 체력 감소는 피격과 같은 경로다. `PlayerHealth`의 무적은 목숨을 잃은 직후(`DecreaseLife`, 0.5초)에만 생기므로 그때만 무시되고, 평소에는 누를 때마다 깎인다.
- 스킬 목록: `GameManager.JsonData.SkillDataDic.Values` 중 `SkillFactory.IsRegistered`인 것(`BattleManager.ShowSelection`과 같은 기준). 보유 스킬은 `Skills.ActiveSkills`·`PassiveSkills`에서 찾는다.
  - 미보유: `장착` → `EquipSkill`. 슬롯이 가득 차면 실패 메시지.
  - 보유: `레벨업` → `LevelUpSkill`. `SkillBase.MaxLevel`이면 버튼을 끈다.
  - 패널 장착·레벨업은 `BattleManager.skillUpgrades`(업적)에 세지 않는다.

### 7. 메인 섹션 (#11)
`PlayerInventory.Instance != null`이고 `GameManager.PlayerData.isPlayerDataLoaded`일 때 그린다. `PlayerInventory`는 devUI_Main, Equip, TestPlayerScene에도 있어, 로그인 없이 실행하면 `currentData`가 null인 채로 섹션이 보이기 때문이다.
- `스태미나 회복`: `DevApi.RefillStamina` 성공 → `GameManager.PlayerData.currentData.currency = 응답` → `NotifyPlayerDataChanged()`(`MainUIAccountBinder`가 TopBar를 갱신). `BattleManager.ApplyBattleChanges`와 같은 방식이다.
- `장비 지급`: itemId(기본 1001), 개수(기본 2) 입력칸 + 버튼. `DevApi.GrantItems` 성공 → `PlayerInventory.Instance.AddServerItems(응답)`. 기본값은 `InventoryTestDriver`에서 가져온다.
- 실패: 에러 코드가 `NOT_FOUND`이면(서버 API가 꺼져 경로가 없음) "서버 개발용 API가 꺼져 있습니다(DEV_TOOLS_ENABLED)", 그 외는 `result.ToUserMessage(...)`. 없는 itemId는 `ITEM_NOT_FOUND`라 구분된다.
- 401은 `ServerLoginManager`가 처리하므로 메시지만 남긴다.

### 8. 정리 (#12, #13)
- `SkillController`: `Update`의 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 블록과 `UpdateDebugInput` 삭제. `using UnityEngine.InputSystem`은 `UpdateDebugInput`에서만 쓰이므로 함께 삭제.
- `InventoryTestDriver`: Unity CLI로 MainScene, devUI_Main, Equip에서 컴포넌트를 먼저 제거하고 저장한 뒤 스크립트와 .meta를 삭제한다(순서가 바뀌면 Missing Script가 남는다). 컴포넌트만 붙은 GameObject라면 GameObject째 제거한다.

## 의존 순서 / 병렬 가능 그룹
- **그룹 A, 서버**: #1, #2, #3 → #4 → #5
- **그룹 B, 클라이언트**: #6, #7, #8, #9 → #10, #11. 끝에 OmniSharp 진단과 Unity 콘솔로 컴파일 확인
- **그룹 C, 정리**: #10 → #12, #11 → #13
- A와 B는 병렬로 진행할 수 있다.
- **직렬**: A, B, C 완료 → #14

## 검증 항목
**컴파일·기동**
- 클라이언트 컴파일 에러가 없다(OmniSharp 진단, Unity 콘솔).
- 서버 `gradlew build`가 통과한다.
- `DEV_TOOLS_ENABLED`가 없으면 `POST /api/dev/stamina`가 404 `NOT_FOUND`, `true`면 200이다.

**서버 API** (test7777)
- `/api/dev/stamina`: 응답과 `GET /api/players/me/save`의 `currencyEnergy`가 `maxStamina`다.
- `/api/dev/items` itemId 1001, count 2: 장비 2개가 응답되고 `GET /api/inventory`에 나온다.
- 없는 itemId: 404 `ITEM_NOT_FOUND`. count 0 또는 21: 400.

**메인씬**
- `DEV` 버튼으로 패널이 열리고 메인 섹션만 보인다.
- 서버를 `DEV_TOOLS_ENABLED` 없이 켜면 "서버 개발용 API가 꺼져 있습니다" 메시지가 나온다.
- 스태미나를 쓴 뒤(전투 입장) 회복하면 TopBar가 최대치로 바뀌고 다시 입장할 수 있다.
- 장비 지급 후 인벤토리 목록에 바로 나오고, 그 장비로 합성 요청이 서버에서 성공한다.
- 재로그인 후에도 지급한 장비가 남아 있다.

**배틀씬**
- 배틀 섹션만 보인다.
- 배속 1·2·4·8이 HUD 시간 흐름에 반영된다. 일시정지창, 스킬 선택, 행운열차가 끝난 뒤에도 배속이 유지된다. 메인씬으로 돌아가면 1배속이다.
- 레벨업: 한 번 누르면 레벨이 1 오르고 선택창이 한 번 열린다.
- 자석: 화면의 경험치 보석이 끌려온다. 폭탄: 일반 몬스터가 처치되고 엘리트·보스는 피해를 받는다.
- 골드 +1000, 킬 +100이 HUD와 일시정지창에 반영된다.
- 체력 -10%가 HP 바에 반영된다. 누를 때마다 깎이고, 목숨을 잃은 직후 0.5초 동안만 무시된다.
- 스킬 목록에 등록 스킬 6개(20010, 20020, 20030, 20040, 21010, 21070)가 나온다. 장착 → 레벨업으로 바뀌고 최대 레벨에서 버튼이 꺼진다.
- 숫자키 1~6을 눌러도 스킬이 장착되지 않는다(#12).

**정리**
- MainScene, devUI_Main, Equip에 Missing Script가 없다(Unity CLI).
- `Keyboard.current` 검색 결과에 `SkillController`, `InventoryTestDriver`가 없다.

## 미해결 / 실행 시 확인 필요
- **패널 터치가 게임 입력에도 전달됨**: IMGUI 버튼은 Input System 입력을 막지 않는다. 배틀 중 패널을 누를 때 이동 조이스틱이 같이 움직이는지 플레이 중 확인한다. 문제가 되면 따로 처리한다.
- **디버그 값이 전투 결과로 서버에 감**: 골드·킬 증가분이 `BattleResultRequest`에 들어간다. 서버는 스테이지 상한까지만 인정하고 경고 로그를 남긴다(`BattleService.clamp`). 킬 수는 계정 경험치와 업적 진행에도 들어가므로 테스트 계정의 업적 진행도가 오른다.
- **개발용 API는 켜져 있는 동안 로그인한 누구나 호출할 수 있음**: 배포 서버에서는 `DEV_TOOLS_ENABLED`를 설정하지 않는다.
- **IMGUI 배율 기준**: 기준 높이 960으로 시작한다. 기기 화면에서 버튼 크기를 보고 조정한다.
- **PlayerDebug**: TestPlayerScene의 7/8/9 키는 없는 스킬 ID(1/2/3)를 장착하려 한다. 이번 범위에서 고치지 않는다.

## 검증 결과
(실행 후 기록)
