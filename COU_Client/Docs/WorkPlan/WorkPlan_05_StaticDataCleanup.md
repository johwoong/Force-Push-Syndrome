# WorkPlan 05: 정적 데이터 관리 정리 (StaticDataCleanup)

## 목표
- 정적 데이터를 모두 `JsonDataManager` 한 곳에서 로드·보관·서버 갱신한다. 지금은 `ItemDatabase`, `SkillDataBase`, `PlayerDatabase`가 따로 들고 있다.
- 서버 갱신을 거치지 않는 값(플레이어 기본 스탯, 장비 강화·합성 상수)을 서버 테이블(PlayerBaseStat, ItemConst)에서 읽게 한다.
- 읽지 않는 파일, 실제와 다른 주석과 문서를 정리한다.
- 입력: 유저가 직접 제시한 요구(정적 데이터 현황조사에서 나온 정리 항목 12개). 별도 기획명세는 없다.

## 범위
**포함**
- Skill: `SkillDataBase` 삭제, 호출처를 `JsonDataManager`로 교체
- Item: `ItemDatabase`의 데이터 보관을 `JsonDataManager`로 이동. `GetGradeIcon`만 남긴다
- PlayerBaseStat: `PlayerDatabase`와 `Resources/Data/player/player.json`을 삭제하고 `PlayerBaseStat` 테이블로 전환
- ItemConst: `ItemLevelConfig`의 하드코딩 상수를 `ItemConst` 테이블 값으로 교체
- `Resources/Data/BossAttack.json` 삭제
- `JsonDataManager` API 정리: 사용처 없는 조회 메서드 삭제, `MonsterDataDic` 읽기 전용화
- `JsonDataManagerEditor`: 모든 테이블 표시
- 주석과 문서 갱신

**제외**
- Shop 테이블 연결(합의). 골드·보석 상품 기능이 없어 가격만 옮기면 반쪽 작업이 된다. `ShopSupplyBoxCard`는 프리팹 직렬화 값을 계속 쓴다
- `Awake`와 `ApplyStaticDataResponse`의 테이블 목록 일원화(합의: 두 목록 유지)
- `ItemDatabase` 이름 변경·이동
- 서버 레포 작업

## 사전 조건
- **공개 API 변경**: 아래 변경을 수정 대상 파일의 담당자에게 먼저 알린다.

| 변경 | 이전 | 이후 |
|---|---|---|
| 스킬 조회 | `SkillDataBase.Get(skillId)` | `GameManager.JsonData.GetSkillDataFromJson(skillId)` |
| 아이템 조회 | `ItemDatabase.Get(itemId)` (없으면 예외) | `GameManager.JsonData.GetItemDataFromJson(itemId)` (없으면 null + 경고) |
| 아이템 전체 | `ItemDatabase.GetAll()` | `GameManager.JsonData.ItemDataDic.Values` |
| 아이템 로드 | `ItemDatabase.Load()` | 없음. `JsonDataManager.Awake`가 로드한다 |
| 기본 스탯 | `PlayerDatabase.BaseStats.attack` 등 | `GameManager.JsonData.PlayerBaseStatData.playerBaseAttack` 등 |
| 장비 상수 | `ItemLevelConfig.MaxLevel`, `SynthesisMaterialCount` (const) | 같은 이름의 static 속성. 호출 코드는 그대로다 |
| 등급 아이콘 | `ItemDatabase.GetGradeIcon` | 변경 없음 |

## 구조 개요

```
이전                                         이후
JsonDataManager  9개 테이블                   JsonDataManager  12개 테이블
ItemDatabase     Item (지연 로드)        ──▶   Monster, MonsterAttack, Wave, SpawnPattern, Stage,
SkillDataBase    Skill (중복 파싱)              Skill, DropItem, DropTable, AccountConst,
PlayerDatabase   player.json (갱신 우회)        + Item, PlayerBaseStat, ItemConst
ItemLevelConfig  const (갱신 우회)             ItemLevelConfig → JsonDataManager.ItemConstData를 읽는다
                                              ItemDatabase    → GetGradeIcon만 남는다
```

