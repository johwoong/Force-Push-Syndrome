# GUIPack-Clean&Minimalist 에셋 가이드

ricimi의 **Clean & Minimalist GUI Pack**에서 프리팹과 스프라이트를 골라 이 프로젝트의 UI 구조에 맞게 쓰기 위한 참조 문서다.
아래에서 `<Pack>`은 `Assets/Externals/GUIPack-Clean&Minimalist`를 뜻한다.

## 0. 핵심 규칙
- 팩은 git에 올라가지 않는다(`Assets/Externals/*` 제외). 각자 로컬에 임포트하고, 팩 파일을 Externals 밖으로 복사해 커밋하지 않는다(재배포 금지 라이선스).
- 커밋한 프리팹이 팩 에셋을 참조하면 팩을 임포트하지 않은 팀원에게는 Missing으로 보인다.
- 팩 폰트(Roboto, Roboto Condensed, Vegur)에는 한글이 없다. 텍스트는 `Assets/Fonts/NanumGothicBold SDF.asset`으로 바꾼다.
- `<Pack>/Demo/Prefabs/UI Elements/Canvas/Canvas.prefab`은 쓰지 않는다. 안에 든 EventSystem이 레거시 `StandaloneInputModule`을 쓰는데, 이 프로젝트는 New Input System만 켜져 있다.
- 팝업은 팩의 `Ricimi.Popup`/`PopupOpener`가 아니라 프로젝트의 `UIPopup`으로 연다(8장).
- 팩은 1920x1280 가로(Match Height)를 기준으로, 프로젝트는 1080x1920 세로(Match Width)를 기준으로 만들어졌다. 크기는 프로젝트 UI에 맞춰 다시 잡는다.
- 프로그레스 바는 Animator로 돌아가는 연출용이다. 값을 표시하려면 Animator를 떼어 낸다.
- 아이콘은 전부 흰색 단색이다. 색은 `Image.color`로 입힌다.

## 1. 현재 상태
- 2026-10 기준으로 프로젝트의 프리팹과 씬 중 팩 에셋을 참조하는 것은 없다.
- 팩 스크립트에는 asmdef가 없어서 `Assembly-CSharp`로 컴파일된다. 네임스페이스는 `Ricimi`다.
- 데모는 URP를 쓰고 Unity 6.5 이상을 요구한다. 스프라이트 자체는 Unity 버전을 타지 않는다.
- 원본 문서: `<Pack>/Documentation/Documentation.pdf`. PDF에 나오는 `Demo/Gradients`, `Demo/Shader` 폴더는 이 임포트에 없다.

## 2. 폴더 구조
| 경로 | 내용 | 사용 |
|---|---|---|
| `Demo/Prefabs/UI Elements/` | UI 요소 프리팹 (3장) | O |
| `Demo/Resources/Popups/{Dark,Light}/` | 완성형 팝업 25종 × 2테마 (4장) | 참고·분해해서 사용 |
| `Demo/Sprites/` | Shapes, Icons, Backgrounds, Avatars, Logos (5장) | O |
| `Demo/Fonts/` | Roboto, Roboto Condensed, Vegur (TTF/OTF + TMP SDF) | 영문·숫자 전용 |
| `Demo/Sounds/` | 버튼·닫기·에러 효과음 | O |
| `Demo/Animations/` | 팝업, 버튼, 페이드, 스피너, 프로그레스 바, 레이더, 나침반 | O |
| `Demo/Editor/` | 컬러 팔레트(`Main Color Palette.colors`, `Complete Color Palette.colors`) | 참고 |
| `Demo/Scripts/` | `Ricimi` 스크립트 11개 (7장) | 일부 |
| `Demo/Scenes/` | `Demo`, `Themes/{Dark,Light}/{Home,Game}`, `UI Elements/{Buttons,Icon Buttons,UI Elements}` | 참고 |
| `Demo/Prefabs/Navigation/` | 데모 씬 상단 바("Back to main menu", SceneTransition 사용) | X |
| `Sources/Icons/` | 아이콘 원본: 512px PNG, PSD, SVG | 고해상도가 필요할 때 |
| `Sources/Game Controller/` | PC, PS4, Steam, XBoxOne, HTC Vive, Oculus 버튼 아이콘 (PNG/PSD/SVG) | 필요할 때 |
| `Sources/Mockups/Themes/{Dark,Light}/PNG/` | 완성 화면 목업 27장 | 미리보기 (9장) |

