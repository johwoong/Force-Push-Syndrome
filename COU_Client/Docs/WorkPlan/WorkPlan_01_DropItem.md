# WorkPlan 01: 드롭아이템 (DropItem)

## 목표
- 드롭아이템 13종의 생성 → 감지 → 흡수 이동 → 루팅 통지를 Unity Entities(ECS)와 Burst Job으로 구현한다.
  - 13종: 경험치잼 4, 골드 4, 행운상자, 보상상자, 회복약, 자석, 폭탄
- 드롭이 수천 개 쌓인 상황에서도, 바닥에 놓인(Grounded) 아이템의 프레임당 로직 비용이 0에 가깝도록 한다. SpriteRenderer 렌더링 비용은 이 목표에 포함하지 않고 따로 측정한다.
- 입력: 유저가 직접 제시한 요구("드롭아이템 설계"). 별도 기획명세 없음.

## 범위
**포함**
- Entities 패키지 도입
- DropItemType, ILootReceiver 계약, ECS 컴포넌트, LootDetectionSystem, AttractMovementSystem, DropItemManager
- 드롭 프리팹 13종(임시 스프라이트), 테스트 드라이버·테스트 씬, 기능·성능 검증
- 플레이어 측 연동 가이드 문서

**제외**
- 플레이어 코드 수정: 효과 분기와 경험치·골드·회복·폭탄 효과는 플레이어 담당 몫이며, 문서로 대체한다.
- Enemy 연동과 드롭 테이블: Spawn API까지만 제공한다.
- 정식 스프라이트, 이펙트, 사운드
- `ObjectPoolManager` 수정

## 구조 개요

```
[Enemy 등 외부] ── Spawn(type, pos) ──▶ DropItemManager (MonoSingleton, 관리 스레드 브리지)
                                          ├ ObjectPool.GetObject → 뷰 배치 (Grounded 동안 Transform 불변)
                                          └ Entity 생성 [DropItem, DropPosition, DropView, Grounded, (ExpGem)]
[ILootReceiver = Player] ◀─ Update: Position / LootRadius Pull → LootTarget 싱글톤 기록

SimulationSystemGroup (Burst, unmanaged)
  LootDetectionSystem   : 1/15초마다 Grounded 쿼리, distsq ≤ r² → ECB: -Grounded, +Attract
  AttractMovementSystem : 매 프레임 Attract 쿼리, 반발 → 추적, 도달 시 ECB: +Arrived
  (EndSimulation ECB 재생)

DropItemManager.LateUpdate
  1) Arrived 쿼리 → receiver.OnLoot(type), ObjectPool.ReturnObject(뷰), 엔티티 일괄 파괴
  2) Attract 쿼리 → TransformAccessArray 동기화 (IJobParallelForTransform, Burst)
```

**실행 순서**: `MonoBehaviour.Update` → SimulationSystemGroup(EndSimulation ECB 재생 포함) → `LateUpdate`. Entities 1.4.8의 `ScriptBehaviourUpdateOrder`가 SimulationSystemGroup을 PlayerLoop `Update` 단계 끝에 붙이는 것을 확인했다.

**상태 전이**

```
Grounded ──(감지: 거리 ≤ LootRadius | 자석: ExpGem 일괄)──▶ Attract(Init → Recoil → Chase)
         ──(거리 ≤ ArriveRadius)──▶ Arrived ──▶ OnLoot + 풀 반환 + 엔티티 파괴
```