## 작업 항목
| # | 작업 단위 | 산출물(클래스/파일 경로) | 의존 |
|---|---|---|---|
| 1 | 사전 확인 (Unity CLI): `JsonDataManager`가 배치된 씬과 `isDontDestroy` 값을 확인한다. 각 씬에서 `PlayerInventory.Awake`처럼 다른 `Awake`가 `JsonDataManager.Awake`보다 먼저 아이템·스탯을 읽을 수 있는지 확인한다. 에디터를 새로고침해 `PlayerBaseStat.json.meta`가 생성됐는지 확인한다(지금 없음) | 본 문서 "사전 확인 결과" 절 | - |
| 2 | 경로 상수 2개 추가: `PlayerBaseStatData_Json_Path = "Data/PlayerBaseStat"`, `ItemConstData_Json_Path = "Data/ItemConst"` | `Assets/Scripts/Utils/GameConstants.cs` | - |
| 3 | ItemConstData 클래스 | `Assets/Scripts/Data/ItemConstData.cs` | - |
| 4 | PlayerBaseStatData 이동 (Unity CLI `AssetDatabase.MoveAsset`, GUID 유지), 필드명을 JSON 키로 변경, 머리 주석 수정 | `Assets/Scripts/Data/PlayerBaseStatData.cs` | - |
| 5 | JsonDataManager: Item 로드·조회·갱신. `ItemDatabase.Parse`를 `ParseItemData`로 옮기고 `ItemDatabase.Replace` 호출을 제거한다 | `Assets/Scripts/Mangers/JsonDataManager.cs` | #1 |
| 6 | JsonDataManager: PlayerBaseStat·ItemConst 로드·갱신 | `JsonDataManager.cs` | #1, #2, #3, #4 |
| 7 | JsonDataManager API 정리: `GetMonsterAttackDataFromJson`, `GetWaveEntryDataFromJson` 삭제, `MonsterDataDic` 읽기 전용, 에디터용 `SpawnPatternDataDic`·`DropTableDic` 추가 | `JsonDataManager.cs` | - |
| 8 | Skill 호출처 교체: `BattleManager.cs:218`, `PlayerLuckTrain.cs:102,121`, `SkillFactory.cs:31` | 각 파일 | - |
| 9 | SkillDataBase 삭제 (Unity CLI `AssetDatabase.DeleteAsset`), `JsonDataManager`의 `SkillDataBase.Replace` 호출 제거, `SkillDataListWrapper` 삭제 | `Assets/Scripts/Skills/Data/SkillDataBase.cs`(삭제), `JsonDataManager.cs`, `Assets/Scripts/Skills/Data/SkillData.cs` | #8 |
| 10 | Item 호출처 교체: `OwnedItem.cs:15`(`GetItemDataFromJson`), `PlayerInventory.cs:31`(Load 제거), `PlayerInventory.cs:309`(`ItemDataDic.ContainsKey`), `PlayerStats.cs:97`(Load 제거), `ServerLoadManager.cs:120`(Load 제거), `ServerLoadManager.cs:148`(`ItemDataDic.TryGetValue`), `ShopSupplyBoxCard.cs:48`(`ItemDataDic.Values`) | 각 파일 | #5 |
| 11 | ItemDatabase 축소: `items`, `Load`, `Replace`, `Parse`, `Get`, `GetAll` 제거. `GetGradeIcon`만 남긴다 | `Assets/Scripts/Data/ItemDataBase.cs` | #10 |
| 12 | PlayerStats 전환: `Base`를 `GameManager.JsonData.PlayerBaseStatData`로 바꾸고 필드 참조를 교체한다(31-36, 67, 134-135, 182-183행) | `Assets/Scripts/Player/PlayerStats.cs` | #6 |
| 13 | PlayerDatabase.cs, player.json, `player` 폴더 삭제 (Unity CLI) | `Assets/Resources/Data/player/`(삭제) | #4, #12 |
| 14 | ItemLevelConfig: const를 static 속성으로 바꾸고 `ItemConstData`에서 읽는다 | `Assets/Scripts/Data/ItemLevelConfig.cs` | #6 |
| 15 | BossAttack.json 삭제 (Unity CLI) | `Assets/Resources/Data/BossAttack.json`(삭제) | - |
| 16 | 주석 수정: `MonsterAttackData.cs:4,11`("보스 공격" → 몬스터 공격), `SaveData.cs:164`(`Item.itemMaxLevel` → `ItemConst.maxLevel`), `ItemData.cs:24`(`ItemDatabase.Load()` → `JsonDataManager`) | 각 파일 | - |
| 17 | JsonDataManagerEditor: SpawnPattern, DropTable, DropItem, Item 목록과 AccountConst, PlayerBaseStat, ItemConst 단일 행 표시 추가 | `Assets/Editor/JsonDataManagerEditor.cs` | #5, #6, #7 |
| 18 | 검증 | 본 문서 하단 "검증 결과" | #1~#17 |
| 19 | 문서 갱신 | `Docs/ETC/StaticDataClient.md`, `Docs/ETC/GameData_Tags.md` | #18 |

