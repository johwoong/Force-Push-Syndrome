# 게임 데이터 태그

기획자 없이 역설계하고 있어서 게임 데이터 선언이 JSON, 코드 상수, 인스펙터, 하드코딩, 테스트 더미에 흩어져 있다. 백엔드를 붙이기 전에 값의 성격을 구분하려고 태그를 정의하고, 현재 값을 개념 단위로 분류한다. 각 값에는 현행 상태의 태그와 구조상 이상적인 상태의 태그를 함께 적는다.

태그는 코드에 붙이지 않고 이 문서에서만 관리한다. 씬 이름, 리소스 경로, 성능 튜닝 상수 같은 기술 상수는 게임 데이터가 아니므로 분류하지 않는다.

## 1. 1차 태그
값 하나에 하나만 붙인다.

| 태그 | 정의 | 수명 | 백엔드 연동 시 |
|---|---|---|---|
| `DESIGN_TABLE` 기획-테이블 | ID로 구분되는 행 단위 정의 데이터 | 패치 단위 | 서버와 클라이언트가 같은 테이블을 가진다. 버전 관리가 필요하다 |
| `DESIGN_CONST` 기획-상수 | ID 없이 하나뿐인 수치나 공식 계수 | 패치 단위 | 서버의 검증 공식에 쓴다 |
| `ACCOUNT` 계정 | 인증·식별 정보 | 영구 | 인증 서버로 옮긴다. 클라이언트에 저장하지 않는다 |
| `USER` 유저 | 유저별로 쌓이는 진행 상태 | 영구 | DB 저장 대상이다 |
| `SESSION` 세션 | 전투 한 번 동안만 쓰는 값 | 전투가 끝나면 버린다 | 저장하지 않고 결과만 서버로 보낸다 |
| `CLIENT_PREF` 로컬설정 | 기기별 환경 설정 | 기기에 영구 저장 | 서버가 필요 없다. 현재 해당 값이 없다 |
| `PRESENTATION` 연출 | 게임 결과에 영향이 없는 표현용 수치 | 빌드 단위 | 관리 대상에서 빼고 인스펙터에 둔다 |
| `DEBUG` 테스트 | 개발용 임시값 | — | 빌드에서 뺀다 |

## 2. 판별 순서
1. 개발용 임시값이면 `DEBUG`
2. 게임 결과에 영향이 없으면 `PRESENTATION`
3. 모든 유저에게 같은 값이면, ID로 구분되는 행은 `DESIGN_TABLE`, 아니면 `DESIGN_CONST`
4. 유저마다 다른 값이면, 인증 정보는 `ACCOUNT`, 기기 설정은 `CLIENT_PREF`, 전투가 끝나면 버리는 값은 `SESSION`, 나머지는 `USER`

## 3. 권위 태그
`USER`, `SESSION`에만 붙인다.

| 태그 | 정의 | 대상 |
|---|---|---|
| `SERVER_AUTH` | 서버가 계산하고 클라이언트는 결과만 받는다 | 재화 증감, 장비 획득·강화 비용 차감, 뽑기 결과, 보상 지급 |
| `CLIENT_AUTH` | 클라이언트가 계산하고 서버는 결과를 받거나 사후 검증한다 | 이동, 전투 중 판정, 생존 시간, 킬 수 |

## 4. 현행 태그와 이상 태그
개념 단위 목록에서는 값마다 태그를 두 개 붙인다.

| 열 | 기준 |
|---|---|
| 현행 | 지금 코드에서 값이 선언되고 쓰이는 방식 |
| 이상 | 구조를 정리했을 때 값이 가져야 할 태그. 2장의 판별 순서를 따른다 |

현행 태그는 다음 기준으로 붙인다.
- 테스트 드라이버의 더미 값과 임시 코드는 `DEBUG`
- 게임 결과에 영향을 주는 값이 인스펙터나 코드 상수로 하나씩 들어 있으면 `DESIGN_CONST`
- 서버가 없어 현재는 모든 값을 클라이언트가 계산한다. 그래서 현행 태그에는 권위 태그를 붙이지 않는다

표기
- `삭제`: 다른 값으로 대체되거나 개발용이라서 이상 상태에서는 없어야 하는 값
- `—`: 아직 정의되지 않아 현재 없는 값
- 이상 태그의 1차 태그가 현행과 다르면 굵게 표시한다. 권위 태그가 붙은 것만으로는 다르다고 보지 않는다