## 3. UI 요소 프리팹 (`Demo/Prefabs/UI Elements/`)

### 3.1 구조와 이름 규칙
- **베이스 프리팹 + 색상 Variant** 구조다. 폴더마다 색 접미사가 없는 베이스(회색 `#ADB5BD`)가 하나 있고, 색 Variant는 베이스의 `Image.m_Color`(일부는 텍스트 색까지)만 덮어쓴다.
- 이름은 `<요소> <모양> - <Filled|Outline> - [Gradient -] <색>` 형식이다. 다만 Gradient의 단어 순서는 모양마다 다르다.
  - Rounded: `Button Rounded - Gradient - Filled - Blue`
  - Semi Rounded, Sharp Rounded, Square: `Button Semi Rounded - Filled - Gradient - Blue`
- 폴더 이름 오타가 원본 그대로 남아 있다: `Textfield (Size Fitter)/Runded`, `Textfield (Size Fitter)/Sharp Runded`.

### 3.2 모양(Shape)
모양은 스프라이트와 `Image.pixelsPerUnitMultiplier`(아래 표의 ppuMul)의 조합으로 정해진다. ppuMul이 클수록 모서리 반경이 작아진다.

| 모양 | Filled 스프라이트 | Outline 스프라이트 | ppuMul (버튼 기준) |
|---|---|---|---|
| Rounded (알약형) | `Shapes/Rounded/Rounded` | `Rounded - Stroke 6px` | 3 |
| Semi Rounded | `Shapes/Semi Rounded/Semi Rounded` | `Semi Rounded - Stroke Npx` | 1 |
| Sharp Rounded | `Shapes/Semi Rounded/Semi Rounded` | `Semi Rounded - Stroke 4px` | 2 |
| Square | `Shapes/Square/Square` | `Square - Stroke 2px` | 2 |
| Circle | `Shapes/Circle/Circle - 256` | `Circle - Stroke Npx` | 3 |

### 3.3 색상
Sharp Rounded Filled 버튼 Variant에서 뽑은 값이다. 다른 요소도 같은 이름에 거의 같은 값을 쓴다.

| 이름 | 값 | 이름 | 값 | 이름 | 값 |
|---|---|---|---|---|---|
| Blue | `#078CE4` | Blue (Soft) | `#339AF0` | Blue (Off) | `#009FD0` |
| Cyan | `#00AFDE` | Teal | `#03BCBD` | Green | `#00C45D` |
| Indigo | `#4C6EF5` | Violet | `#7950F2` | Purple | `#CC5DE8` |
| Pink | `#FF64A5` | Red | `#FA5252` | Orange | `#FF873A` |
| Yellow | `#FFB700` | Light | `#AFBCCD` | Dark | `#455D73` |
| Black | `#28313C` | White | `#FFFFFF` (글자 `#3D4D65`) | (베이스) | `#ADB5BD` |

Dark 테마 기본색: 패널 `#2C3642`, 상단 바 `#1C232A`, 구분선 `#374355`, 보조 텍스트·아이콘 `#C9D3DF`.

### 3.4 요소 목록
크기는 RectTransform sizeDelta 기준이다(1920x1280 캔버스 단위). 변형 수는 베이스를 뺀 색 Variant 수다.