## 합의된 구현 결정

### 1. 보관 위치 (합의: 모두 JsonDataManager)
- 정적 데이터는 모두 `JsonDataManager`가 `Awake`에서 로드하고 `ApplyStaticDataResponse`에서 교체한다. 읽기 경로는 `ReadTableText`(저장본 → 빌드 사본) 하나다.
- 다른 클래스는 데이터를 보관하지 않는다. `ItemLevelConfig`는 계산 메서드만 남기고 값은 `JsonDataManager`에서 읽는다.
- 기존 테이블의 형식은 바뀌지 않으므로 `STATIC_DATA_MAJOR_VERSION`(2)은 그대로 둔다.

### 2. Item
```csharp
private Dictionary<long, ItemData> itemDataDic;
public IReadOnlyDictionary<long, ItemData> ItemDataDic => itemDataDic;
public ItemData GetItemDataFromJson(long itemId);   // 기존 Get*FromJson과 같은 null·미존재 처리
private static Dictionary<long, ItemData> ParseItemData(JObject root);   // ItemDatabase.Parse 본문 그대로
```
- 지금의 지연 로드(`ItemDatabase.Load()`를 처음 필요할 때 호출)를 없애고 `Awake`에서 다른 테이블과 함께 로드한다.
- 없는 itemId를 조회하면 예외 대신 null과 경고를 반환한다. 지금 `OwnedItem.Data`는 예외가 나고, 바꾼 뒤에는 호출한 곳에서 NullReferenceException이 난다. 실패 지점만 달라진다.
- `ItemDatabase.GetGradeIcon`은 테이블 데이터가 아니라 스프라이트 경로 규칙이므로 옮기지 않는다.

### 3. Skill
- `SkillDataBase`를 삭제한다. `JsonDataManager.skillDataDic`(Newtonsoft, `OnAfterDeserialize` 수동 호출)만 남는다.
- `SkillDataListWrapper`는 `SkillDataBase`만 쓰므로 함께 삭제한다. `SkillData`의 `ISerializationCallbackReceiver`는 남긴다. `ParseSkillData`가 `OnAfterDeserialize`를 직접 호출한다.
- `GetSkillDataFromJson`은 없는 ID에 null과 경고를 반환한다. `SkillDataBase.Get`도 null을 반환했으므로 호출처 동작은 같고, 경고 로그만 추가된다.