## 5. 개념 단위별 데이터
위치의 표기는 다음과 같다.
- **JSON**: Resources 아래 JSON 파일
- **const**: 코드 상수
- **인스펙터**: `[SerializeField]` 값
- **하드코딩**: 메서드 안에 직접 쓴 값
- **더미**: 테스트 드라이버가 넣는 값
- **없음**: 아직 정의되지 않은 값

### 5.1 계정
| 값 | 위치 | 현행 | 이상 | 비고 |
|---|---|---|---|---|
| playerId, password | `LocalAccountData` → `accounts.json` | `ACCOUNT` | `ACCOUNT` | 현재 password를 평문으로 로컬에 저장한다 |
| 아이디 규칙(영문·숫자·밑줄 3~20자) | `LocalLoginManager.SignUp` 정규식 | `DESIGN_CONST` | `DESIGN_CONST` | 서버와 같은 규칙을 써야 한다 |

### 5.2 플레이어 기본 스탯
| 값 | 위치 | 현행 | 이상 | 비고 |
|---|---|---|---|---|
| playerBaseAttack, playerBaseHp, playerBaseCriticalDamage, playerBaseCriticalChance, playerBaseSkillDamage, playerBaseMoveSpeed, playerBaseMaxMoveSpeed, playerBaseLootRadius | `Resources/Data/PlayerBaseStat.json` | `DESIGN_CONST` | `DESIGN_CONST` | 전투에 쓰는 값은 playerBaseHp, playerBaseMoveSpeed, playerBaseLootRadius뿐이다(6.3). `PlayerBaseStatData` 필드 초기값이 같은 값을 한 번 더 정의한다(6.2) |
| 최종 스탯 공식 `(기본 + 장비) × (100 + 보너스%) / 100` | `PlayerStats.CalculateStat` | `DESIGN_CONST` | `DESIGN_CONST` | 장비 수치는 강화·등급 배율을 적용한 값이다. 장비 보너스%는 `ItemData`에 필드가 없어 0으로 계산한다 |
| maxLives 2, invulnerableTime 0.5 | `PlayerHealth` 인스펙터 | `DESIGN_CONST` | `DESIGN_CONST` | |
| dummyEquipments | `PlayerStats` 인스펙터(`DummyEquipment`) | `DEBUG` | `삭제` | 로그인 데이터가 없는 씬에서만 쓴다 |

### 5.3 재화
| 값 | 위치 | 현행 | 이상 | 비고 |
|---|---|---|---|---|
| gold, gem | `PlayerSaveData` | `USER` | `USER` · `SERVER_AUTH` | |
| currentStamina | `PlayerSaveData` | `USER` | `USER` · `SERVER_AUTH` | 전투에 입장할 때 `PlayerInventory.TrySpendStamina`로 차감한다. 회복하는 코드가 없다 |
| maxStamina | `PlayerSaveData` | `USER` | **`DESIGN_CONST`** | 모든 유저에게 같은 값인데 계정별로 저장된다(6.5) |
| 초기 재화(gold 0, gem 0, stamina 60/60) | `PlayerSaveData.CreateDefault` | `DESIGN_CONST` | `DESIGN_CONST` | `AccountConst.json`의 initialGold, initialGem, initialStamina, maxStamina가 같은 값을 한 번 더 정의하지만 읽지 않는다(6.2, 6.3) |
| 스태미나 회복 주기 | 없음 | `—` | **`DESIGN_CONST`** | |
| 스태미나 회복 기준 시각 | 없음 | `—` | **`USER`** · `SERVER_AUTH` | 회복을 계산하려면 필요하다 |
| DNA | 없음 | `—` | **`USER`** · `SERVER_AUTH` | `EvolutionInfo.dna` 필드만 있고 값을 넣는 코드가 없다 |