| 요소 | 경로 (`UI Elements/` 아래) | 변형 | 베이스 구조 |
|---|---|---|---|
| 버튼 | `Button/Button/{Rounded,Semi Rounded,Sharp Rounded,Square}/{Basic,Gradient}/{Filled,Outline}/` | Basic 15~17색, Gradient 8색 | 200x90. Image + `CleanButton` + `CleanButtonConfig` + AudioSource(`Button-5`) + CanvasGroup / `Text`(TMP, Roboto-Medium 34) |
| 아이콘+텍스트 버튼 | `Button/Button With Icon (Text)/{Rounded,Semi Rounded,Sharp Rounded,Square} - Filled/{Filled,Outline}/` | 14~16색 | 260x90 / `Text`, `Icon`(40x40) |
| 재화 버튼 | `Button/Credits/{Dark,Light}/` | Coins, Gems | 아이콘+텍스트 버튼의 Variant |
| 아이콘 버튼 | `Button/Icon Button/{Circle,Rounded,Semi Rounded,Sharp Rounded,Square}/{Filled,Outline}/` | 14~16색 | 100x100 / `Icon`(50~60) / AudioSource(`Button-2`) |
| 아이콘 배경 | `Icon/{Circle,Rounded,...}/{Filled,Outline}/` | 0~4색 | 100x100 배경 + `Icon`. 버튼 기능 없음 |
| 텍스트 배지 | `Textfield (Size Fitter)/{모양}/{Filled,Outline}/` | 13~14색 | ContentSizeFitter + HorizontalLayoutGroup으로 글자 길이에 맞춰 늘어남 |
| 배경 패널 | `Background/` | Rounded, Semi Rounded(+Outline), Square | 단일 Sliced Image (흰색) |
| 팝업 틀 | `Popup/Base/Popup - {Dark,Light}`, `Popup/Footer/` | - | 1000x1000. Mask / `Top Bar`, `Border`, `Line`, `Title`, `Close Button` |
| 프로그레스 바 | `Progress Bar/{Dark,Light}/{Basic,Gradient}/`, `Horizontal/Horizontal Dots` | Basic 14색, Gradient 6색 | 300x10. Mask / `Border`, `Loading Bar`(Animator) |
| 원형 프로그레스 | `Progress Bar/{Dark,Light}/Radial/` | 기본, Yellow | 300x300. `Progress Bar`(Image Filled + Gradient + Animator), `Title Text`, `Text` |
| 슬라이더 | `Slider/{Dark,Light}/{Basic,Gradient}[ (Circle Handle)]/` | 6~13색 | 300x20. Slider / `Background`, `Fill Area/Fill`, `Handle Slide Area/Handle` |
| 스위치 | `Switch/Switch - {Filled,Outline}` | - | 120x20. Toggle이 아니라 **Slider**(0/1) |
| 체크박스 | `Checkbox/Checkbox - {Dark,Light}` | - | 40x40 Toggle |
| 토글 그룹 | `Toggle/{Dark,Light}/Group/Toggles[ - Text]`, `Single/Toggle - Heart` | - | ToggleGroup + 원형 Toggle 3개 |
| 입력 필드 | `Input Field/{Dark,Light}/{Single,Group}/` | Single: 기본, 배경+아이콘, 검색 / Group: Login, Sign Up, Contact Form | 400x50 `TMP_InputField` |
| 드롭다운 | `Dropdown/Dropdown - {Dark,Light}` | - | - |
| 스크롤바 | `Scrollbar/{Dark,Light}/` | Horizontal, Vertical | - |
| 툴팁 | `Tooltip/{Dark,Light}/Tooltip {Dark,Light}[ - Top/Bottom/Left/Right]` | 화살표 방향 | VerticalLayoutGroup + ContentSizeFitter + CanvasGroup / `Title`, `Text`, `Arrow` |
| 스피너 | `Spinner/` | Circle, Circle - Dark, Dots Horizontal | Circle은 점 8개, 점마다 Animator |
| 로고 | `Logo/Clean Minimalist Logo` | - | 팩 로고. 쓰지 않음 |

## 4. 팝업 프리팹 (`Demo/Resources/Popups/{Dark,Light}/`)
Dark와 Light에 같은 25종이 있다. 이름은 `<테마> - <이름>`이다.

Chat, Coins Shop, Contact, End Game, End Game - Level Up, End Game - Lose, Gems Shop, Info, Inventory, Item (Single), Leaderboard, Level, Life Shop, Log In, Messages, Missions, Modal (Base), Modal - Alert, Modal - Notification, Profile, Settings, Shop, Sign Up, Statistics, Video Player

- 루트에는 `Ricimi.Popup` + Animator(`Animations/Popup/Popup.controller`, Open/Close 상태) + CanvasGroup이 붙어 있다.
- 닫기 계열 버튼의 onClick은 persistent call로 `Ricimi.Popup.Close`에 연결되어 있다. Variant 안에서는 `m_Modifications`의 `...m_MethodName` 값(`Close`)으로 들어 있다.
- `Modal (Base)`는 620폭에 VerticalLayoutGroup + ContentSizeFitter를 써서 내용에 맞춰 높이가 늘어난다(`Icon`, `Title Text`, `Text`, `Buttons`). Alert와 Notification은 이 프리팹의 Variant다.
- `End Game`은 750x880이다. 별 3개, 점수, 시간·킬·아이템 통계, 하단 버튼으로 이루어져 있다. Level Up과 Lose는 이 프리팹의 Variant다.

