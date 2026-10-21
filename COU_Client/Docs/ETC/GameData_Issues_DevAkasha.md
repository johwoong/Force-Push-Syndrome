# 게임 데이터 이슈 - DevAkasha 작업분

`GameData_Tags.md`에서 확인한 이슈 중 DevAkasha가 작성한 코드에 해당하는 것만 모은다. 작성자는 `git blame` 기준이다. 괄호 안 번호는 `GameData_Tags.md`의 절 번호다.

## 1. 전투 진행
- `BattleManager`의 accountExpPerKill, accountExpPerSecond가 코드 주석에 임시값으로 표시돼 있다 (5.11)
- `WaveManager`
  - 기본 stageId 1은 메인 씬을 거치지 않고 전투 씬을 실행할 때만 쓰는 `DEBUG` 값이다 (5.7)
  - lineLength 6, clusterRadius 1.5가 임시값이다. Ring, Line, Cluster 패턴(7~9)은 `TestSpawnPattern` ContextMenu 검증용 샘플이라 웨이브에 들어 있지 않다 (5.7)
- `Wave.json` 웨이브 1의 보스 등장(45초)이 `Stage.json` duration(150초)과 다르다. duration은 읽는 코드가 없지만 `StageData` 주석에는 보스 등장 시각과 맞춘 값이라고 적혀 있다 (5.7, 6.3)
- 스테이지 2의 waveID 2에 웨이브 행이 없다 (5.7)

## 2. 드롭·보상
- `DropTable.json`이 경험치 드롭을 ExpGem1로 고정해 `Monster.json`의 monsterExp와 `DropItem.json`의 ExpGem2~4를 쓰지 않는다 (5.12, 6.3)
- `PlayerInventory.ClaimBattleResult`가 보상 반영과 계정 레벨업을 클라이언트에서 한다. 보상 지급이라 `SERVER_AUTH` 대상이다 (5.4, 5.11)

## 3. 스킬
- `SkillBase`의 MaxLevel 5와 레벨당 배율(쿨타임 0.758, 피해 1.2)이 const이고 모든 스킬이 같은 배율을 쓴다. 스킬 레벨 테이블로 옮겨야 한다 (5.10)

## 4. 메인 UI
- `BattleTab.StartBattle`이 전투 입장 스태미나를 클라이언트에서 차감한다. 재화 증감이라 `SERVER_AUTH` 대상이다 (5.3, 5.7)
- 진화(`EvolutionTab`, `EvolutionInfo`)
  - 진화 노드 데이터가 없다. `EvolutionNodeInfo` 구조체만 있고 value가 문자열이라 효과를 계산할 수 없다 (5.4, 6.6)
  - `EvolutionTab.NodesPerLevel` const는 진화 노드 테이블의 level 열에서 결정돼야 한다 (5.4)
  - 해금한 진화 노드 수와 DNA가 저장 구조에 없다 (5.3, 5.4, 6.4)
- 도전(`ChallengeInfo` 외)
  - 도전 조건·보상 데이터가 없다. `ChallengeCondition`은 설명만 있어 효과를 계산할 수 없다 (5.8, 6.6)
  - 보상 수령 여부(rewarded)가 `PlayerSaveData`에 없다 (5.8, 6.4)

## 5. 범위에서 뺀 부분
- 다른 작성자의 코드: `PlayerLuckTrain`, `PlayerHealth`, `PlayerLootReceiver`, `LinearProjectile`의 lifeTime, `Skill_*`의 레벨별 효과
- DevAkasha 작성 코드 중 연출 값(`PRESENTATION`)만 있는 `DropItemManager`, `LuckTrainWindow`는 이슈가 없다