### 5.4 계정 성장
| 값 | 위치 | 현행 | 이상 | 비고 |
|---|---|---|---|---|
| accountLevel, accountExp | `PlayerSaveData` | `USER` | `USER` · `SERVER_AUTH` | accountExp는 현재 레벨에서 쌓은 경험치다. `PlayerInventory.ClaimBattleResult`가 전투 결과의 계정 경험치를 더하고, 필요 경험치를 채울 때마다 차감하며 레벨을 올린다. 최대 레벨에서는 레벨업을 멈추고 경험치는 계속 쌓인다 |
| 계정 레벨 필요 경험치(accountBaseRequiredExp 100, accountRequiredExpIncrement 100), 최대 계정 레벨(maxAccountLevel 20) | `Resources/JsonFiles/AccountConst.json` | `DESIGN_CONST` | `DESIGN_CONST` | 필요 경험치 = `100 + 100 × (레벨 - 1)`(`AccountConstData.GetRequiredExp`). `TopBar` 경험치 바 비율 = accountExp / 필요 경험치이고, 최대 레벨이면 1이다 |
| 진화 노드(level, name, value, description, cost) | 없음 | `—` | **`DESIGN_TABLE`** | `EvolutionNodeInfo` 구조체만 있다. 값을 넣던 `MainTestDriver`는 전부 주석 처리됐다. 골드 노드와 DNA 노드 두 종류. value가 문자열이라 효과를 계산할 수 없다 |
| 레벨당 골드 노드 수 3 | `EvolutionTab.NodesPerLevel` const | `DESIGN_CONST` | `삭제` | 진화 노드 테이블의 level 열에서 결정된다 |
| 해금한 진화 노드 수 | 없음 | `—` | **`USER`** · `SERVER_AUTH` | `EvolutionInfo`에 필드만 있다 |
| 진화 트리 간격·여백 | `EvolutionTab` 인스펙터 | `PRESENTATION` | `PRESENTATION` | |

### 5.5 장비
| 값 | 위치 | 현행 | 이상 | 비고 |
|---|---|---|---|---|
| itemId, itemName, description, iconPath, slotType, grade, hpBonus, attackBonus, moveSpeedBonus, gradeSkills | `Resources/JsonFiles/Item.json` | `DESIGN_TABLE` | `DESIGN_TABLE` | grade는 획득할 때의 기본 등급이다. gradeSkills가 문자열이라 효과를 계산할 수 없다(장비 상세 팝업에 표시만 한다) |
| 부위(`EquipSlotType`), 장비 등급(`ItemGrade`) | `EquipDefine.cs` enum | `DESIGN_CONST` | `DESIGN_CONST` | 도전 보상 등급도 `ItemGrade`를 쓴다 |
| 무기 종류(`WeaponType`) | `WeaponType.cs` enum | `DESIGN_CONST` | `DESIGN_CONST` | 임시 정의. `ItemData`에 필드가 없어 `WeaponSkillTable`로 구한다 |
| 무기별 시작 스킬(6001→3, 6002→2, 6003→1), 무기 종류↔스킬 매핑 | `WeaponSkillTable` const | `DEBUG` | **`DESIGN_TABLE`** | 장비 테이블의 열. 코드 주석에 임시 코드로 표시돼 있다 |
| 강화: maxLevel 10, levelUpBaseCost 1000, statGrowthPerLevel 0.1 | `Resources/Data/ItemConst.json` | `DESIGN_CONST` | `DESIGN_CONST` | 비용 = `1000 × 현재 레벨`, 배율 = `1 + 0.1 × (레벨 - 1)`. 계산은 `ItemLevelConfig`가 한다 |
| 합성: synthesisMaterialCount 2, gradeStatMultiplier(1, 1.75, 2.75) | `Resources/Data/ItemConst.json` | `DESIGN_CONST` | `DESIGN_CONST` | 같은 itemId·같은 등급 장비 2개를 소모해 한 등급 올린다. 장비 수치 = `기본 수치 × 강화 배율 × 등급 배율`. 등급 배율은 `ItemGrade` 순서이고 개수가 다르면 로드에 실패한다 |
| 보유 장비(instanceId, itemId, level, isEquipped, grade) | `PlayerSaveData.equipmentList` | `USER` | `USER` · `SERVER_AUTH` | grade는 합성으로 바뀐 현재 등급이다 |

### 5.6 상점
| 값 | 위치 | 현행 | 이상 | 비고 |
|---|---|---|---|---|
| 보석 상품 지급량(gemAmount) | `ShopGemCard` 인스펙터 | `DESIGN_CONST` | **`DESIGN_TABLE`** | 상품 ID로 구분되는 상품 테이블. 현재는 결제 없이 바로 지급한다 |
| 골드 상품 가격·지급량(gemCost, goldAmount) | `ShopGoldCard` 인스펙터 | `DESIGN_CONST` | **`DESIGN_TABLE`** | 상품 테이블 |
| 지원품 상자 가격·등급 범위(gemCost, minGrade, maxGrade) | `ShopSupplyBoxCard` 인스펙터 | `DESIGN_CONST` | **`DESIGN_TABLE`** | 상품 테이블 |
| 지원품 상자 확률 | 없음 | `—` | **`DESIGN_TABLE`** | 현재는 기본 등급이 범위 안인 장비 중에서 균등 랜덤이다 |
| 구매·뽑기 결과 | 클라이언트에서 계산 | `USER` | `USER` · `SERVER_AUTH` | |
| 카드·등급 색상 | 상점 카드·장비 팝업 인스펙터 | `PRESENTATION` | `PRESENTATION` | |