## 작업 항목
| # | 작업 단위 | 산출물(클래스/파일 경로) | 의존 |
|---|---|---|---|
| 1 | Entities 패키지 추가(`com.unity.entities` 1.4.8) 후 컴파일 확인 | `Packages/manifest.json` | - |
| 2 | DropItemType enum | `Assets/Scripts/DropItem/DropItemType.cs` | - |
| 3 | ILootReceiver 인터페이스 | `Assets/Scripts/Inheritance/Interface/ILootReceiver.cs` | #2 |
| 4 | ECS 컴포넌트 정의 | `Assets/Scripts/DropItem/DropItemComponents.cs` | #1, #2 |
| 5 | LootDetectionSystem | `Assets/Scripts/DropItem/LootDetectionSystem.cs` | #4 |
| 6 | AttractMovementSystem | `Assets/Scripts/DropItem/AttractMovementSystem.cs` | #4 |
| 7 | DropItemManager (Spawn, 자석, 루팅 통지, 뷰 동기화, 싱글톤 관리) | `Assets/Scripts/Mangers/DropItemManager.cs` | #3, #4 |
| 8 | `GameManager.DropItem` 접근자 추가 | `Assets/Scripts/Mangers/MonoSingleton.cs` (GameManager 클래스에 1줄) | #7 |
| 9 | 드롭 프리팹 13종 (Unity CLI. SpriteRenderer만, 임시 스프라이트·색으로 구분) | `Assets/Prefabs/DropItem/{DropItemType}.prefab` | - |
| 10 | 테스트 드라이버: 더미 ILootReceiver(이동, 루팅거리 조절, 로그), 키 입력(New Input System `Keyboard.current`)으로 스폰·대량 스폰·자석 | `Assets/Scripts/Dev/DropItemTestDriver.cs` | #7, #8 |
| 11 | 테스트 씬 구성 (Unity CLI): DropItemManager(프리팹 13종 연결), ObjectPoolManager, 드라이버 | `Assets/Scenes/TestScenes/DropItemTestScene.unity` | #9, #10 |
| 12 | 기능·성능 검증 (아래 "검증 항목") | 본 문서 하단 "검증 결과"에 기록 | #5, #6, #11 |
| 13 | 플레이어 연동 가이드 (아래 "플레이어 측 연동 명세" 원문) | `Docs/Guide/DropItem_PlayerIntegration.md` | #3, #7 |

## 합의된 구현 결정

### 1. ECS 방식: Unity Entities, 렌더링은 풀링된 GameObject
- `com.unity.entities` **1.4.8**을 쓴다. 6000.3.24f1 에디터에 번들된 버전이다. Entities Graphics는 도입하지 않는다.
- 근거: Entities Graphics는 SpriteRenderer를 지원하지 않고, URP에서는 Forward+만 지원한다. 그래서 이 프로젝트의 2D Renderer에서는 엔티티를 직접 렌더링할 수 없다.
  - 렌더링은 기존 풀에서 꺼낸 GameObject(SpriteRenderer)가 맡는다.
  - 상태·위치·로직의 원본(Source of Truth)은 엔티티다.
- 뷰 Transform에 쓰는 대상은 Attract 상태 엔티티뿐이다. Grounded 아이템은 스폰할 때 한 번 배치한 뒤, 루팅이 시작될 때까지 Transform에 접근하지 않는다.

### 2. 플레이어 연동: Pull 방식 ILootReceiver
```csharp
public interface ILootReceiver
{
    Vector2 Position { get; }
    float LootRadius { get; }          // 런타임 변경 값. 매 프레임 Pull
    void OnLoot(DropItemType type);    // 도달 시 1회. 효과 분기는 구현 측
}
```
- 등록은 `GameManager.DropItem.RegisterReceiver(ILootReceiver)`, 해제는 `UnregisterReceiver(ILootReceiver)`로 한다.
  - 둘 다 리시버 참조만 바꾸므로, 매니저의 Awake보다 먼저 호출돼도 안전하다.
  - 리시버가 파괴되면(`(receiver as UnityEngine.Object) == null`) 매니저가 해제된 것으로 간주한다.
- 루팅거리의 원본은 플레이어 측 한 곳에만 둔다. 드롭 측은 값을 따로 보관하지 않고, 매 프레임 LootTarget 싱글톤에 기록만 한다.

### 3. 드롭 발생: Spawn API까지
- `DropItemManager.Spawn(DropItemType type, Vector2 position)`을 제공한다.
- 호출 측(Enemy 등)과 드롭 테이블은 범위 밖이다.

