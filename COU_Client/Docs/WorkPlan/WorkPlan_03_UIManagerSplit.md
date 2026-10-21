# WorkPlan 03: UIManager 영속/씬 분리

## 목표
- UIManager를 씬 전환에도 살아 있는 영속 UIManager 하나로 바꾼다. 씬 UI는 등록 전용 컴포넌트(SceneUIRoot)를 통해 여기에 등록한다.
  - 호출 진입점은 `UIManager.Instance.Get<T>()` 하나로 유지한다. 기존 호출부는 수정하지 않는다.
  - 팝업 스택과 Cancel 입력은 영속 UIManager 한 곳에서 처리한다.
- 모든 씬에서 쓰는 시스템 UI의 첫 항목으로 알림/확인 팝업(MessagePopup)을 만든다.
- 계기: 로그인 창(`LogInMainCanvas`)에서 결과 알림과 확인 팝업이 필요하다. 이 기능은 로비와 전투에서도 똑같이 필요하다.
- 입력: 유저와의 설계 논의. 별도 기획명세는 없다.

## 범위
**포함**
- UIManager 영속화: MonoSingleton, Resources 자동 생성, Overlay 캔버스 소유
- SceneUIRoot: HUD/Screen/Popup 캔버스 등록과 해제
- `GameManager.UI` 접근자
- MessagePopup: 알림(확인) / 확인·취소 두 모드
- 씬 프리팹 이름 변경과 컴포넌트 교체: `UIManager.prefab` → `BattleUI.prefab`, `MainUIManager.prefab` → `MainUI.prefab`
- UIManager 컴포넌트를 직접 쓰는 씬(devUI) 이관
- jyj8943님용 연동 가이드 문서

**제외**
- 토스트, 로딩·페이드, 입력 차단 (후속 작업)
- 다른 담당자 파일 수정: `LogInMainCanvas.cs`, `GameSceneManager.cs`, `LocalLoginManager.cs`, `LogInScene.unity`. 연동 방법은 가이드 문서로 전달한다.
- 씬 전환 API
- UICamera와 카메라 스택 변경

## 현재 상태 (조사 결과)
| 항목 | 내용 |
|---|---|
| UIManager 컴포넌트 위치 | `UIManager.prefab`(전투 UI), `MainUIManager.prefab`(로비 UI). devUI 씬에는 컴포넌트가 직접 붙어 있다 |
| 프리팹 사용 씬 | `UIManager.prefab`: BattleScene, enemy / `MainUIManager.prefab`: devUI_Main |
| LogInScene | UIManager가 없다. `LogInMainCanvas`는 UIView가 아닌 일반 MonoBehaviour다 |
| 캔버스 | 4개 모두 Screen Space - Camera이고 프리팹 안의 UICamera를 쓴다. sortingOrder는 HUD 0, Screen 10, Popup 20, Overlay 30 |
| OverlayCanvas | 두 프리팹 모두 비어 있다 |
| UICamera | URP Overlay 카메라. 씬 Main Camera의 스택에 연결돼 있다 |
| CanvasScaler | Scale With Screen Size, 1080×1920, Match Width 0 |
| 호출 시점 | `Get<T>()` 호출은 모두 Start 이후이거나 버튼 클릭 시점이다. Awake에서 부르는 곳은 없다 |

## 구조 개요

```
[BeforeSceneLoad] Resources/UI/UIManager.prefab Instantiate
                  └ UIManager (MonoSingleton, DontDestroyOnLoad)
                     ├ OverlayCanvas (Screen Space - Overlay) ─ SafeArea ─ MessagePopup
                     ├ views: Dictionary<Type, UIView>   ← 영속 + 현재 씬 뷰
                     ├ popups: List<UIPopup>             ← 단일 스택
                     └ Update: UI/Cancel → CloseTopPopup

[씬 로드]   BattleUI / MainUI 프리팹
             └ SceneUIRoot ── Awake: Register(HUD/Screen/Popup 캔버스) ──▶ UIManager
                            └ OnDestroy: Unregister(보관한 뷰 목록) ──────▶ UIManager
             └ UICamera (Main Camera 스택, 기존 그대로)

[호출부]    UIManager.Instance.Get<T>()  /  GameManager.UI.Get<T>()
```