### 5.7 스테이지·스폰
| 값 | 위치 | 현행 | 이상 | 비고 |
|---|---|---|---|---|
| stageId, stageName, stageIllustrationColor, stageDuration, waveId, stageDescription, clearAccountExp, rewardBoxGradeWeights | `Resources/JsonFiles/Stage.json` | `DESIGN_TABLE` | `DESIGN_TABLE` | stageIllustrationColor는 연출용 열이다. stageDuration은 읽는 코드가 없다(6.3). waveId는 `Wave.json`의 웨이브이고 스테이지와 1:1이다. clearAccountExp는 승리했을 때 더하는 계정 경험치다. rewardBoxGradeWeights는 `ItemGrade` 순서의 보상상자 장비 등급 가중치이고 모든 스테이지가 `100,0,0`이다(5.12) |
| 선택한 stageId | `GameSceneManager.pendingStageId` | `SESSION` | `SESSION` · `CLIENT_AUTH` | 메인 씬에서 전투 씬으로 넘길 때만 쓴다 |
| 기본 stageId 1 | `WaveManager` 인스펙터 | `DEBUG` | `삭제` | 메인 씬을 거치지 않고 전투 씬을 실행할 때만 쓴다 |
| 전투 입장 스태미나 비용(battleStaminaCost 5) | `Resources/JsonFiles/AccountConst.json` | `DESIGN_CONST` | `DESIGN_CONST` | 모든 스테이지가 같은 값을 쓴다. `BattleTab`에 표시하고, 입장할 때 차감한다. 스태미나가 부족하면 입장하지 않는다. 시트의 AccountConst 탭에는 이 열이 없다(6.8) |
| 웨이브(waveEntryId, waveId, patternStartTime, patternId, monsterId) | `Resources/JsonFiles/Wave.json` | `DESIGN_TABLE` | `DESIGN_TABLE` | 한 행이 시각, 패턴, 몬스터 1종이다. 여러 종은 같은 시각에 행을 여러 개 둔다. `WaveManager`가 스테이지의 waveId 행을 경과 시간으로 진행한다. 웨이브 1만 있고, 스테이지 2의 waveId 2는 행이 없다. 계층은 Stage(3차) → Wave(2차) → SpawnPattern·Monster(1차)다 |
| 스폰 패턴(patternId, eventType, formation, spawnCount, spawnInterval, patternDuration, dropTableId) | `Resources/JsonFiles/SpawnPattern.json` | `DESIGN_TABLE` | `DESIGN_TABLE` | formation은 Random, Ring, Line, Cluster, Screen이다. 패턴 1~6만 웨이브가 쓰고 7~9(Ring, Line, Cluster)는 `WaveManager`의 `TestSpawnPattern` ContextMenu로 검증하는 샘플이다. 상자는 Screen 패턴으로만 스폰해야 한다. 드롭 테이블은 패턴별로 두므로 모양이 같아도 드롭이 다르면 패턴을 나눈다 |
| spawnRadius 10, minRepeatInterval 0.1 | `WaveManager` 인스펙터 | `DESIGN_CONST` | `DESIGN_CONST` | |
| lineLength 6, clusterRadius 1.5 | `WaveManager` 인스펙터 | `DESIGN_CONST` | `DESIGN_CONST` | 임시값이다. 모든 패턴이 공통으로 쓴다 |
| 스테이지 기록(stageId, isCleared, bestSurvivalSeconds) | `PlayerSaveData.stageRecordList` | `USER` | `USER` · `CLIENT_AUTH` | 가입할 때 `Stage.json`의 스테이지마다 만들고 전투 결과로 갱신한다 |

### 5.8 도전
값을 넣던 `MainTestDriver`가 전부 주석 처리돼 구조체만 남았다.