### 4. 매니저: 기존 체계를 따른다
- `DropItemManager : MonoSingleton<DropItemManager>`로 만들어 `Assets/Scripts/Mangers/`에 두고, `GameManager.DropItem` 접근자를 추가한다.
- 씬에 배치해서 쓴다. 프리팹 참조와 튜닝값을 SerializeField로 가지므로, MonoSingleton의 자동 생성 경로에 기대지 않는다.

### 5. 오브젝트 풀: 기존 ObjectPoolManager를 그대로 쓴다
- 생성은 `GameManager.ObjectPool.GetObject(prefab, pos, Quaternion.identity)`, 반환은 `ReturnObject(go)`로 한다.
- 드롭 프리팹에는 SpriteRenderer만 붙이고 MonoBehaviour는 붙이지 않는다.
  - IPoolable을 구현하지 않으므로 풀의 `GetComponent<IPoolable>()`는 null을 반환하고 그대로 통과한다.
  - 아이템별 Update 비용이 0이 된다.
- 뷰는 기존 풀의 `Instantiate(prefab)`에 의해 부모 없는 루트 오브젝트로 생성된다. 이 상태를 **유지한다**.
  - IJobParallelForTransform은 루트 계층 단위로 병렬 분배된다.
  - 뷰를 공통 부모 아래에 넣으면 단일 스레드로 실행된다.

### 6. DropItemType
```csharp
public enum DropItemType : byte
{
    ExpGem1, ExpGem2, ExpGem3, ExpGem4,
    Gold1, Gold2, Gold3, Gold4,
    LuckyBox, RewardBox, Potion, Magnet, Bomb
}
```
- `byte`로 두어 컴포넌트 크기를 줄인다.
- 경험치잼은 enum 앞쪽에 연속으로 배치한다. 그래서 스폰할 때 `type <= ExpGem4` 조건만으로 ExpGem 태그를 붙일 수 있다.
- 1~4 등급별 수치(경험치량, 골드량)는 플레이어 측 분기에서 정한다.

### 7. ECS 컴포넌트 (모두 unmanaged `IComponentData`)
| 컴포넌트 | 필드 | 용도 |
|---|---|---|
| `DropItem` | `DropItemType Type` | 아이템 식별 |
| `DropPosition` | `float2 Value` | 위치 원본. LocalTransform을 쓰지 않아 TransformSystemGroup 비용을 피한다 |
| `DropView` | `int Slot` | 매니저가 가진 뷰 Transform 슬롯의 인덱스 |
| `Grounded` | tag | 저주기 감지 대상 |
| `ExpGem` | tag | 자석 일괄 쿼리용 |
| `Attract` | `AttractPhase Phase, float2 Origin, float2 Dir, float Elapsed, float Speed` | 흡수 이동 상태 |
| `Arrived` | tag | 도달. 매니저 후처리 대상 |
| `LootTarget` (싱글톤) | `float2 Position, float Radius` | 매니저가 매 프레임 기록 |
| `DropItemConfig` (싱글톤) | `RecoilDistance, RecoilDuration, ChaseStartSpeed, ChaseAcceleration, ArriveRadius` | 매니저의 SerializeField 값으로 생성 |

- `AttractPhase : byte { Init = 0, Recoil, Chase }`로 정의한다.
  - `Attract`의 기본값(0)이 Init이므로 `AddComponent<Attract>`만으로 상태가 전이된다. 실제 초기화는 이동 시스템이 첫 틱에 수행한다.
  - 덕분에 감지 경로와 자석 경로가 같은 전이 방식을 공유한다.
- 아키타입
  - 경험치잼: `[DropItem, DropPosition, DropView, Grounded, ExpGem]`
  - 나머지: `[DropItem, DropPosition, DropView, Grounded]`
- Grounded → Attract 전이는 구조 변경(아키타입 이동)으로 처리한다.
  - Grounded 청크와 Attract 청크가 분리되므로, 매 프레임 도는 이동 쿼리는 Grounded 청크를 아예 순회하지 않는다.
  - IEnableableComponent 방식 대신 이 방식을 고른 이유가 이것이다.