## 5. 스프라이트 (`Demo/Sprites/`)

### 5.1 Shapes (9-slice)
| 폴더 | 파일 | 크기 | spriteBorder |
|---|---|---|---|
| `Shapes/Rounded/` | `Rounded`, `Rounded - Stroke {2,4,6,8}px` | 256 | 128 |
| `Shapes/Semi Rounded/` | `Semi Rounded`, `Semi Rounded - Stroke {2,4,6,8}px` | 256 | 30 |
| `Shapes/Square/` | `Square`, `Square - Stroke 2px` | 256 | 5 |
| `Shapes/Circle/` | `Circle - {32,64,256}`, `Circle - Stroke {2,3,4,6,8,10}px` | 32~256 | 없음 (Simple) |

### 5.2 아이콘
- 전부 256x256 흰색 단색 PNG이고, Sprite(Single)로 임포트되어 있다. 색은 `Image.color`로 입힌다. `Sources/Icons/Icons/PNG`에 같은 아이콘의 512px 원본이 있다.
- `Icons/Icons/Game/<분류>/`: Animals, Coins, Commerce, Food, Gems, Gift, Heart, Horror, Magic, Map, Other, Potion, Time, Tools, Trophies, Weather
- `Icons/Icons/UI/<분류>/`: Arrows, Basic, Chat, Cloud, Compass, Gender, Lock, Mail, Media, Navigation, Settings, Social, Sound, User
- `Icons/Emojis/{Filled,Outline}/`: 표정 32종
- `Icons/Icons Circle/`: 원 안에 기호가 든 아이콘 13종(Arrow, Checked, Close, Help, Info, Minus, Notification, Play, Plus, Warning 등)
- 일부 파일 이름에 원본 오타나 공백이 있다(`Hourglass .png`, `Binocularsses`, `Play - Outlune`, `Wifi  2`, `User-  Minus Right`). 이름을 짐작하지 말고 Glob으로 확인한다.

### 5.3 게임 개념 → 아이콘
`Icons/Icons/` 아래 경로다.

| 개념 | 아이콘 | 개념 | 아이콘 |
|---|---|---|---|
| 골드 | `Game/Coins/Coin (Star)`, `Coins x3` | 보석 | `Game/Gems/Gem - Filled` |
| 스태미나 | `UI/Basic/Bolt` | HP | `Game/Heart/Heart - Health`, `Heart` |
| 킬 수, 도전 탭 | `Game/Horror/Skull` | 경과 시간 | `Game/Time/Timer`, `Clock Basic` |
| 일시정지 / 계속 | `UI/Navigation/Pause`, `Play` | 홈 | `UI/Navigation/Home` |
| 닫기 | `UI/Navigation/Close (Popup)`, `Close - Thin (Popup)` | 뒤로가기 | `UI/Arrows/Arrow Left` |
| 설정 | `UI/Settings/Settings` | 사운드 on/off | `UI/Sound/Volume`, `Volume Off`, `Music`, `Music Off` |
| 잠금 / 해금 | `UI/Lock/Lock`, `Lock - Unlocked` | 등급 별 | `UI/Basic/Star` |
| 경고(보스 습격) | `UI/Basic/Alert` | 체크(획득함) | `UI/Basic/Checkmark` |
| 상점 탭 | `Game/Commerce/Shopping Cart`, `Shop (Building)` | 장비 탭 | `Game/Tools/Sword`, `Shield` |
| 보상 상자 | `Game/Coins/Chest`, `Game/Gift/Gift` | 열쇠 | `Game/Magic/Key` |
| 자석 | `Game/Tools/Magnet` | 폭탄 | `Game/Tools/Bomb` |
| 회복 | `Game/Potion/Potion - Full`, `Game/Heart/Heart - Plus` | 정보(i) | `UI/Basic/Info` |
| 새로고침 | `UI/Arrows/Reload` | 광고 | `UI/Media/Video` |
| 프로필 | `UI/User/Profile` | 랭킹 | `Game/Trophies/Cup`, `UI/Basic/Ranking` |