### 4. PlayerBaseStat
```csharp
// PlayerBaseStat.json의 유일한 행. 필드 이름이 JSON 키와 같아야 한다
[Serializable]
public class PlayerBaseStatData
{
    public int playerBaseAttack = 100;
    public int playerBaseHp = 1000;
    public int playerBaseCriticalDamage = 200;
    public int playerBaseCriticalChance = 5;
    public int playerBaseSkillDamage = 100;
    public float playerBaseMoveSpeed = 9f;
    public float playerBaseMaxMoveSpeed = 16f;
    public float playerBaseLootRadius = 2f;

    public void Sanitize();   // 지금 보정 규칙 그대로, 필드명만 교체
}
```
- 필드명은 다른 데이터 클래스처럼 JSON 키와 같게 한다. 매핑 어트리뷰트는 쓰지 않는다.
- 초기값은 남긴다. Newtonsoft `ToObject`는 JSON에 없는 키의 초기값을 유지한다.
- `ParsePlayerBaseStatData`: `ParseAccountConstData`와 같은 단일 행 검사를 하고 `Sanitize()`를 호출한다.
- JsonDataManager 속성: `public PlayerBaseStatData PlayerBaseStatData`
- 로드에 실패하면 다른 테이블처럼 null이 된다. 지금 `PlayerDatabase`는 파일이 없으면 기본값 객체를 썼다(미해결 참고).

### 5. ItemConst
```csharp
// ItemConst.json의 유일한 행
[Serializable]
public class ItemConstData
{
    public int maxLevel;
    public int levelUpBaseCost;
    public float statGrowthPerLevel;
    public int synthesisMaterialCount;
    public float[] gradeStatMultiplier;   // ItemGrade 순서
}
```
- `ParseItemConstData`는 단일 행을 검사하고, `gradeStatMultiplier` 길이가 `ItemGrade` 개수(3)와 다르면 예외를 던진다. `GetGradeMultiplier`가 `(int)grade`로 인덱싱하기 때문이다.
- JsonDataManager 속성: `public ItemConstData ItemConstData`
- `ItemLevelConfig`
  - `MaxLevel`, `SynthesisMaterialCount`: const를 같은 이름의 static 속성으로 바꾼다. const 문맥(case 라벨, 어트리뷰트 인자, const 초기화)에서 쓰는 곳은 없다.
  - `GetLevelUpCost`, `GetStatMultiplier`, `GetGradeMultiplier`: 시그니처는 그대로 두고 값만 `GameManager.JsonData.ItemConstData`에서 읽는다.
  - `BaseCost`, `StatGrowthPerLevel`, `GradeStatMultiplier` 상수는 삭제한다.

### 6. 테이블 목록 (합의: 두 목록 유지)
- `Awake`와 `ApplyStaticDataResponse`에 Item, PlayerBaseStat, ItemConst를 각각 추가해 12개로 만든다.
- 시작 로드는 테이블별로 실패를 허용하고, 서버 갱신은 모두 성공해야 교체한다. 이 차이를 유지한다.
- `ApplyStaticDataResponse`의 부수 처리는 `RebuildMonsterAttackIndex`만 남는다(`SkillDataBase.Replace`, `ItemDatabase.Replace` 제거).

### 7. JsonDataManager API (합의: 미사용 멤버 삭제)
- 삭제: `GetMonsterAttackDataFromJson`, `GetWaveEntryDataFromJson`
- `DropItemDataDic`은 유지한다. 삭제 대상이었지만 #17에서 에디터가 사용한다.
- `GetSkillDataFromJson`은 #8부터 사용처가 생긴다.
- `MonsterDataDic`: `Dictionary` → `IReadOnlyDictionary`
- 에디터용 추가: `IReadOnlyDictionary<int, SpawnPatternData> SpawnPatternDataDic`, `IReadOnlyDictionary<int, List<DropTableEntryData>> DropTableDic`

### 8. JsonDataManagerEditor (합의: 전부 추가)
- `DrawDictionary`의 `TKey : IComparable<TKey>` 제약을 `IComparable`로 완화한다. DropItem은 `DropItemType` enum이 키이고, enum은 제네릭 `IComparable<T>`를 구현하지 않는다. `List<TKey>.Sort()`는 enum도 정렬한다.
- 목록 추가
  - SpawnPattern: 라벨 `{eventType} / {formation}`
  - DropTable: 값이 행 목록이다. 라벨은 `행 {n}개`이고, 펼치면 행마다 dropGroup, dropItemType, weight, count를 표시한다
  - DropItem: 라벨 `ID {dropItemId}`. 키가 이미 `DropItemType`이라 `{dropItemType}`을 쓰면 `Gold / Gold`처럼 겹친다
  - Item: 키가 long이다. 라벨 `{itemName}`