### 8. LootDetectionSystem
`ISystem`, `[BurstCompile]`, SimulationSystemGroup에서 `[UpdateBefore(typeof(AttractMovementSystem))]`로 실행한다.
- `RequireForUpdate<LootTarget>()`, `RequireForUpdate<DropItemConfig>()`로 전투 씬 밖에서는 돌지 않게 한다.
- 시스템 필드에 경과 시간을 누적하고, `const float DetectInterval = 1f / 15f`가 지났을 때만 잡을 스케줄한다.
- 잡은 `IJobEntity`(`WithAll<Grounded>`)다. `math.distancesq(pos, target.Position) <= r * r`이면 EndSimulation ECB의 ParallelWriter로 `RemoveComponent<Grounded>`와 `AddComponent<Attract>`를 기록한다.
- 물리와 콜라이더는 쓰지 않는다. 거리 계산만으로 루팅한다.
- 루팅거리 `r`은 틱마다 LootTarget에서 읽으므로, 런타임 변경이 곧바로 반영된다.

### 9. AttractMovementSystem
`ISystem`, `[BurstCompile]`. 매 프레임 `IJobEntity.ScheduleParallel`로 실행한다.
- `RequireForUpdate<LootTarget>()`, `RequireForUpdate<DropItemConfig>()`를 건다. 리시버가 없으면 시스템이 멈추므로, 흡수 중이던 아이템은 그 자리에 정지한다.
- **Init**
  - `Origin = pos`, `Dir = math.normalizesafe(pos - player, 대체 방향)`로 정한 뒤 Recoil로 넘어간다.
  - 플레이어와 위치가 겹쳐 거리가 0이어도 NaN이 나지 않는다.
- **Recoil**
  - `Elapsed += dt`, `t = saturate(Elapsed / RecoilDuration)`, `pos = Origin + Dir * RecoilDistance * easeOut(t)`로 이동한다.
  - `t ≥ 1`이 되면 Chase로 넘어가며 `Speed = ChaseStartSpeed`로 설정한다.
- **Chase**
  - `Speed += ChaseAcceleration * dt`로 가속하며 플레이어의 **현재 위치**로 이동한다.
  - 이번 프레임 이동량이 남은 거리보다 크거나 같으면 도달로 처리해 오버슈트를 막는다.
  - `distsq ≤ ArriveRadius²`가 되면 ECB에 `AddComponent<Arrived>`를 기록한다.
- 반발 방향은 흡수를 시작한 시점의 플레이어 반대 방향으로 고정한다. 추적 목표는 매 프레임 갱신한다.

### 10. DropItemManager (관리 스레드 브리지)
- **SerializeField**
  - `GameObject[] prefabs`: DropItemType 순서로 13개
  - 튜닝값: `recoilDistance`, `recoilDuration`, `chaseStartSpeed`, `chaseAcceleration`, `arriveRadius`
- **API**
  - `Spawn(DropItemType, Vector2)`: 풀에서 뷰를 꺼내고, 슬롯을 할당하고, 엔티티를 생성한다.
  - `AttractAllExpGems()`: `[Grounded, ExpGem]` 쿼리에 `AddComponent<Attract>(query)`를 적용한 뒤 `RemoveComponent<Grounded>(query)`를 적용한다. 둘 다 청크 단위 일괄 연산이다.
  - `RegisterReceiver` / `UnregisterReceiver`: 리시버 참조만 저장하거나 비운다. LootTarget 싱글톤은 `Update`에서 다룬다.