## 작업 항목
| # | 작업 단위 | 산출물(클래스/파일 경로) | 의존 |
|---|---|---|---|
| 1 | UIManager 영속화: `MonoSingleton<UIManager>` 상속, 자체 `Instance` 제거, `overlayCanvas`만 보유, 자동 생성, 등록 API를 덮어쓰기/조건부 해제로 변경 | `Assets/Scripts/UIManager/UIManager.cs` | - |
| 2 | SceneUIRoot: 캔버스 3개 등록, 뷰 목록 보관, OnDestroy에서 해제 | `Assets/Scripts/UIManager/SceneUIRoot.cs` | #1 |
| 3 | `GameManager.UI` 접근자 추가 | `Assets/Scripts/Mangers/MonoSingleton.cs` (GameManager 클래스에 1줄) | #1 |
| 4 | MessagePopup 스크립트 (UIPopup) | `Assets/Scripts/UI/MessagePopup.cs` | - |
| 5 | MessagePopup 프리팹 (Unity CLI): 전체 화면 dim blocker, 메시지 텍스트, 확인·취소 버튼 | `Assets/Prefabs/UI/MessagePopup.prefab` | #4 |
| 6 | 영속 UIManager 프리팹 (Unity CLI) | `Assets/Resources/UI/UIManager.prefab` | #1, #5 |
| 7 | 씬 프리팹 이관 (Unity CLI): 이름 변경, UIManager 컴포넌트 → SceneUIRoot, OverlayCanvas 삭제 | `Assets/Prefabs/UI/BattleUI.prefab`, `Assets/Prefabs/UI/MainUI.prefab` | #2 |
| 8 | 씬 이관 (Unity CLI): devUI의 UIManager 컴포넌트 → SceneUIRoot. BattleScene, enemy, devUI_Main의 OverlayCanvas 오버라이드 확인 | `Assets/Scenes/TestScenes/devUI.unity` 외 확인 대상 3개 | #2, #7 |
| 9 | 연동 가이드 문서 | `Docs/Guide/MessagePopup_LoginIntegration.md` | #3, #6 |
| 10 | 검증 | 본 문서 하단 "검증 결과" | #6, #7, #8 |

## 합의된 구현 결정

### 1. 구조: 영속 UIManager 하나 + 씬 등록 컴포넌트
- 매니저를 두 개(Global/Scene) 두지 않는다. 호출부가 T의 위치를 알 필요가 없고, 팝업 스택과 Esc 처리가 나뉘지 않는다.
- SceneUIRoot는 매니저가 아니다. 캔버스 참조를 들고 등록과 해제만 한다.
- UILayer는 그대로 쓴다. HUD/Screen/Popup은 씬 쪽, Overlay는 영속 쪽이다.

### 2. UIManager
```csharp
public class UIManager : MonoSingleton<UIManager>
{
    [SerializeField] private Canvas overlayCanvas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap();                              // Resources.Load("UI/UIManager") 후 Instantiate

    protected override void Awake();                              // base.Awake() → Register(overlayCanvas, Overlay) → cancelAction

    internal void Register(Canvas canvas, UILayer layer);         // private → internal, views[type] = view 로 덮어쓰기
    internal void Unregister(IEnumerable<UIView> targets);        // views 값이 같은 인스턴스일 때만 제거 + popups에서 제거

    // Get / Open / Close / CloseAll / CloseTopPopup / PushPopup / RemovePopup 은 그대로
}
```
- **생성**: `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)`에서 Resources 프리팹을 Instantiate한다.
  - 어느 씬에서 Play해도 첫 씬의 Awake보다 먼저 존재한다.
  - 씬 파일에 UIManager를 둘 필요가 없다.
  - 프리팹의 `isDontDestroy`는 true로 둔다. DDOL 처리는 MonoSingleton에 맡긴다.
- **Instance**: MonoSingleton의 `Instance`를 쓴다. 기존 호출부의 `UIManager.Instance`는 그대로 컴파일된다.
- **덮어쓰기 등록**: 씬 로드 방식(특히 `LoadSceneAsync`)에 따라 새 씬의 Awake와 이전 씬의 OnDestroy 순서가 보장되지 않는다. 같은 씬을 다시 로드하면 같은 타입이 두 번 등록되므로, `Add`로 등록하면 예외가 난다.
  - 등록은 `views[type] = view`로 덮어쓴다.
  - 해제는 사전의 값이 해제 대상과 같은 인스턴스일 때만 한다. 먼저 등록된 새 뷰를 지우지 않기 위해서다.
  - 대신 같은 씬 안의 타입 중복은 더 이상 예외로 드러나지 않는다.
- **Unregister**: `popups`에서도 대상을 제거한다. 지금은 `Close()`에서만 제거되므로, 열린 채로 씬이 내려가면 파괴된 팝업이 스택에 남는다.

