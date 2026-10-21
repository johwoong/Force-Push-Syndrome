# 불필요한 코드

참조가 없거나 대체된 코드를 모은다. 기준은 2026-10-12, `feat/BattleSystem` 브랜치(`390f561`)다.

검사 방법은 다음과 같다.
- `Assets/Scripts`, `Assets/Editor`, `Assets/Animations`의 C# 184개 파일에서 타입·멤버 이름을 텍스트로 검색했다.
- 씬·프리팹·에셋 파일에서 스크립트 GUID와 `m_MethodName`(UnityEvent 연결)을 대조했다.
- 후보는 하나씩 열어 확인했다.

## 1. 파일 전체가 쓰이지 않는다
| 파일 | 내용 |
|---|---|
| `Assets/Animations/ExampleScript.cs` | 빈 클래스다 |
| `Assets/Scripts/Utils/Utils.cs` | `GetAPIData`, `FromJson`, `Wrapper`를 쓰는 곳이 없다 |
| `Assets/Scripts/Dev/MainTestDriver.cs` | 108줄 전체가 주석이다. `devUI_Main.unity`에 붙어 있어 그 씬에서는 스크립트가 없는 컴포넌트(Missing Script)로 남는다 |
| `Assets/Scripts/Player/Dummy/DummyEquipment.cs` | 쓰는 곳이 없다. 주석에 "임시 코드"라고 적혀 있다 |
| `Assets/Scripts/Player/API/PlayerServerStats.cs` | 쓰는 곳이 없다 |
| `Assets/Scripts/Skills/SkillType/AoE/AoeSkillBase.cs` | 상속하는 스킬이 없다. 이 클래스만 쓰는 `AoEData`(`SkillObjectData.cs:40`)도 함께 쓰이지 않는다 |
| `Assets/Scripts/Dev/ApiClientTestDriver.cs` | 어떤 씬·프리팹에도 붙어 있지 않다. ContextMenu로만 실행하는 도구라 직접 붙여 쓰는 용도일 수 있다 |

## 2. 로컬 저장·로그인 시스템이 남아 있다
서버로 전환한 뒤 쓰이지 않는다. `SaveData.cs` 주석에 "서버 전환 시 삭제"라고 적혀 있다.
- `LocalLoginManager.cs`, `LocalSaveLoadManager.cs`
  - 씬에 배치된 곳이 없다.
  - 이 둘을 가리키는 것은 `GameManager.LocalSaveLoad`, `LocalLogin`(`MonoSingleton.cs:17-20`, `[Obsolete]`)뿐이다. 이 두 속성은 쓰이지 않는 `LocalLoginManager` 안(:154) 말고는 쓰는 곳이 없다.
- `SaveData.cs`
  - `AccountSaveData`(:16), `LocalAccountData`(:27)
  - `PlayerSaveData.CreateDefault`(:73), `GetFirstStageId`(:111)
- `SaveLoadHelper`의 `SaveAccounts`, `LoadAccounts`, `SavePlayer`, `PlayerFileExists`, `LoadPlayer`. 정적 데이터 메서드 3개는 `JsonDataManager`가 쓴다.
- `PlayerDataManager.SetPlayerDataFromLocal`(:19). 본문이 `SetPlayerDataFromServer`와 같다.
- `GameConstants.Paths`의 `ACCOUNT_SAVE_PATH`, `PLAYER_DIRECTORY`(:20-21). `SaveLoadHelper`의 계정·플레이어 경로에만 쓰인다.

## 3. 로컬 인벤토리 로직이 남아 있다
장착·레벨업·합성은 서버 응답을 반영하는 `Apply*` 메서드로 바뀌었다.
- `PlayerInventory`의 다음 메서드는 호출하던 코드가 주석으로만 남아 있다.
  - `Equip`(:96), `Unequip`(:108)
  - `TryLevelUp`(:119), `BatchLevelUp`(:135)
  - `TrySynthesize`(:153), `BatchSynthesize`(:184)
- 해당 주석 블록은 다음과 같다.
  - `EquipDetailPopUp.cs:118-119, 127-128, 140-145`
  - `SynthesisWindow.cs:139-140, 148-149`
- `PlayerInventory`의 `AddGem`(:70), `AddGold`(:76), `TrySpendGem`(:82), `GetTotalStat`(:192), `Stamina`(:22)는 호출하는 곳이 없다.
  - `AddGold`는 `BattleManager.AddGold`와 이름이 같아 텍스트 검색으로는 구분되지 않는다.
- `AddItem`(:62)은 `InventoryTestDriver`만 쓴다. `CreateNewItem`(:259)은 `AddItem`만 호출하는 private 메서드다.
  - 이 드라이버는 실제 게임 씬인 `MainScene`에도 붙어 있다.
  - F1을 누르면 클라이언트가 임의로 정한 inventoryId로 장비를 추가한다. 서버에는 없는 장비다.
  - `Debug.isDebugBuild` 검사가 있어 에디터와 개발 빌드에서만 동작한다. 릴리스 빌드에는 영향이 없다.