- **생명주기**
  - `Awake`: EntityManager를 캐시하고, 아키타입 2종과 쿼리(Arrived, Attract, Grounded+ExpGem)를 만든다. DropItemConfig 싱글톤과 TransformAccessArray도 생성한다.
  - `Update`
    - 파괴된 리시버는 해제로 처리한다.
    - 리시버가 있으면 LootTarget 싱글톤에 기록한다(없으면 생성). 리시버가 없으면 LootTarget을 파괴한다.
    - SerializeField 튜닝값을 DropItemConfig에 기록한다. 플레이 중 인스펙터에서 바꾼 값이 곧바로 반영된다.
  - `LateUpdate`: Arrived 처리 → 뷰 동기화 순서로 실행한다.
  - `OnDestroy`: 모든 DropItem 엔티티와 싱글톤을 파괴하고 TransformAccessArray를 Dispose한다.
    - World는 씬이 바뀌어도 남아 있으므로 이 정리는 필수다.
    - World가 이미 해제된 경우를 가드한다.
- **뷰 슬롯**: `List<Transform> views`와 `Stack<int> freeSlots`로 관리한다.
- **Arrived 처리**
  1. 쿼리 결과를 `ToComponentDataArray<DropItem/DropView>`로 복사한다.
  2. 각 항목에 `receiver?.OnLoot(type)`을 호출하고, `ObjectPool.ReturnObject`로 뷰를 반환하고, 슬롯을 해제한다.
  3. `EntityManager.DestroyEntity(arrivedQuery)`로 엔티티를 한 번에 파괴한다.
  - 복사본을 순회하므로 OnLoot 안에서 Spawn이나 AttractAllExpGems를 호출해도 안전하다.
- **뷰 동기화**
  - `EntityManager.GetComponentOrderVersion<Attract>()`가 바뀌었을 때만 TransformAccessArray를 재구성한다(DropView.Slot → views[slot]).
    - 쿼리의 `GetCombinedComponentOrderVersion`은 쓰지 않는다. Entity 타입과 DropPosition·DropView의 버전까지 합산하므로, Spawn이나 감지처럼 Attract와 무관한 구조 변경에도 값이 바뀐다.
    - 자석의 청크 단위 AddComponent도 Attract 버전을 올린다(Entities 1.4.8 `ChunkDataUtility.cs`에서 확인).
  - `ToComponentDataArray<DropPosition>(TempJob)`으로 위치를 가져와 private nested `ViewSyncJob : IJobParallelForTransform`(`[BurstCompile]`)을 실행하고, Complete한 뒤 Dispose한다.
  - 재구성과 동기화 사이에 구조 변경이 없어야 쿼리 순서가 일치한다. 그래서 LateUpdate 안의 실행 순서를 고정한다.

### 11. 자석 / 폭탄 흐름
- **자석**
  - Magnet 아이템도 일반 루팅거리로 루팅된다.
  - 도달하면 `OnLoot(Magnet)`이 호출되고, 플레이어 측이 `GameManager.DropItem.AttractAllExpGems()`를 호출한다. "효과는 플레이어 측에서 분기한다"는 원칙을 지킨다.
  - 자석으로 끌려오는 잼도 반발 → 추적의 같은 경로로 움직인다.
- **폭탄**: `OnLoot(Bomb)`을 받은 플레이어 측이 화면 내 몬스터 공격을 처리한다. 몬스터 측 API가 필요하며 범위 밖이다.

## 의존 순서 / 병렬 가능 그룹
- 직렬: #1 → #4 → #7 → #8 → #10 → #11 → #12
- 병렬
  - #2와 #9는 바로 시작할 수 있다. #3은 #2가 끝난 뒤 진행한다.
  - #5, #6, #7은 #4가 끝난 뒤 동시에 진행할 수 있다(#7은 #3도 필요).
  - #13은 #7의 API가 확정된 뒤 언제든 진행할 수 있다.