### 3. SceneUIRoot
```csharp
public class SceneUIRoot : MonoBehaviour
{
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private Canvas screenCanvas;
    [SerializeField] private Canvas popupCanvas;

    private UIManager manager;       // Awake에서 캐시
    private UIView[] ownedViews;     // 세 캔버스에서 수집한 뷰
}
```
- **Awake**: `manager = UIManager.Instance`로 캐시한 뒤 캔버스 3개를 Register한다. 세 캔버스의 `GetComponentsInChildren<UIView>(true)` 결과를 `ownedViews`에 보관한다.
- **OnDestroy**: `if (manager != null) manager.Unregister(ownedViews);`
  - `UIManager.Instance`를 부르지 않는다. Play 종료 중에 영속 UIManager가 먼저 파괴됐으면 MonoSingleton이 빈 인스턴스를 새로 만든다(WorkPlan 02의 MonsterSpawner와 같은 문제).
  - 해제 시점에 `GetComponentsInChildren`을 다시 부르지 않고 Awake에서 보관한 목록을 쓴다.

### 4. 영속 Overlay 캔버스: Screen Space - Overlay
- 카메라 없이 모든 Screen Space - Camera 캔버스 위에 그려진다. 씬 프리팹의 UICamera와 카메라 스택은 건드리지 않는다.
- 설정
  - sortingOrder 100
  - GraphicRaycaster
  - CanvasScaler는 기존과 같다 (Scale With Screen Size, 1080×1920, Match Width 0)
  - 자식에 SafeArea를 둔다

### 5. MessagePopup
```csharp
public class MessagePopup : UIPopup
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    public void ShowAlert(string message, Action onConfirm = null);                    // 취소 버튼 숨김
    public void ShowConfirm(string message, Action onConfirm, Action onCancel = null); // 두 버튼 표시
}
```
- **호출 방식**: 기존 관례(`Get<EquipDetailPopUp>().Show(item)`)를 따라 `GameManager.UI.Get<MessagePopup>().ShowAlert(...)`로 부른다. UIManager에 편의 메서드를 추가하지 않는다.
- **Esc 동작**: 알림 팝업은 [확인]과 같고, 확인·취소 팝업은 [취소]와 같다.
- **콜백은 닫힐 때 정확히 한 번 호출**
  - 닫힐 때 부를 콜백을 하나 보관한다. 기본값은 알림이면 `onConfirm`, 확인·취소면 `onCancel`이다.
  - 확인·취소 모드에서 [확인]을 누르면 보관 콜백을 `onConfirm`으로 바꾼 뒤 `Close()`를 호출한다.
  - `Close()` 순서: `base.Close()` → 보관 콜백을 꺼내 비우기 → 호출. 콜백 안에서 새 메시지를 띄워도(예: 회원가입 완료 → 확인 → 로그인 결과 알림) 덮어쓰이지 않는다.
  - 버튼, Esc(`CloseTopPopup`), `CloseAll` 모두 `Close()`를 거치므로 콜백이 빠지지 않는다.
- 버튼 리스너는 Awake에서 한 번만 등록한다.
- **프리팹**: 화면 전체를 덮는 반투명 배경(Raycast Target 켬)을 둔다. 팝업이 떠 있는 동안 아래 씬 UI 입력을 막는다.

### 6. 이름
| 대상 | 이름 |
|---|---|
| 영속 매니저 | `UIManager` (클래스 유지), `Assets/Resources/UI/UIManager.prefab` |
| 씬 등록 컴포넌트 | `SceneUIRoot` |
| 전투 씬 UI 프리팹 | `UIManager.prefab` → `BattleUI.prefab` (루트 오브젝트 이름도 변경) |
| 로비 씬 UI 프리팹 | `MainUIManager.prefab` → `MainUI.prefab` (루트 오브젝트 이름도 변경) |
| 시스템 팝업 | `MessagePopup` |

- 이름 변경은 Unity CLI의 `AssetDatabase.MoveAsset`으로 한다. GUID가 유지되므로 씬의 프리팹 참조는 깨지지 않는다.

### 7. 로그인 연동: API + 가이드 문서
- jyj8943님 파일은 수정하지 않는다. `Docs/Guide/MessagePopup_LoginIntegration.md`에 다음을 적는다.
  - `LogInMainCanvas`의 `Debug.Log(message)`를 `ShowAlert(message)`로 바꾸는 예시
  - 회원가입 성공 → `ShowAlert("회원가입이 완료되었습니다.", 자동 로그인)` 흐름 예시
  - 영속 UIManager는 자동 생성되므로 LogInScene에 추가로 둘 것이 없다. EventSystem만 있으면 된다.
  - 로그인 UI를 UIView로 바꾸게 되면 SceneUIRoot를 붙이는 방법