### 5.4 기타
- `Backgrounds/Main/{Dark,Light} Background`: 약 2280x1440 가로 배경. `Variations/`에 색 변형 15종이 있다.
- `Avatars/{Boy,Girl} 1~6`: 256px 컬러 일러스트. 팩에서 흰색 단색이 아닌 유일한 스프라이트다.
- `Logos/`: 팩·제작사 로고. 쓰지 않는다.

## 6. 폰트, 사운드, 애니메이션, 팔레트
- **폰트**: `Roboto-{Light,Regular,Medium,Bold} SDF`, `RobotoCondensed-{Light,Regular,Bold} SDF`, `Vegur-{Light,Regular,Bold} SDF`. 모두 Static 512 아틀라스이고 fallback이 없어서 한글이 네모로 깨진다. 프리팹은 주로 Roboto-Medium(버튼, 제목), Roboto-Regular·Light(본문), RobotoCondensed-Light(팝업 제목)를 쓴다.
- **사운드**: `Button-1~5.wav`, `Close.wav`, `Deep.wav`, `Error.wav`, `Glitch.aif`, `Select.wav`, `Select (High Tone).wav`. 버튼은 `Button-5`, 아이콘 버튼은 `Button-2`, 팝업 닫기는 `Close`를 쓴다. 출처는 `AudioCredits-CleanMinimalistGUIPack.pdf`에 있다.
- **애니메이션** (`Demo/Animations/`)
  | 컨트롤러 | 동작 |
  |---|---|
  | `Popup/Popup.controller` | Open(0.42초, CanvasGroup alpha + scale), Close. 1회 재생 |
  | `Popup Taps/Tap Up.controller` | anchoredPosition.y를 올리는 1회 연출 |
  | `Fade/{Canvas Group,Image,Text}` | alpha 반복 |
  | `Pulse` (scale), `Radar` (fillAmount), `Compass` (회전) | 반복 |
  | `Progress Bar/Basic` (localScale), `Progress Bar/Radial` (fillAmount) | 로딩 반복 |
  | `Spinner/Main.controller` + `Item 1~8.overrideController` | 점 8개가 차례로 나타나는 스피너 |
- **팔레트**: `Demo/Editor/Main Color Palette.colors`(30색), `Complete Color Palette.colors`(132색). 이름 없이 색만 들어 있다. 3.3의 값이 이 팔레트에서 나왔다.

## 7. 스크립트 (`Demo/Scripts/`, namespace `Ricimi`)
| 클래스 | 역할 | 주의 |
|---|---|---|
| `CleanButton : Button` | 호버 시 `onHoverAlpha`, 누를 때 `onClickAlpha`로 CanvasGroup alpha를 바꿈 | 같은 오브젝트에 `CleanButtonConfig`가 없으면 포인터 이벤트에서 NullReferenceException이 난다. CanvasGroup은 없으면 자동으로 추가한다. `Button`의 하위 클래스라서 `Button` 타입 필드에 그대로 연결된다 |
| `CleanButtonConfig` | `fadeTime`(0.2), `onHoverAlpha`, `onClickAlpha` | 프리팹 기본값은 0.6 / 0.4 |
| `Popup` | `Open()`: 어두운 배경 생성, `Close()`: Close 애니메이션 재생 후 `destroyTime`(0.5초) 뒤 Destroy | `GameObject.Find("Canvas")`로 배경을 붙이므로 이름이 `Canvas`인 오브젝트가 있어야 한다. Animator가 있어야 한다. 닫으면 오브젝트를 파괴한다 |
| `PopupOpener` | `popupPrefab`을 Instantiate해서 부모 Canvas 아래에 열기 | 프로젝트의 UIManager 흐름과 맞지 않는다 |
| `SceneTransition` / `Transition` | 색 페이드 후 `SceneManager.LoadScene` | 쓰지 않는다 |
| `Tooltip` | 포인터 enter/exit로 `tooltip` 오브젝트의 CanvasGroup을 페이드 | 툴팁 오브젝트에 CanvasGroup(alpha 0)이 있어야 한다. 호버 기반이다 |
| `Gradient : BaseMeshEffect` | 메시 정점 색에 `Color1`→`Color2` 그라디언트, `Angle` | `UnityEngine.Gradient`와 이름이 겹친다. `using Ricimi;`를 쓰면 이름이 모호해진다 |
| `SpriteSwapper` | `SwapSprite()`로 enabled/disabled 스프라이트를 교대 | Play/Pause, Mute 토글에 쓸 수 있다 |
| `URLOpener` | `Application.OpenURL` | - |
| `Utils` | `FadeIn`/`FadeOut` 코루틴 | - |