| 값 | 위치 | 현행 | 이상 | 비고 |
|---|---|---|---|---|
| 챕터별 도전 조건(name, description) | 없음 | `—` | **`DESIGN_TABLE`** | `ChallengeCondition` 구조체만 있다. 설명만 있어 효과를 계산할 수 없다 |
| 도전 보상(grade, amount, techPart) | 없음 | `—` | **`DESIGN_TABLE`** | `ChallengeReward` 구조체만 있다. grade는 `ItemGrade`다 |
| 보상 수령 여부(rewarded) | 없음 | `—` | **`USER`** · `SERVER_AUTH` | `ChallengeInfo`에 필드만 있고 `PlayerSaveData`에 없다 |

### 5.9 몬스터
| 값 | 위치 | 현행 | 이상 | 비고 |
|---|---|---|---|---|
| monsterId, monsterName, monsterType, monsterMaxHealthPoint, monsterExp, monsterMoveSpeed, monsterAttackPoint, monsterAsset | `Resources/JsonFiles/Monster.json` | `DESIGN_TABLE` | `DESIGN_TABLE` | monsterType은 Normal, Elite, Boss, Box. monsterId는 11001~ 일반, 12001~ 엘리트, 13001~ 보스, 14001~ 상자 대역이다. monsterExp는 읽는 코드가 없다(6.3) |
| 몬스터 공격(monsterAttackId, monsterId, monsterAttackType, monsterAttackCooldown, monsterAttackTriggerRange, monsterAttackDamage, monsterAttackCount, monsterAttackAngle, monsterAttackDuration) | `Resources/Data/MonsterAttack.json` | `DESIGN_TABLE` | `DESIGN_TABLE` | 한 몬스터가 여러 공격을 가진다. 공격 종류는 Melee, Ranged, Area, Trap이다 |
| 접촉 공격 attackRange 0.6, attackInterval 1 | `Enemy` 인스펙터 | `DESIGN_CONST` | `DESIGN_CONST` | |
| 몬스터 현재 HP | `Enemy.currentHp` | `SESSION` | `SESSION` · `CLIENT_AUTH` | |

### 5.10 스킬
| 값 | 위치 | 현행 | 이상 | 비고 |
|---|---|---|---|---|
| ID, Name, Type, Cooldown, Speed, Damage, Range, Description | `Resources/JsonFiles/Skill.json` | `DESIGN_TABLE` | `DESIGN_TABLE` | 모든 열을 읽는다. Speed와 Range는 스킬마다 뜻이 다르다(Planet은 초당 회전 각도와 회전 반지름). Damage는 플레이어 공격력과 관계없는 고정 피해다. 레벨업 선택지 후보는 테이블 전체다. ID 5(Missile)는 구현 예정이라 `SkillFactory`에 등록되지 않았고, 등록되지 않은 스킬은 후보에서 빠진다 |
| 투사체 수명(lifeTime 3) | `LinearProjectile` 인스펙터 | `DESIGN_CONST` | **`DESIGN_TABLE`** | 스킬 테이블의 열 |
| 최대 스킬 슬롯 3 | `SkillController.MaxSkillSlots` const | `DESIGN_CONST` | `DESIGN_CONST` | |
| 스킬 최대 레벨 5, 레벨당 배율(쿨타임 0.758, 피해 1.2) | `SkillBase` const | `DESIGN_CONST` | **`DESIGN_TABLE`** | 스킬 레벨 테이블. 쿨타임 = `Cooldown × 0.758^(레벨 - 1)`, 피해 = `Damage × 1.2^(레벨 - 1)`. 모든 스킬이 같은 배율을 쓴다. 코드 주석에 수정 예정으로 표시돼 있다 |
| 레벨별 효과(발사 수, 관통 수, 부채꼴 각도, 행성 수) | `Skill_Shuriken`, `Skill_Revolver`, `Skill_Katana`, `Skill_Planet` 하드코딩 | `DESIGN_CONST` | **`DESIGN_TABLE`** | 스킬 레벨 테이블. 모두 3레벨과 5레벨에서 바뀐다 |
| 전투 중 보유 스킬·레벨 | `SkillController._activeSkills` | `SESSION` | `SESSION` · `CLIENT_AUTH` | |