## 4. 호출되지 않는 멤버
| 분류 | 멤버 |
|---|---|
| 스킬 | `SkillController.UnequipSkill`(:115), `SkillController.PassiveSkills`(:25), `SkillBase.LevelDown`(:74) |
| 무기·스탯 | `WeaponSkillTable.GetSkillByType`(:27), `PlayerStats.EquippedWeaponType`(:26), `EvolutionRules.IsMaxStep`(:53), `OwnedItem.ScaledMoveSpeed`(:29) |
| 런타임 상태 | `PlayerLuckTrain.Pending`, `IsShowing`(:30-31), `BossArena.Center`(:29), `ServerInventoryManager.IsRequesting`(:9) |
| UI | `MessagePopup.ShowConfirm`(:26) |
| 네트워크 | `ApiClient.Put`(:58), `ApiClient.Delete`(:61) |
| 데이터 | `StageData.RollRewardBoxGrade`(:30), `PlayerStatDto`(`PlayerApiDtos.cs:15`) |

- `TabBar.SetBadge`(:85), `ChallengeTab.SetChapters`(:17)는 주석 처리된 `MainTestDriver`에서만 호출한다. 그래서 ChallengeTab은 데이터를 받을 경로가 없다.
- `PlayerDebug.CompareFinalStats`(:115)는 private이고 키 연결도 없어 실행할 방법이 없다.
  - 이 메서드와 `PlayerServerStats`(1절)가 아래 코드의 유일한 사용처라서, 아래 코드도 함께 쓰이지 않는다.
  - `PlayerStatsComparison.cs`
  - `PlayerApi.GetFinalStats`
  - `FinalStatsResponse`, `StatBreakdown`, `StatParts`(`PlayerApiDtos.cs`)

## 5. 쓰이지 않거나 중복된 상수·필드
- `GameConstants.Server`의 `SIGNUP_API`, `LOGIN_API`, `PLAYER_SAVE_API`, `INVENTORY_API`(:47-50)
  - `AccountApi`, `InventoryApi`에 같은 값의 상수가 따로 있고 실제로는 그쪽을 쓴다.
- `GameConstants.SceneNames.MANAGERS_TEST_SCENE`, `MANAGERS_TEST_SCENE_2`(:10-11)
- `GameConstants.Value` 클래스 전체(:53-)
  - `REQUEST_TIMEOUT_SECONDS`는 `ApiClient.DefaultTimeoutSeconds`(:11)와 값이 같고, 실제로는 그쪽을 쓴다.
- 서버 에러 코드 상수 중 쓰는 곳이 없는 것
  - `InventoryApi`: `ErrorInventoryNotFound`, `ErrorNotEnoughGold`, `ErrorNotEnoughMaterial`(:11-13)
  - `PlayerApi`: `ErrorPlayerStatNotFound`, `ErrorInternal`, `ErrorEvolutionMaxStepReached`, `ErrorCurrencyNotFound`(:14-18 중 :16 제외)
  - 서버 에러 코드를 정리해 두는 목적이라면 남겨도 된다.
- 인스펙터 값만 있고 코드에서 읽지 않는 `[SerializeField]`
  - `ShopGemCard.gemAmount`(:9)
  - `ShopGoldCard.goldAmount`(:11)
  - `ShopSupplyBoxCard.minGrade`, `maxGrade`(:10-11)

## 6. 주석 처리된 코드
- `PlayerMovement.cs:61-66`의 `OnDrawGizmos`
- 3절의 `EquipDetailPopUp`, `SynthesisWindow` 주석 블록

## 7. 범위에서 뺀 부분
- 의도적으로 둔 코드
  - `[ContextMenu]`로 실행하는 테스트 메서드(`TestKill`, `TestHeal` 등)
  - 서버 응답·JSON을 받는 데이터 필드(`tokenType`, `spentGem`, `accountCreatedAt`, `clearAccountExp` 등)
  - 에디터 스크립트, `[RuntimeInitializeOnLoadMethod]`로 실행되는 메서드
- 미완성 기능
  - `Enemy.HpRatio`, `InGameHUD.SetBossHp`는 `HUDTestDriver`에서만 호출한다.
  - 불필요하다기보다 보스 HP 바가 아직 전투 로직에 연결되지 않은 것이다.
- 검출 한계
  - 텍스트 검색이라 `Clear`, `Show`처럼 흔한 이름의 메서드는 다른 코드의 같은 이름과 구분되지 않는다.
  - 그래서 이런 메서드는 쓰이지 않아도 빠졌을 수 있다.