## 8. 프로젝트에 적용할 때
프로젝트 UI 구조: `UIManager`(영속, Overlay 레이어), 씬마다 `SceneUIRoot`(HUD/Screen/Popup 캔버스), 뷰는 `UIView`/`UIPopup`을 상속한다. 팝업은 `Dim`(검정 `#00000099`)과 `Panel`로 구성한다(`Assets/Prefabs/UI/PopupFrame.prefab`, `MessagePopup.prefab` 참고).

1. **캔버스**: 팩 Canvas 프리팹을 쓰지 않고, 기존 `BattleUI.prefab`·`MainUI.prefab`의 캔버스 아래에 요소를 둔다.
2. **크기**: 프로젝트 UI는 팩보다 대략 1.3~1.5배 크다. 예를 들어 MessagePopup 버튼은 299x122에 글자 46, 팩 버튼은 200x90에 글자 34다. 같은 화면의 기존 프리팹을 기준으로 크기를 맞춘다.
3. **폰트**: 모든 TMP 텍스트의 Font Asset을 `NanumGothicBold SDF`로 바꾼다. 숫자나 영문만 표시하는 텍스트는 Roboto를 그대로 둘 수 있다.
4. **팝업**: Resources 팝업은 레이아웃 참고용이나 분해할 재료로 쓴다. 가져올 때는
   - `Ricimi.Popup`과 Animator를 떼고, 루트 스크립트가 `UIPopup`을 상속하게 한다.
   - 닫기 버튼의 persistent call(`Ricimi.Popup.Close`)을 지우고 `UIPopup.Close`에 다시 연결한다.
   - 어두운 배경은 `Popup.AddBackground` 대신 프로젝트 팝업처럼 `Dim` 자식 Image로 둔다.
5. **버튼**: `CleanButton`을 유지하면 Ricimi 스크립트에 의존하게 된다. 일반 `Button`으로 바꿔도 코드 쪽(`[SerializeField] Button`)은 그대로 동작한다. 클릭음은 버튼마다 붙은 AudioSource를 onClick에서 `Play`해서 낸다.
6. **프로그레스 바(HP·EXP·보스 HP)**: `Loading Bar`의 Animator가 localScale을 반복 재생하므로 떼어 낸다. 값은 Image Type을 Filled로 바꿔 `fillAmount`로 넣거나, pivot.x를 0으로 두고 `localScale.x`로 넣는다.
7. **프리팹 의존**: 팩 프리팹을 nested 또는 Variant로 두면 팩 프리팹 파일에 의존한다. 완전히 Unpack하면 스프라이트, 폰트, 스크립트 GUID에만 의존한다.
8. **편집 방법**: `.prefab`, `.unity`, `.meta`는 직접 수정하지 않고 Unity CLI로 다룬다(`unity-cli` 스킬). 에디터가 연결되어 있지 않을 때 구조만 확인하려면 YAML을 읽기 전용으로 본다.

## 9. 찾기와 미리보기
- 파일 찾기 (Glob)
  - 버튼: `Assets/Externals/GUIPack-Clean&Minimalist/Demo/Prefabs/UI Elements/Button/**/*Sharp Rounded - Filled - Green.prefab`
  - 아이콘: `Assets/Externals/GUIPack-Clean&Minimalist/Demo/Sprites/Icons/**/*Skull*.png`
- 미리보기: Read 도구로 PNG를 열면 이미지를 볼 수 있다.
  - 완성 화면: `Sources/Mockups/Themes/{Dark,Light}/PNG/`. `Scene - Home`, `Scene - Game`과 팝업 목업(`Alert Popup`, `Level Up Popup`, `Win Popup`, `Lose Popup`, `Settings Popup`, `Shop Popup`, `Profile - Inventory Popup` 등)이 있다.
  - 아이콘과 Shapes는 투명 배경에 흰색이라 미리보기에서 보이지 않을 수 있다. 이름으로 고른다.
- 데모 씬 `Demo/Scenes/UI Elements/{Buttons,Icon Buttons,UI Elements}.unity`에서 요소를 한 화면에 모아 볼 수 있다. 데모 씬의 EventSystem은 `InputSystemUIInputModule`을 쓰므로 Play해도 된다.
