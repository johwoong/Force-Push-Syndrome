# 플레이어 데이터·권위 이슈

제기된 두 문제(장비 데이터 모델 분리, 로컬·서버 권위 혼재)를 코드로 확인한 결과다. 기준은 2026-10-09, 클라이언트 `COU_Client`, 서버 `COU_Server`(Feat/UI 브랜치)다. 이슈 1~2는 장비 데이터 모델 분리, 3~8은 로컬·서버 권위 혼재에 해당한다.

## 1. 장비 데이터 모델이 서버와 다르다

### 형식
| 형식 | 위치 | ID | 장착 표시 | 용도 |
|---|---|---|---|---|
| `InventoryData` | `SaveData.cs:159` | int | 프로필의 `equipped{Slot}InventoryId` 6칸(`SaveData.cs:138`) | `PlayerSaveData.inventoryList`, 전투 보상 |
| `EquipmentResponse` | `ServerApiData.cs:83` | long | `isEquipped` | 서버 응답 |
| `OwnedItem` | `OwnedItem.cs:6` | GUID `instanceId`. 서버 ID는 `PlayerInventory.inventoryIds`에 따로 둔다 | `isEquipped` | 메모리, UI |
| `EquipmentSaveData` | `SaveData.cs:195` | string `instanceId` | `isEquipped` | 쓰는 곳 없음 |

서버 `PlayerProfile`에는 장착 칸 컬럼이 없다. 장착 여부는 `Equipment.equipped`에 있다.

### 변환 흐름
- 로그인: `GET /api/inventory` 응답을 `ServerLoadManager.ApplyInventory`(:118)가 `InventoryData`와 장착 칸으로 바꾼다. long을 int로 형변환한다.
- 메인 씬: `PlayerInventory.Load`(:221)가 `InventoryData`와 장착 칸을 `OwnedItem`으로 바꾼다.
- 서버 응답을 반영한 뒤: `PlayerInventory.Save`(:196)가 `OwnedItem`을 다시 `InventoryData`와 장착 칸으로 되돌린다. 전투 씬의 `PlayerStats`가 이 값을 읽는다.
- 전투 씬: `PlayerStats.GetEquippedFromSaveData`(:95)가 장착 칸을 `OwnedItem`으로 바꾼다.
- 전투 결과: 서버의 `List<EquipmentResponse>`를 `List<InventoryData>`로 받아(`ServerApiData.cs:68`) `inventoryList`에 넣는다(`BattleManager.cs:317`). 필드 이름이 같아서 역직렬화는 되지만 `isEquipped`는 버려진다.

### 기타
- 획득 시각: 서버 `Equipment`에는 `acquiredAt`이 있지만 응답에 넣지 않는다. 클라이언트는 이 값을 `default`로 채우고(`ServerLoadManager.cs:133`) `acquiredAts`에 넣어 옮기기만 한다. 읽는 곳은 없다.
- `ServerLoadManager.cs:117`의 TODO는 `InventoryData`의 ID를 long으로, 장착 표시를 장비 필드로 바꾸는 것을 전제로 한다.

## 2. 장착 칸 처리와 복원 로직이 중복돼 있다
- 슬롯 switch가 3개 있다: `PlayerInventory.GetEquippedInventoryId`(:311), `PlayerInventory.SetEquippedInventoryId`(:322, private), `ServerLoadManager.SetEquippedSlot`(:155).
- 복원 로직이 `PlayerInventory.Load`와 `PlayerStats.GetEquippedFromSaveData`에 두 벌 있다. 두 사본은 이미 동작이 다르다.
  - `Load`는 아이템 데이터에 없는 itemId를 건너뛴다. `PlayerStats`는 이를 검사하지 않아서 `OwnedItem` 생성자에서 `Data`가 null이 된다.
  - inventoryId가 겹치면 `Load`는 덮어쓰고, `PlayerStats`는 `ToDictionary`(:100)에서 예외를 던진다.
- `PlayerDataManager.SetPlayerDataFromLocal`(:19)과 `SetPlayerDataFromServer`(:37)는 본문이 같다.
- 제기 내용과 다른 점: 슬롯 switch는 2개가 아니라 3개다(`GetEquippedInventoryId` 포함).