## 검증 항목 (#12)
**기능**
- Grounded 아이템은 루팅거리 밖에서 움직이지 않는다.
- 루팅거리에 들어오면 반발 → 추적 → 도달 순으로 움직이고, `OnLoot`이 정확히 1회 호출된다.
- 루팅거리를 런타임에 바꾸면 곧바로 반영된다.
- 자석은 Grounded 경험치잼만 전부 끌어오고, 경험치잼이 아닌 아이템은 그대로 둔다.
- 도달 후 뷰가 풀에 반환되고, 재스폰 때 재사용된다(Instantiate가 다시 일어나지 않음).
- 씬을 전환한 뒤 남는 드롭 엔티티가 0개다(Entities Hierarchy 창으로 확인).
- 플레이어 위치에 겹쳐 스폰해도 NaN이 생기지 않는다.
- 플레이어 `OnEnable`이 DropItemManager의 Awake보다 먼저 실행돼도 등록된다.
- 씬 전환·플레이 종료 때 DropItemManager가 새로 생성되지 않는다(Console에 "Some objects were not cleaned up" 경고 없음).
- 플레이 중 인스펙터에서 튜닝값을 바꾸면 곧바로 반영된다.

**성능** (Profiler, Grounded 5000개 기준. 수치를 기록한다)
- Grounded만 있을 때 드롭 관련 메인 스레드 비용이 얼마인지, 감지 잡이 15Hz로만 실행되는지(Timeline) 확인한다.
- 정상 상태에서 GC Alloc이 0이다.
- 자석을 발동한 프레임의 스파이크를 측정하고, 흡수 중 ViewSyncJob이 워커 스레드에 병렬로 분배되는지 확인한다.
- 각 잡이 Burst로 컴파일됐는지 확인한다(Timeline의 잡 이름에 Burst 표기).
- 드롭이 0개일 때 Entities 기본 World의 시스템 비용을 기준값으로 기록한다.
- 렌더링 비용(Batches, SetPass Calls)을 로직 비용과 따로 기록한다.
- 흡수 중인 아이템이 없으면 Spawn이 이어져도 TransformAccessArray 재구성이 일어나지 않는다.

## 플레이어 측 연동 명세 (문서화 대체, #13 원문)
- **구현 대상**: 플레이어 루트의 MonoBehaviour 하나가 `ILootReceiver`를 구현한다.
- **등록**
  - `OnEnable`에서 `GameManager.DropItem.RegisterReceiver(this)`를 호출한다. 매니저의 Awake보다 먼저 실행돼도 안전하다.
  - `OnDisable`·`OnDestroy`에서는 `GameManager.DropItem`을 호출하지 않는다. 씬 언로드나 종료 중에 매니저가 먼저 파괴됐으면 MonoSingleton이 매니저를 새로 생성한다. 파괴된 리시버는 매니저가 해제로 처리한다.
  - 사망 시 루팅을 멈추려면 `PlayerHealth.OnDied`에서 `UnregisterReceiver(this)`를 호출한다. 이때 흡수 중이던 아이템은 그 자리에 멈춘다.
- **Position**: `transform.position`. Rigidbody2D로 이동한 결과가 반영된 값이다.
- **LootRadius**: 플레이어 스탯 원본을 그대로 반환한다. 패시브 등으로 값이 바뀌어도 따로 통지할 필요가 없다.
- **OnLoot 분기**

| DropItemType | 플레이어 측 처리 |
|---|---|
| ExpGem1~4 | 경험치 획득 (등급별 수치는 플레이어 측 또는 데이터) |
| Gold1~4 | 골드 획득 |
| LuckyBox | 행운상자 효과 |
| RewardBox | 보상상자 효과 |
| Potion | 회복 (`PlayerHealth.Heal`) |
| Magnet | `GameManager.DropItem.AttractAllExpGems()` 호출 |
| Bomb | 화면 내 몬스터에게 강력한 공격 (몬스터 측 API 필요) |

- **호출 시점**: `OnLoot`은 `DropItemManager.LateUpdate` 안에서 호출된다. 그 안에서 `Spawn`이나 `AttractAllExpGems`를 호출해도 안전하다.