### 5.11 전투 진행
| 값 | 위치 | 현행 | 이상 | 비고 |
|---|---|---|---|---|
| 경과 시간, 킬 수, 전투 레벨, 경험치, 대기 레벨업 수, 골드, 보상상자 수 | `BattleManager` 필드 | `SESSION` | `SESSION` · `CLIENT_AUTH` | |
| 레벨업 필요 경험치(base 6, 레벨당 +6, 가속 0.3) | `BattleManager` 인스펙터 | `DESIGN_CONST` | `DESIGN_CONST` | 필요 경험치 = `6 + 6 × (레벨 - 1) + 반올림(0.3 × (레벨 - 1)²)` |
| 레벨업 선택지 수 3 | `BattleManager.OptionCount` const | `DESIGN_CONST` | `DESIGN_CONST` | |
| 선택지가 없을 때 회복량 30% | `BattleManager.healRewardRatio` 인스펙터 | `DESIGN_CONST` | `DESIGN_CONST` | |
| 승리 판정 지연 2초 | `BattleManager.bossVictoryDelay` 인스펙터 | `DESIGN_CONST` | `DESIGN_CONST` | 보스를 처치하고 2초 뒤 승리한다 |
| 계정 경험치 계수(킬당 1, 초당 1) | `BattleManager` 인스펙터 | `DESIGN_CONST` | `DESIGN_CONST` | 계정 경험치 = `킬 수 × 1 + 생존 초 × 1 + 클리어 보상`. 코드 주석에 임시값으로 표시돼 있다 |
| 현재 HP, 남은 목숨 | `PlayerHealth` | `SESSION` | `SESSION` · `CLIENT_AUTH` | |
| 전투 결과(stageId, 승패, 생존 시간, 킬 수, 골드, 보상상자 수, 계정 경험치) | `BattleResult.Last` | `SESSION` | `SESSION` · `CLIENT_AUTH` | 메인 씬의 `PlayerInventory.ClaimBattleResult`가 gold, accountExp, 스테이지 기록, 보상상자 장비에 반영한다. 반영은 보상 지급이라 `SERVER_AUTH` 대상이다 |
| 행운열차 결과(선택 칸, 골드) | `PlayerLuckTrain` 필드 | `SESSION` | `SESSION` · `CLIENT_AUTH` | |
| 행운열차 확률(5칸 10%, 3칸 20%, 1칸 70%), 골드량(칸당 100~300) | `PlayerLuckTrain` const·인스펙터 | `DESIGN_CONST` | **`DESIGN_TABLE`** | 골드 = `100~300 랜덤 × 선택 칸 수`. 골드량은 코드 주석에 임시값으로 표시돼 있다 |
| 행운열차 회전 시간·바퀴 수·공개 간격 | `LuckTrainWindow` 인스펙터 | `PRESENTATION` | `PRESENTATION` | |

### 5.12 드롭 아이템
| 값 | 위치 | 현행 | 이상 | 비고 |
|---|---|---|---|---|
| 드롭 종류(`DropItemType`) | `DropItemType.cs` enum | `DESIGN_CONST` | `DESIGN_CONST` | ExpGem1~4, Gold1~4, LuckyBox, RewardBox, Potion, Magnet, Bomb |
| dropItemId, dropItemType, value | `Resources/JsonFiles/DropItem.json` | `DESIGN_TABLE` | `DESIGN_TABLE` | 종류별 수치. ExpGem1~4 3·5·10·100, Gold1~4 10·30·100·300, Bomb 50, Potion 50. ExpGem2~4는 떨어뜨리는 코드가 없다(6.3). Bomb은 엘리트·보스에게 주는 피해량이다. 일반 몬스터는 즉사하고, 범위 제한 없이 살아 있는 몬스터 전체가 대상이다. Potion은 기본 회복량이고 계산식이 추가될 예정이다 |
| 드롭 테이블(dropTableId, dropGroup, dropItemType, weight, count) | `Resources/JsonFiles/DropTable.json` | `DESIGN_TABLE` | `DESIGN_TABLE` | 스폰 패턴의 dropTableId가 참조하고, 몬스터는 스폰될 때 받은 테이블로 처치 보상을 정한다. 같은 테이블 안에서 dropGroup마다 한 번씩 weight로 1행을 뽑아 count개 떨어뜨린다. dropItemType이 None이면 건너뛴다. 테이블 1 일반(ExpGem1), 2 엘리트(ExpGem1, LuckyBox 1, RewardBox 2), 3 보스(ExpGem1, RewardBox 5), 4 상자(Gold1~4 31·5·3·1, Bomb·Potion·Magnet 각 20 중 1개). 테이블 5는 검증용 샘플이다. 경험치잼은 monsterExp와 관계없이 ExpGem1로 고정돼 있다(6.3) |
| 보상상자 내용물 | `Resources/JsonFiles/Stage.json` rewardBoxGradeWeights 열 | `DESIGN_TABLE` | `DESIGN_TABLE` | 상자마다 스테이지의 등급 가중치로 등급을 뽑고, 그 등급이 기본 등급인 장비 중에서 균등 랜덤으로 지급한다(`StageData.RollRewardBoxGrade`) |
| 흡수 연출(recoilDistance, recoilDuration, chaseStartSpeed, chaseAcceleration, arriveRadius) | `DropItemManager` 인스펙터 | `PRESENTATION` | `PRESENTATION` | |