## 3. 상점이 서버 구매 API를 쓰지 않고 로컬에서 지급한다
| 카드 | 클라이언트 동작 | 서버 상품(`Shop.json`) |
|---|---|---|
| `ShopGemCard` | `AddGem(gemAmount)`. 비용 없이 보석을 늘린다 | Gem 50021~50026 (priceGem 0) |
| `ShopGoldCard` | `TrySpendGem` 뒤 `AddGold` | Gold 50011~50013 |
| `ShopSupplyBoxCard` | `TrySpendGem` 뒤 클라이언트에서 무작위로 장비를 골라 `AddItem` | RandomItem 50001~50002 |

- 서버의 `POST /api/shop/purchase`(`ShopController.java:29`)는 productId를 받아 젬 차감, 지급, 장비 생성을 한 트랜잭션에서 처리한다. 클라이언트에는 `/api/shop`을 호출하는 코드가 없다.
- 카드의 가격과 지급량(gemCost, goldAmount, minGrade, maxGrade)은 인스펙터 값이다. 카드에 productId 필드가 없고, 클라이언트는 Shop 테이블을 읽지 않는다(`StaticDataClient.md` 2.2).
- `PlayerInventory`의 `AddItem`, `AddGem`, `AddGold`, `TrySpendGem`은 상점 카드와 `Dev/InventoryTestDriver`가 호출한다. 지금도 실행되는 로컬 권위 경로다.
- `Save()`는 메모리에만 쓴다. 로컬 저장 호출은 주석 처리돼 있다(`PlayerInventory.cs:217`). 구매 결과가 사라지는 시점은 다음과 같다.
  - 재화: 다음 서버 응답이 currency를 덮어쓸 때 사라진다. 전투 입장(`BattleTab.cs:60`), 전투 결과와 세이브 재수신(`BattleManager.cs:391`), 장비 레벨업(`PlayerInventory.cs:363`, 골드만)이 여기에 해당한다.
  - 장비: 재로그인하면 `ApplyInventory`가 서버 목록으로 다시 만들면서 사라진다. 그 전까지는 메모리에 남는다.
- 제기 내용과 다른 점: "다음 서버 응답이 오면 구매가 사라진다"는 재화에만 해당한다. 상점 장비는 재로그인 전까지 남는다.

## 4. 상점 장비의 inventoryId가 서버와 맞지 않는다
- `PlayerInventory.CreateNewItem`(:297)이 보유 장비의 최대값+1을 준다. 서버의 Equipment ID는 모든 플레이어가 함께 쓰는 IDENTITY다.
- 그 ID의 행이 없으면 `INVENTORY_NOT_FOUND`, 다른 플레이어의 행이면 `NOT_OWNED_INVENTORY`가 나서 장착·레벨업·합성이 실패한다(`InventoryService.getOwned`).
- 그 사이에 다른 장비 행이 생기지 않았으면 서버는 다음 보상상자 장비에 같은 ID를 준다. 그러면 `inventoryList`에 같은 inventoryId가 두 개 생긴다(코드상 추론이다).
  - 다음 전투에 입장하면 `PlayerStats`의 `ToDictionary`가 예외를 던져 최종 스탯을 계산하지 못한다.
  - 그 ID로 장착을 요청하면 서버는 보상 장비를 장착하고, 클라이언트는 `FindByInventoryId`가 먼저 찾은 장비(상점 장비)에 반영한다.
- 제기 내용과 다른 점: 상점 장비는 서버 요청이 실패하는 것 말고도 서버 ID와 겹칠 수 있다.

## 5. 쓰지 않는 로컬 로그인·저장 코드가 남아 있고 씬에서 실행된다
- 다음 코드는 서로끼리만 호출한다: `LocalLoginManager`, `LocalSaveLoadManager`, `AccountSaveData`, `LocalAccountData`, `PlayerSaveData.CreateDefault`, `SetPlayerDataFromLocal`, 그리고 `SaveLoadHelper`의 계정·플레이어 메서드(`SaveAccounts`, `LoadAccounts`, `SavePlayer`, `PlayerFileExists`, `LoadPlayer`). `GameManager.LocalLogin`/`LocalSaveLoad` 접근자는 `PlayerInventory.cs:217`의 주석에서만 쓰인다.
- 다만 두 매니저는 LogInScene의 `DontDestroyManagers/LocalSaveLoadManager` 오브젝트에 붙어 켜져 있다(isDontDestroy true, Unity CLI로 확인했다). `LocalSaveLoadManager.OnApplicationPause`(:87)와 `OnApplicationQuit`(:96)는 서버에서 받은 `PlayerSaveData`를 `persistentDataPath/Players/{accountLoginId}.json`에 쓴다. 이 파일을 읽는 곳은 없다.
- `SaveLoadHelper`의 정적 데이터 저장 메서드는 `JsonDataManager`가 쓴다. 이 파일 전체가 로컬 전용인 것은 아니다.
- `PlayerInventory` 메서드