## 미해결 / 실행 시 확인 필요
- **TransformAccessArray 재구성**: GC 할당이 없는 재구성 경로를 확인한다.
- **리시버 해제 시 흡수 중 아이템**: 지금은 그 자리에 멈춘다. 원작 동작을 확인하고 필요하면 바꾼다.
- **튜닝값**: 반발 거리·시간, 추적 초기속도·가속, 도달 반경의 초기값이 정해지지 않았다. 테스트 씬에서 원작 체감에 맞춰 조정한다.
- **스프라이트**: 정식 스프라이트가 없어 임시 스프라이트를 쓴다. 정식 리소스가 들어오면 하나의 SpriteAtlas로 묶어야 배칭이 유지된다.
- **Sorting Layer/Order**: 드롭 뷰가 플레이어·몬스터보다 아래에 그려져야 한다. 기존 레이어 규칙을 확인한다.
- **드롭 수 상한·병합**: 개수 상한이나 오래된 잼을 합치는 정책이 정해지지 않았다. 원작 동작을 확인해야 한다.
- **Spawn 비용**: 프레임당 Spawn이 수백 회에 이르면 EntityManager로 하나씩 생성하는 비용이 커질 수 있다. 측정해 보고 필요하면 요청을 모아 일괄 생성하는 방식으로 바꾼다.
- **풀 선생성 부재**: 기존 풀에는 선생성(prewarm)이 없어 첫 대량 드롭 때 Instantiate 스파이크가 생길 수 있다. 측정 후 논의한다(풀 수정은 범위 밖).
- **폭탄 효과**: 몬스터 측 API가 없다.
- **Enemy 연동**: Enemy.Die 연동과 드롭 테이블은 범위 밖이며 후속 작업이다.

## 검증 결과
환경: 에디터 Play Mode(`DropItemTestScene`), 창이 포커스되지 않은 상태로 `Application.runInBackground = true`를 두고 약 30fps(33ms)로 측정했다. 조작은 Unity CLI `eval`로 했고, 수치는 `ProfilerRecorder`로 수집했다. 잡 워커 수는 19다.

**기능**
| 항목 | 결과 |
|---|---|
| 루팅거리 밖 Grounded 정지 | 통과. 13종 스폰 후 Grounded 13, Attract 0이 유지됐다 |
| 반발 → 추적 → 도달, OnLoot 1회 | 통과. 반경 2 안의 7개가 각각 1회씩 OnLoot됐다(total 1~7) |
| 루팅거리 런타임 변경 | 통과. 2 → 2.5로 바꾸자 해당 범위의 2개(Potion, ExpGem3)가 곧바로 루팅됐다 |
| 자석은 Grounded 경험치잼만 | 통과. 잼 100 + 골드 100 + 기타에서 잼만 흡수되고 골드 100, Magnet, Bomb은 그대로 남았다 |
| 풀 반환·재사용 | 통과. 재스폰 100개 뒤에도 뷰 총수가 207로 그대로였다(Instantiate 없음) |
| 씬 전환 후 잔존 엔티티 | 통과. SampleScene 로드 후 DropItem, DropItemConfig, LootTarget이 모두 0이고 DropItemManager도 0이다(Hierarchy 창 대신 쿼리 카운트로 확인) |
| 플레이어 위치 겹침 스폰 | 통과. NaN 없이 루팅됐다 |
| 매니저 Awake 전 등록 | 코드 경로로만 확인했다. `RegisterReceiver`는 필드만 대입하고, `Instance`는 `FindFirstObjectByType`로 씬의 매니저를 찾는다. 실행 순서를 강제로 바꾼 테스트는 하지 않았다 |
| 종료·전환 시 매니저 재생성 | 통과. 플레이 종료와 씬 전환 모두 "Some objects were not cleaned up" 경고가 없었다 |
| 튜닝값 런타임 반영 | 통과. chaseStartSpeed, chaseAcceleration을 변경한 값이 곧바로 이동 속도에 반영됐다 |
| 리시버 해제·파괴 | 해제 시 LootTarget이 파괴되고 흡수 중인 50개가 제자리에 정지했다. 재등록하면 재개된다. 리시버 GameObject를 파괴하면 LootTarget이 0이 된다 |