## 6. 발견된 문제
분류하면서 확인한 사실만 적는다.

### 6.1 기획 값의 저장 방식이 네 가지다
- JSON은 `Resources/Data` 한 폴더에 있다(Monster, MonsterAttack, Wave, SpawnPattern, Stage, Skill, Item, DropItem, DropTable, AccountConst, PlayerBaseStat, ItemConst, Shop)
- `Resources/Data`의 JSON은 구글 시트의 같은 이름 탭을 내보낸 것이다(서버 레포 `Tools/SheetExporter`, `StaticDataClient.md` 3장). 원본은 시트이고, JSON만 고치면 다음 내보내기에서 덮어써진다. 시트에는 JSON이 없는 탭(PlayerConst, BattleConst, MonsterConst)도 있다
- JSON 로더는 `JsonDataManager` 하나다. Shop.json만 읽지 않는다(상점 카드는 인스펙터 값을 쓴다)
- 코드 상수: `SkillBase`, `MaxSkillSlots`, `OptionCount`, `PlayerLuckTrain`, `WeaponSkillTable`, `NodesPerLevel`
- 인스펙터: `BattleManager`, `PlayerHealth`, `PlayerLuckTrain`, `WaveManager`, `Enemy`, 상점 카드, 투사체 프리팹
- 하드코딩: 스킬 레벨별 효과(`Skill_*`)

### 6.2 같은 의미의 중복 정의
| 의미 | 정의 1 | 정의 2 |
|---|---|---|
| 플레이어 기본 스탯 | `PlayerBaseStat.json` | `PlayerBaseStatData` 필드 초기값 |
| 초기 재화·최대 스태미나 | `AccountConst.json` | `PlayerSaveData.CreateDefault` |

### 6.3 불러오지만 쓰지 않는 값
- `Stage.json`의 stageDuration. 승리는 보스 처치로 판정한다. 값도 150초로 웨이브 1의 보스 등장(45초)과 다르다
- `Monster.json`의 monsterExp. 경험치 드롭은 `DropTable.json`에서 ExpGem1로 고정돼 있다
- `PlayerBaseStat.json`의 playerBaseAttack, playerBaseCriticalDamage, playerBaseCriticalChance, playerBaseSkillDamage, playerBaseMaxMoveSpeed. 스킬 피해는 `Skill.json`의 Damage만 쓴다. playerBaseAttack은 최종 공격력까지 계산하지만 디버그 로그에서만 읽는다
- `DropItem.json`의 ExpGem2~4. `DropTable.json`이 ExpGem1만 쓴다
- `AccountConst.json`의 initialGold, initialGem, initialStamina, maxStamina. 계정을 만들 때 `PlayerSaveData.CreateDefault`에 직접 쓴 값을 쓴다

### 6.4 저장 구조에 없는 `USER` 값
DNA, 해금한 진화 노드 수, 도전 보상 수령 여부, 스태미나 회복 기준 시각

### 6.5 저장 구조에 들어간 기획 값
`PlayerSaveData.maxStamina`는 모든 유저에게 같은 값인데 계정별로 저장된다.

### 6.6 정의되지 않은 기획 데이터
스태미나 회복 주기, 지원품 상자 확률, 진화 노드, 도전 조건·보상

### 6.7 계정 정보 평문 저장
`accounts.json`에 password가 평문으로 저장된다.

### 6.8 시트와 JSON 불일치
2026-10-05에 시트의 모든 탭을 `Resources/JsonFiles`와 대조했다. 아래 탭만 다르다.
- AccountConst: battleStaminaCost가 JSON에만 있다. 시트대로 내보내면 열이 사라져 전투 입장 스태미나 비용이 0이 된다