- 단일 행: `DrawSingle<T>(string title, T data, Action<T> drawData)`를 추가해 AccountConst, PlayerBaseStat, ItemConst를 표시한다. null이면 기존과 같은 "아직 로드된 … 없습니다" 안내를 표시한다.
- 펼침 상태 `HashSet`은 지금처럼 테이블마다 따로 둔다.
- 각 Draw 메서드는 해당 데이터 클래스의 공개 필드를 모두 표시한다(기존 `DrawMonster` 방식).

### 9. 에셋 이동·삭제
- `.cs`, `.json` 이동·삭제는 Unity CLI(`AssetDatabase.MoveAsset`, `AssetDatabase.DeleteAsset`)로 한다. `.meta`를 직접 다루지 않는다.
- #4 이동은 GUID를 유지한다. `PlayerBaseStatData`를 직렬화한 에셋은 없지만 규칙대로 한다.
- `PlayerBaseStat.json`은 아직 git에 없고 `.meta`도 없다. 커밋할 때 JSON과 `.meta`를 함께 올린다.

## 의존 순서 / 병렬 가능 그룹
- **#1을 가장 먼저 한다.** 결과에 따라 #5, #6 전에 추가 합의가 필요할 수 있다(미해결 참고).
- **그룹 A, 데이터 클래스·상수**: #2, #3 병렬. #4는 단독으로 끝내면 컴파일이 깨지므로 그룹 D의 묶음으로 진행한다
- **그룹 B, Skill**: #8 → #9. #8 뒤에도 컴파일이 깨지지 않는다.
- **그룹 C, Item**: #5 → #10 → #11. #5는 `ItemDatabase`를 남긴 채 추가하므로 #10 전까지 컴파일된다. 다만 #5부터 #10 전까지는 서버 갱신이 `ItemDatabase`에 반영되지 않으므로 한 묶음으로 진행한다.
- **그룹 D, Const**: A 완료 → #6 → #12 → #13, #6 → #14
  - #4에서 필드명을 바꾸면 #12 전까지 `PlayerStats`가 컴파일되지 않는다. #4 → #6 → #12를 한 묶음으로 진행한다.
- **독립**: #7, #15, #16
- **직렬**: #5, #6, #7 완료 → #17 → #18 → #19
- 각 묶음이 끝날 때 OmniSharp 진단과 Unity 콘솔로 컴파일을 확인한다.

## 사전 확인 결과
2026-10-09, Unity CLI로 확인했다.
- `JsonDataManager`는 LogInScene의 `DontDestroyManagers/JsonDataManager`에만 있고 `isDontDestroy`는 true다. 다른 씬과 프리팹에는 없다. 실행 순서는 기본값(0)이다.
- MainScene, BattleScene, 테스트 씬에는 `JsonDataManager`가 없다. LogInScene을 거치면 이미 로드된 인스턴스가 남아 있다. 직접 실행하면 `Instance`가 `FindFirstObjectByType`에서 null을 받고 `AddComponent`로 만들며, 이때 `Awake`가 바로 실행되어 로드가 끝난 뒤 반환된다.
- LogInScene에서 아이템을 읽는 `ServerLoadManager.ApplyInventory`는 로그인 흐름에서 호출되므로 `Awake` 이후다.
- 결론: Awake 순서 위험은 없다. #5, #6 전에 추가 합의가 필요하지 않다.
- 에디터 새로고침 뒤 `PlayerBaseStat.json.meta`가 생성됐다.

## 결정 (2026-10-09)
- ItemConst 값 검증: `gradeStatMultiplier` 길이만 검사한다(계획 그대로).
- 아이템 존재 확인: `PlayerInventory.cs:309`는 `ItemDataDic.ContainsKey`, `ServerLoadManager.cs:148`은 `ItemDataDic.TryGetValue`를 쓴다.