**성능** (단위 ms, 평균 / 최대)
| 항목 | 드롭 0개 | Grounded 5000 | 자석(잼 2500 흡수) |
|---|---|---|---|
| LootDetectionSystem (메인) | 0.002 / 0.029 | 0.003 / 0.016 | 0.004 / 0.017 |
| LootDetectionJob (Burst, 워커) | 0 | 0.039 / 0.256 | 0.031 / 0.125 |
| AttractMovementSystem (메인) | 0.003 / 0.017 | 0.003 / 0.029 | 0.019 / 0.123 |
| AttractMovementJob (Burst) | 0 | 0 | 0.029 / 0.198 |
| ViewSyncJob (Burst, 전체 스레드 합) | 0 | 0 | 0.117 / 0.917 |
| ViewSyncJob 중 메인 스레드 몫 | - | - | 0.045 / 0.247 |
| LateUpdate 전체 | 0.051 / 0.24 | 0.050 / 0.17 | 0.58 / 3.9 |
| GC Allocated In Frame (중앙값) | 14,793 B | 14,793 B | 14,793 B |
| Batches / SetPass | 2 / 2 | 약 1,568 / 10 | 약 1,367 / 10 |

- **Grounded 비용**: 5000개일 때도 드롭 0개와 메인 스레드 비용 차이가 없다(시스템 메인 비용 0.003ms 수준). 감지 잡은 151/301 프레임에서만 실행됐다(30fps에서 15Hz).
- **GC**: 에디터 자체 할당(eval 서버, 인스펙터 등) 때문에 매 프레임 약 14.8KB가 잡힌다. 드롭 0개, Grounded 5000, 흡수 중 모두 중앙값이 같아서 정상 상태의 드롭 추가 할당은 0으로 판단했다. 최대값 스파이크(~1.9MB)는 Grounded 전용 구간에서도 발생해 에디터 쪽으로 보이지만 에디터 안에서는 분리할 수 없다. 개발 빌드 Profiler로 재확인이 필요하다.
- **자석 스파이크**: `AttractAllExpGems()` 호출 자체는 0.11~0.12ms다(2500개, 청크 단위 구조 변경). 흡수 구간의 최대 비용은 LateUpdate 3.9ms로, 한 프레임에 수백 개가 도달해 `OnLoot` + `ReturnObject`(SetActive false)가 몰리는 프레임이다. 테스트 드라이버의 OnLoot 로그를 켜면 11ms로 오른다.
- **ViewSyncJob 병렬 분배**: 전체 스레드 합(0.117) 대비 메인 스레드 몫이 0.045라서 나머지는 워커에서 실행됐다. Timeline에서 눈으로 확인하지는 않았다.
- **Burst**: 세 잡 모두 `(Burst)` 마커에서만 샘플이 잡히고 관리 코드 마커는 0이다. 시스템 OnUpdate도 `Burst Jobs/Default World ...System` 마커로 등록돼 있다.
- **TransformAccessArray 재구성**: 흡수 중인 아이템이 없을 때 5000개를 Spawn해도 `GetComponentOrderVersion<Attract>()`가 0 → 0으로 그대로여서 재구성이 일어나지 않는다. 재구성은 `RemoveAtSwapBack` + `Add`로 해서 관리 배열을 할당하지 않는다.
- **Spawn 비용**: 5000개 연속 Spawn에 95ms가 걸렸다(개당 약 19µs, 첫 생성이라 Instantiate 포함).
- **렌더링**: 임시 스프라이트가 텍스처 4종에 색까지 달라서 5000개에 Batches 약 1,568이 나온다. 로직 비용과 별개이며 SpriteAtlas 도입 후 다시 측정해야 한다.

**구현 중 수정**
- 감지 주기: `elapsed = 0`으로 리셋하면 30fps에서 나머지가 버려져 10Hz로 떨어졌다(91/239 프레임). `elapsed %= DetectInterval`로 바꿔 15Hz를 유지하게 했다. 히치가 생겨도 몰아서 실행하지 않는다.
- Sorting: 드롭 프리팹은 `sortingOrder = -1`로 두었다(Player 프리팹은 Default/0). 레이어 규칙이 정해지면 조정한다.
