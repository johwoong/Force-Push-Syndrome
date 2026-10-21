# COU_Client

Unity로 개발한 2D 스테이지 생존 게임 Force Push Syndrome의 클라이언트입니다. 플레이어는 웨이브마다 등장하는 적을 상대하며 경험치를 모으고, 레벨업 때 스킬을 선택해 전투 구성을 확장합니다. 로그인, 장비와 성장, 전투 보상, 친구와 채팅 등 계정 기반 기능은 별도 서버와 연동합니다.

## 주요 기능

- **스테이지 전투**: 스테이지 로스터와 웨이브·스폰 패턴 JSON을 조합해 적의 종류, 등장 시점, 배치 형태와 능력치 배율을 결정합니다. 일반 적, 엘리트, 보스 전투를 처리합니다.
- **스킬 성장**: 전투 중 획득한 경험치로 레벨을 올리고 액티브·패시브 스킬을 선택하거나 강화합니다. 스킬 데이터와 레벨별 효과는 정적 데이터 테이블에서 읽습니다.
- **드롭 아이템**: Unity Entities와 Burst Job으로 획득 범위 감지 및 플레이어를 향한 이동을 처리합니다. 화면에 보이는 아이템 오브젝트는 풀에서 재사용합니다.
- **계정 및 보상**: REST API로 로그인, 플레이어 데이터와 인벤토리 조회, 전투 입장과 결과 제출을 처리합니다. 서버 응답을 받은 뒤 보상과 진행 상태를 반영합니다.
- **소셜 기능**: 친구, 랭킹, 업적, 1:1 채팅 UI를 제공합니다. 채팅은 REST로 내역 조회·전송을 처리하고 WebSocket으로 새 메시지를 수신합니다.

## 기술 구성

| 영역 | 사용 기술 및 구현 |
| --- | --- |
| 엔진·렌더링 | Unity 6000.3.24f1, Universal Render Pipeline 2D |
| 입력·UI | Input System, uGUI, TextMeshPro, DOTween |
| 전투 | C# `MonoBehaviour`, 데이터 기반 웨이브·스킬, 오브젝트 풀 |
| 드롭 처리 | Unity Entities 1.4.8, Burst, Job System |
| 통신 | `UnityWebRequest` 기반 REST API, WebSocket 채팅, JSON 직렬화 |

### 주요 시스템

- [`WaveManager`](Assets/Scripts/Mangers/WaveManager.cs)는 스테이지의 웨이브 항목을 로스터의 몬스터·배율 및 스폰 패턴과 결합해 생성 시점을 관리합니다. [`MonsterSpawner`](Assets/Scripts/Mangers/MonsterSpawner.cs)는 적을 풀에서 꺼내 초기화하고 처치 이벤트를 전달합니다.
- [`BattleManager`](Assets/Scripts/Mangers/BattleManager.cs)는 경험치, 스킬 선택, 일시정지, 승패와 결과 전송을 관리합니다. [`SkillController`](Assets/Scripts/Skills/Controller/SkillController.cs)는 장착한 액티브·패시브 스킬을 갱신합니다.
- [`JsonDataManager`](Assets/Scripts/Mangers/JsonDataManager.cs)는 `Resources/Data`의 기본 테이블을 로드하고 서버 정적 데이터의 버전을 확인해 로컬 저장본을 갱신합니다.
- [`ApiClient`](Assets/Scripts/Network/ApiClient.cs)는 인증 토큰, 요청·응답 JSON, 네트워크 및 HTTP 오류 처리를 공통화합니다. [`ServerChatManager`](Assets/Scripts/Mangers/SaveLoad/Server/ServerChatManager.cs)는 WebSocket 재연결 후 누락 메시지를 조회하고, 연결이 끊긴 동안 폴링으로 전환합니다.
- [`UIManager`](Assets/Scripts/UIManager/UIManager.cs)는 씬별 뷰와 팝업을 등록하고, 상단 팝업 닫기와 테마 변경을 관리합니다.

## 실행

1. Unity Hub에서 **Unity 6000.3.24f1**로 프로젝트 루트를 엽니다.
2. [`LogInScene`](Assets/Scenes/GameScenes/LogInScene.unity)을 열고 Play를 실행합니다. 빌드에 등록된 씬 순서는 `LogInScene` → `MainScene` → `BattleScene`입니다.
3. 로그인과 계정 데이터, 전투 입장·결과 등 서버 기능을 사용하려면 호환되는 별도 백엔드가 실행 중이어야 합니다. 에디터의 기본 API 주소는 `http://localhost:8080`이며 [`GameConstants.Server.BASE_URL`](Assets/Scripts/Utils/GameConstants.cs)에서 설정합니다.

## 디렉터리

```text
Assets/
├─ Scenes/GameScenes/   로그인·메인·전투 씬
├─ Scenes/TestScenes/   기능 확인용 씬
├─ Scripts/             전투, 플레이어, 스킬, UI, 네트워크 코드
├─ Resources/Data/      기본 게임 데이터 JSON
└─ Prefabs/             게임 오브젝트와 UI 프리팹
Docs/                   기능별 작업 계획과 개발 가이드
```

클라이언트 소스와 Unity 에셋은 이 저장소에 있으며, 서버 소스는 포함되지 않습니다.