## 검증 항목
**컴파일·로드**
- 컴파일 에러가 없다(OmniSharp 진단, Unity 콘솔).
- `persistentDataPath/StaticData`를 비우고 서버 없이 LogInScene을 실행하면 12개 테이블이 빌드 사본으로 로드되고, 로드 에러가 없다. 저장본이 남아 있으면 저장본을 읽으므로 빌드 사본 검증이 되지 않는다.
- 서버를 켜고 실행하면 `정적 데이터를 {version}(으)로 갱신했습니다.` 로그가 남고, `persistentDataPath/StaticData`에 `PlayerBaseStat.json`, `ItemConst.json`이 저장된다.

**값 동일성 (변경 전과 같아야 한다)**
- BattleScene: `[PlayerStats]` 로그의 기본 Atk 100, Hp 1000. 이동 속도 9, 최대 16, 루팅 반경 2
- MainScene: 장비 최대 레벨 10, 강화 비용 `1000 × 현재 레벨`, 합성 재료 2개, 합성 창 등급 보너스(우수 +75%, 레어 +175%)
- 인벤토리, 장비 상세, 합성 창, 보급 상자 결과의 아이콘과 이름이 이전과 같다
- 레벨업 선택 창과 행운열차의 스킬 이름·설명이 이전과 같다

**저장본 경로**
- 서버 갱신으로 저장본이 생긴 뒤에 진행한다.
- 저장본 `PlayerBaseStat.json`의 `playerBaseHp`를 바꾸고 다시 실행하면 PlayerStats 로그에 반영된다. 확인 후 되돌린다.

**데이터 오류**
- 읽히는 쪽 `ItemConst.json`(저장본이 있으면 저장본)의 `gradeStatMultiplier`를 2개로 줄이면 `ItemConst 데이터 로드 실패` 로그가 남는다. 이어서 장비 관련 코드에서 나는 NullReferenceException은 예상된 결과다(미해결 참고). 확인 후 되돌린다.

**에디터**
- Play 중 JsonDataManager 인스펙터에 12개 테이블이 모두 표시된다.

## 검증 결과
2026-10-09. 처음에는 서버가 꺼져 있었고(`localhost:8080` 연결 거부) `persistentDataPath/StaticData`는 없었다. 서버 갱신 항목만 서버를 켠 뒤 다시 실행했다.