## 의존 순서 / 병렬 가능 그룹
- **직렬**: #1 → #2 → #7 → #8
- **병렬**
  - #4 → #5는 #1과 별개로 진행할 수 있다.
  - #3은 #1 직후 언제든 진행할 수 있다.
- **합류**: #6은 #1과 #5가 끝난 뒤, #9는 #3과 #6이 끝난 뒤, #10은 #6, #7, #8이 모두 끝난 뒤 진행한다.
- **컴파일 주의**: #1이 끝나면 씬 프리팹의 UIManager 컴포넌트에서 `hudCanvas`/`screenCanvas`/`popupCanvas` 직렬화 값이 사라진다. #7을 끝내기 전에는 전투·로비 씬이 동작하지 않는다.

## 검증 항목 (#10)
**생성과 수명**
- BattleScene, devUI_Main, LogInScene, DropItemTestScene에서 각각 Play하면 DontDestroyOnLoad에 UIManager가 하나만 있고 예외가 없다.
- Play를 종료할 때 "Some objects were not cleaned up" 경고가 없다. 다시 Play해도 UIManager가 하나다(도메인 리로드가 꺼져 있다).

**기존 흐름 회귀**
- BattleScene: HUD 갱신, PauseWindow 열기·닫기, HomePopup을 Esc로 닫기, SkillSelectWindow 표시
- devUI_Main: 탭 전환, StageSelectScreen, ChallengePopup, EquipDetailPopup, Esc 닫기
- devUI: HUDTestDriver 동작

**MessagePopup** (Unity CLI eval로 호출)
- `ShowAlert`: [확인]과 Esc 모두 `onConfirm`이 한 번 호출된다.
- `ShowConfirm`: [확인]은 `onConfirm`만, [취소]와 Esc는 `onCancel`만 한 번 호출된다.
- `onConfirm` 안에서 다시 `ShowAlert`를 부르면 새 메시지가 열린 채로 남는다.
- 씬 팝업(HomePopup) 위에 MessagePopup을 띄우면 화면 최상단에 그려지고 아래 UI는 클릭되지 않는다. Esc를 누르면 MessagePopup, HomePopup 순으로 닫힌다.
- Device Simulator에서 SafeArea가 적용된다.

**씬 전환**
- 씬 팝업을 연 채로 다른 씬을 로드하면 이전 씬의 뷰가 `views`와 `popups`에서 빠진다. Esc를 눌러도 예외가 없다.
- BattleScene을 `LoadScene`과 `LoadSceneAsync`로 각각 다시 로드해도 HUD가 동작한다(같은 타입 재등록).
- 전환 중에도 MessagePopup은 유지된다.

## 미해결 / 실행 시 확인 필요
- **EventSystem**: 영속 프리팹에 넣지 않는다. EventSystem이 없는 씬에서는 MessagePopup을 클릭할 수 없다. LogInScene과 BattleScene에는 있다. 나머지 씬은 #10에서 확인한다.
- **OverlayCanvas 씬 오버라이드**: 프리팹의 OverlayCanvas는 비어 있다. BattleScene, enemy, devUI_Main에서 씬 오버라이드로 자식을 추가했는지 #8에서 확인하고, 있으면 옮길 위치를 다시 논의한다.
- **MessagePopup 외형**: `PopupFrame.prefab`을 재사용할 수 있는지 #5에서 확인한다. TMP 폰트는 기존 팝업과 같은 한글 폰트 에셋을 쓴다.
- **중복 호출**: 열려 있는 동안 다시 호출하면 내용과 콜백을 덮어쓴다. 이전 콜백은 호출되지 않는다. 대기열이 필요해지면 다시 논의한다.
- **CloseAll**: 이제 MessagePopup도 닫고, 기본 콜백을 호출한다. 현재 호출하는 곳은 없다.
- **이름 변경 공유**: WorkPlan 02와 팀원들이 `UIManager.prefab`이라는 이름을 쓴다. `BattleUI.prefab`으로 바뀐 것을 공유한다.
- **후속 작업**: 토스트, 로딩·페이드, 입력 차단, 씬 전환 API(`GameSceneManager`, jyj8943님)

## 검증 결과
Unity CLI(eval, editor_play, capture_game_view)로 확인했다. 콘솔 에러·경고는 모든 항목에서 0건이다.