| 메서드 | 호출처 |
|---|---|
| `Equip`, `Unequip`, `TryLevelUp`, `BatchLevelUp`, `TrySynthesize`, `BatchSynthesize` | 주석(`EquipDetailPopUp.cs:118~143`, `SynthesisWindow.cs:139~148`)과 이 메서드들 사이의 호출뿐이다 |
| `ClearSave`, `ResetCurrency` | ContextMenu |

- 쓰는 곳이 없는 코드: `EquipmentSaveData`(`SaveData.cs:195`), `GameConstants.Server.PLAYER_SAVE_API`(:47).
- 로컬 매니저를 지우려면 LogInScene의 컴포넌트도 함께 제거해야 한다. 그대로 두면 Missing Script가 남는다.
- 제기 내용과 다른 점: 로컬 매니저는 코드에서 참조하는 곳은 없지만, 씬에 배치돼 있어서 앱 종료나 백그라운드 전환 때 파일을 쓴다.

## 6. 클라이언트와 서버의 최종 스탯 계산이 다르다
| 항목 | 클라이언트 `PlayerStats` | 서버 `PlayerFinalStatService` |
|---|---|---|
| 장비 | `round(bonus × (1 + statGrowthPerLevel × (level − 1)) × gradeStatMultiplier[grade])` (`OwnedItem.cs:25`) | attackBonus, hpBonus 원값 (`statOf`, :69) |
| 진화 | 반영하지 않는다(playerStat을 읽지 않음) | 레벨 × `EvolutionDummyData` 상승량 |
| 보너스% | 0 | 0 (`BONUS_PERCENT`) |
| 방어력, 포션 회복 | 없음 | 있음 |

- 전투에서는 클라이언트 값만 쓴다. `/api/players/me/stats`는 `PlayerDebug` F1(`PlayerStatsComparison` 로그)과 `Dev/ApiClientTestDriver`에서만 호출한다.
- 진화 업그레이드(골드 차감)도 `PlayerDebug` F2에서만 호출한다. 서버에서 진화 레벨이 올라도 클라이언트의 전투 스탯은 바뀌지 않는다.
- 제기 내용과 다른 점:
  - 최종 스탯은 레벨·등급 배율 외에 진화 항목도 다르다.
  - `/me/stats`는 `PlayerDebug` 외에 `Dev/ApiClientTestDriver`도 호출한다.

## 7. 서버가 ItemConst 값을 상수로 갖고 있다
- 서버는 ItemConst를 읽지 않는다. `InventoryService`가 MAX_LEVEL 10, LEVEL_UP_BASE_COST 1000, SYNTHESIS_MATERIAL_COUNT 2를 상수로 갖고 있다(:29~31). 지금은 `ItemConst.json` 값과 같지만, 시트를 바꾸면 클라이언트(`ItemLevelConfig`)와 값이 달라진다.

## 8. 서버 PUT /api/players/me/save가 재화·레벨을 검증하지 않는다
- `PlayerService.save`(:79)는 닉네임 중복과 스테이지 해금만 검사한다. accountLevel, accountExp, 재화(골드·보석·스태미나·갱신 시각), 진화 레벨(공격·체력·방어)은 요청 값을 그대로 저장한다.
- 인증된 플레이어라면 자기 재화와 레벨을 원하는 값으로 바꿀 수 있다. 서버 `Docs/ETC/DesignDecisions.md:256`에 적혀 있지만 아직 고쳐지지 않았다.
- 클라이언트는 이 경로를 호출하지 않는다(`AccountApi`는 GET만 쓴다).

## 주의
- 서버 `Docs/ETC/BackendIntegration_DevAkasha.md` 108줄에 따르면 다음은 다른 작성자의 코드다: 로그인·저장 계층(`LocalLoginManager`, `LocalSaveLoadManager`, `SaveLoadHelper`, `PlayerDataManager`, `PlayerSaveData`), `PlayerInventory`의 장착·강화·합성·재화 메서드, 상점 카드.
- `PUT /api/players/me/save`는 jyj8943이 작성했다(`DesignDecisions.md:256`).