| 항목 | 결과 |
|---|---|
| 컴파일 | 통과. Unity `compilationFailed: false`, 콘솔 에러 0. 컴파일된 어셈블리에 `ItemConstData`가 있고 `SkillDataBase`, `PlayerDatabase`는 없다. OmniSharp MCP는 이 세션에 없어 쓰지 못했다 |
| 빌드 사본 로드 (LogInScene) | 통과. 12개 테이블 로드(Monster 49, MonsterAttack 50, Wave 7, SpawnPattern 9, Stage 1, Skill 7, DropItem 10, DropTable 5, Item 19, AccountConst·PlayerBaseStat·ItemConst 각 1행). 로드 에러 없음. 경고는 서버 연결 실패 2건뿐 |
| 서버 갱신 | 통과(서버 기동 후 재실행). `정적 데이터를 2.2.0(으)로 갱신했습니다.` 로그가 남았다. `StaticData`에 13개 테이블(`PlayerBaseStat.json`, `ItemConst.json` 포함)과 `version.txt`(2.2.0)가 저장됐다. 갱신 뒤 메모리 값이 빌드 사본과 같다 |
| 기본 스탯 (BattleScene 직접 실행) | 통과. `[PlayerStats] … 기본 Atk: 100, 기본 Hp: 1000`. 이동 속도 9, 최대 16, 루팅 반경 2, 치명타 5/200, 스킬 피해 100. `JsonDataManager`는 자동 생성 1개 |
| 장비 상수 | 통과(Play 중 eval). 최대 레벨 10, 강화 비용 1레벨 1000·5레벨 5000, 3레벨 배율 1.2, 합성 재료 2, 등급 배율 1.75·2.75(우수 +75%, 레어 +175%). `OwnedItem.Data`가 `ItemDataDic`의 같은 객체를 반환한다 |
| 스킬 조회 | 통과(eval). `GetSkillDataFromJson(20010)` = Shuriken/Nearest, `SkillFactory.Create(20010)` = `Skill_Shuriken` |
| 장비 화면 (MainScene, 테스트 계정 로그인) | 통과. 테스트 장비는 `PlayerInventory.AddItem`으로 메모리에만 넣었다(서버 저장 없음). 장비 상세: 가죽 갑옷, 아이콘·등급 프레임, `레벨: 1/10`, 강화 비용 `0/1000`. 합성 창: `등급 일반 → 우수`, `스탯 +75%`, `필수 자료: 2x … (보유 2/2)`, 우수 장비는 `스탯 +175%`. 보급 상자: `ItemDataDic.Values`에서 가죽 벨트 지급, 결과 팝업 아이콘·이름 표시. 인벤토리 탭: 장비 6개 아이콘·등급 프레임 표시 |
| 레벨업 창·행운열차 (BattleScene 직접 실행) | 통과. 레벨업 선택지 Katana/카타나, Branch/브랜치, Revolver/리볼버. 행운열차 슬롯·보상에 Katana 표시. 에러 없음 |
| 저장본 경로 | 통과. 저장본 `PlayerBaseStat.json`의 `playerBaseHp`를 1234로 바꾸고(유저가 직접 수정) BattleScene을 직접 실행하자 `[PlayerStats] … 기본 Hp: 1234`가 남았다. 빌드 사본은 1000 그대로였다 |
| 데이터 오류 | 통과. 빌드 사본 `gradeStatMultiplier`를 2개로 줄이자 `ItemConst 데이터 로드 실패: ItemConst의 gradeStatMultiplier는 3개(ItemGrade 순서)여야 합니다.`가 남고 `ItemConstData`가 null이 됐다. 파일은 되돌렸다 |
| 에디터 인스펙터 | 미실시(화면 확인 불가). 컴파일은 통과했다 |

## 미해결 / 실행 시 확인 필요
- **PlayerBaseStat 기본값 대체**: 로드에 실패하면 `PlayerBaseStatData`가 null이 되어 `PlayerStats`에서 예외가 난다. 빌드 사본이 항상 포함되므로 데이터가 손상됐을 때만 해당한다.
- **ItemConst 로드 실패**: 실패하면 `ItemConstData`가 null이 되어 `OwnedItem.MaxLevel`, `ScaledAttack`, `NextLevelUpCost`가 예외를 던진다. 인벤토리 UI, 합성 창, `PlayerStats.CalculateFromEquipped`(BattleScene 포함)가 함께 깨진다. 지금은 const라 실패할 수 없으므로 이번 작업으로 새로 생기는 실패 경로다.
- **서버 테이블 포함 여부(해결)**: 2026-10-09 확인. 서버(2.2.0)가 AccountConst, DropItem, DropTable, Item, ItemConst, Monster, MonsterAttack, PlayerBaseStat, Shop, Skill, SpawnPattern, Stage, Wave를 보낸다. PlayerBaseStat, ItemConst 값은 빌드 사본과 같다.
- **Shop**: 범위에서 제외했다(합의). `Shop.json`은 서버에서 받아 저장만 되고 읽히지 않는다. `ShopSupplyBoxCard`의 가격·등급 범위는 프리팹 값이다.
- **ItemDatabase 이름**: #11 뒤에는 `GetGradeIcon`만 남아 이름과 역할이 맞지 않는다. 이름 변경·이동은 범위 밖이다.
- **기기 저장본의 BossAttack.json(해결)**: 서버가 BossAttack을 보내지 않는다. 이전에 받은 저장본이 있는 기기에만 남는다.
