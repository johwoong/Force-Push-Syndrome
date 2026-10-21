# 드롭아이템 플레이어 연동 가이드

플레이어가 드롭아이템을 루팅하려면 `ILootReceiver`를 구현해 `DropItemManager`에 등록한다.

```csharp
public interface ILootReceiver
{
    Vector2 Position { get; }
    float LootRadius { get; }          // 런타임 변경 값. 매니저가 매 프레임 Pull
    void OnLoot(DropItemType type);    // 도달 시 1회. 효과 분기는 구현 측
}
```

## 구현 대상
- 플레이어 루트의 MonoBehaviour 하나가 `ILootReceiver`를 구현한다.

## 등록
- `OnEnable`에서 `GameManager.DropItem.RegisterReceiver(this)`를 호출한다. 매니저의 Awake보다 먼저 실행돼도 안전하다.
- `OnDisable`·`OnDestroy`에서는 `GameManager.DropItem`을 호출하지 않는다. 씬 언로드나 종료 중에 매니저가 먼저 파괴됐으면 MonoSingleton이 매니저를 새로 생성한다. 파괴된 리시버는 매니저가 해제로 처리한다.
- 사망 시 루팅을 멈추려면 `PlayerHealth.OnDied`에서 `GameManager.DropItem.UnregisterReceiver(this)`를 호출한다. 이때 흡수 중이던 아이템은 그 자리에 멈춘다.

## 프로퍼티
- **Position**: `transform.position`. Rigidbody2D로 이동한 결과가 반영된 값이다.
- **LootRadius**: 플레이어 스탯 원본을 그대로 반환한다. 패시브 등으로 값이 바뀌어도 따로 통지할 필요가 없다.

## OnLoot 분기

| DropItemType | 플레이어 측 처리 |
|---|---|
| ExpGem1~4 | 경험치 획득 (등급별 수치는 플레이어 측 또는 데이터) |
| Gold1~4 | 골드 획득 |
| LuckyBox | 행운상자 효과 |
| RewardBox | 보상상자 효과 |
| Potion | 회복 (`PlayerHealth.Heal`) |
| Magnet | `GameManager.DropItem.AttractAllExpGems()` 호출 |
| Bomb | 화면 내 몬스터에게 강력한 공격 (몬스터 측 API 필요) |

## 호출 시점
- `OnLoot`은 `DropItemManager.LateUpdate` 안에서 호출된다. 그 안에서 `Spawn`이나 `AttractAllExpGems`를 호출해도 안전하다.

## 예시

```csharp
public class PlayerLootReceiver : MonoBehaviour, ILootReceiver
{
    [SerializeField] private float lootRadius = 2f;

    public Vector2 Position => transform.position;
    public float LootRadius => lootRadius;

    private void OnEnable()
    {
        GameManager.DropItem.RegisterReceiver(this);
    }

    public void OnLoot(DropItemType type)
    {
        switch (type)
        {
            case DropItemType.Magnet:
                GameManager.DropItem.AttractAllExpGems();
                break;
            // ExpGem1~4, Gold1~4, LuckyBox, RewardBox, Potion, Bomb: 플레이어 측 처리
        }
    }
}
```