**생성과 수명**
| 항목 | 결과 |
|---|---|
| BattleScene, devUI_Main, LogInScene, DropItemTestScene, devUI에서 Play | 통과. DontDestroyOnLoad에 `UIManager(Clone)` 하나, `UIManager.Instance == GameManager.UI` |
| Play 종료 시 "not cleaned up" 경고 / 재Play | 통과. 경고 없음, 에디트 모드에 UIManager가 남지 않음, 재Play 시 하나 |

**기존 흐름 회귀**
| 항목 | 결과 |
|---|---|
| BattleScene: HUD 갱신, PauseWindow 열기·닫기, HomePopup Esc, SkillSelectWindow 표시 | 통과. 일시정지 시 timeScale 0 → 닫으면 1 |
| devUI_Main: 탭 전환, StageSelectScreen, ChallengePopup, EquipDetailPopup, Esc 닫기 | 통과 |
| devUI: HUDTestDriver | 통과. 타이머 갱신, 보스 경보 → 보스 등장 |

**MessagePopup**
| 항목 | 결과 |
|---|---|
| ShowAlert: [확인]과 Esc | 통과. `onConfirm` 1회. 알림 모드에서 [취소] 숨김 |
| ShowConfirm: [확인] / [취소] / Esc | 통과. `onConfirm`만 / `onCancel`만 / `onCancel`만 1회 |
| `onConfirm` 안에서 ShowAlert | 통과. 새 메시지가 열린 채 남고, 새 콜백도 1회 |
| 중복 Close, 열린 채 재호출, CloseAll | 통과. 중복 Close 시 콜백 추가 호출 없음, 재호출 시 이전 콜백 미호출, CloseAll 시 기본 콜백 1회 |
| HomePopup 위에 MessagePopup | 통과. 레이캐스트 최상단이 Overlay(order 100)이고 HUD·HomePopup 버튼 위치가 Dim에 막힘. 포인터 클릭으로 [확인] 동작. 실제 Esc 키 입력(Input System 이벤트)으로 MessagePopup → HomePopup 순서로 닫힘 |
| Device Simulator SafeArea | 미확인. CLI로 Simulator 뷰를 전환할 수 없어 수동 확인이 필요하다 |

**씬 전환**
| 항목 | 결과 |
|---|---|
| HomePopup·MessagePopup을 연 채 BattleScene → devUI_Main | 통과. BattleScene 뷰는 `Get`에서 KeyNotFound, Esc 두 번 모두 예외 없음 |
| BattleScene `LoadScene` / `LoadSceneAsync` 재로드 | 통과. HUD가 새 씬 인스턴스로 교체되고 타이머 갱신 |
| 전환 중 MessagePopup 유지 | 통과. 비동기 재로드 전후로 열린 상태 유지 |

**미해결 항목 확인 결과**
- **OverlayCanvas 씬 오버라이드**: BattleScene, enemy, devUI_Main 모두 없다.
- **EventSystem**: DropItemTestScene, ExampleGameScene, ManagersTestScene(_2), SampleScene, SkillTestScene에는 없다. 이 씬들에서는 MessagePopup을 클릭할 수 없다.
- **PopupFrame 재사용**: 하지 않았다. PopupFrame은 제목·X 버튼이 있는 886×1100 프레임이라 맞지 않는다. HomePopup과 같은 색·Square 스프라이트·NanumGothicBold SDF로 구성했다(Panel 786×440, 버튼 299×122). 버튼은 HorizontalLayoutGroup에 넣어 알림 모드에서 [확인]이 가운데로 온다.

**계획과 다르게 처리한 부분**
- BattleScene, devUI_Main: 프리팹 인스턴스의 이름 오버라이드(`UIManager`, `MainUIManager`)를 되돌렸다. 되돌리지 않으면 루트 이름 변경이 씬에 반영되지 않는다.
- devUI: 컴포넌트 교체와 함께 빈 OverlayCanvas도 삭제했다(#7과 같은 구조). 오브젝트 이름 `UIManager`는 그대로 두었다.
- enemy.unity: 다른 담당자 씬이라 저장하지 않았다. 파일에는 없어진 UIManager 컴포넌트를 대상으로 한 오버라이드(popup/screen/overlay 캔버스 null)와 이름 오버라이드 `UIManager`가 남아 있다. 이 오버라이드는 더 이상 적용되지 않는다. 이제 SceneUIRoot가 캔버스 3개를 모두 등록한다(InGameHUD 컴포넌트 제거 오버라이드는 유지).
- MessagePopup은 계획대로 SafeArea 아래에 두었다. 노치 기기에서는 Dim이 SafeArea 밖(노치 영역)을 덮지 않는다.
