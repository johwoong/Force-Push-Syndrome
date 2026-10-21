# UI 작업 방법

`Docs/Guide/UIStyleGuide.md`의 규격을 Unity에 적용할 때 쓰는 도구 사용법과 작업 대기 목록이다. 규격 자체는 스타일 가이드를 본다.

## 1. 화면 캡처 (Unity CLI)
Editor가 켜져 있을 때 `unity command`로 진행한다. 시작 전에 Editor가 이미 Play 모드인지 확인하고, 사람이 실행해 둔 Play는 멈추지 않는다.

1. `editor_play` 후 `eval`로 `UnityEngine.Application.runInBackground = true;`를 실행한다. Editor 창이 포커스를 잃으면 Play 모드가 멈추기 때문이다. `PlayerSettings`는 바꾸지 않는다.
2. LogInScene의 자동 로그인 MessagePopup에서 `Panel/Buttons/ConfirmButton`의 onClick을 호출하면 MainScene으로 들어간다. 저장된 계정이 없으면 `CLAUDE.md`의 테스트 계정으로 로그인한다.
3. 탭은 `UIManager.Instance.Get<TabBar>().Select(i)`로 연다 (0 상점, 1 장비, 2 전투, 3 도전, 4 진화). 팝업은 `UIManager.Instance.Get<T>()`의 Show 계열 메서드로 띄운다.
4. `capture_game_view --source screen --width 1080 --height 1920 --save_path "Temp/UICapture/<이름>.png"`로 찍는다. 파일은 `Assets/Temp/UICapture/`에 저장되므로 프로젝트 밖으로 복사한 뒤 `delete_asset --asset "Assets/Temp" --confirm true`로 지운다.
5. 화면 왼쪽 위 `[F1] 합성 테스트용…` 문구와 오른쪽 위 `DEV` 버튼은 디버그 표시다. 검수 대상에서 뺀다.

## 2. 프리팹 수정
- `.prefab` 파일을 직접 고치지 않는다. `eval`에서 `PrefabUtility.LoadPrefabContents` → 수정 → `SaveAsPrefabAsset` → `UnloadPrefabContents`로 고친다. private 직렬화 필드는 `SerializedObject`로 바꾼다.
- 여러 프리팹을 도는 `eval`은 기본 제한 시간(5초)을 넘기므로 `--timeout`을 준다.
- 상위 프리팹(`MainUI`, `BattleUI`, `Resources/UI/UIManager`)의 덮어쓰기는 `PrefabUtility.GetPropertyModifications`로 찾고, 의도하지 않은 것은 `RevertPropertyOverride`로 되돌린다.
- 이름으로 찾는 일괄 변경에서 빠지기 쉬운 예
  - 장비 상세 팝업은 Dim이 `Dim` 오브젝트가 아니라 루트 `EquipDetailPopup`의 Image다.
  - 장착 칸 아이콘 크기는 프리팹이 아니라 `CharacterEquipView`의 직렬화 값(`equippedIconSize` 등)이 정한다.
  - 상점 구매 버튼 색은 `ShopGoldCard`, `ShopSupplyBoxCard`의 직렬화 색이 실행 중에 덮어쓴다.

## 3. 코드 수정 확인
`recompile` → `recompile_status`가 completed인지 → `console_status`의 `compilationFailed`가 false인지 본다.

## 4. 작업 대기 목록
규격은 정했지만 아직 적용하지 않았거나 확인하지 못한 항목이다. 끝나면 지운다.

| 항목 | 상태 |
|---|---|
| 모서리 반경 | 스타일 가이드 2장의 ppuMul을 적용하지 않았다. 프리팹은 모두 `Semi Rounded` ppuMul 2(반경 15)다 |
| 선택되지 않은 탭 아이콘 색 | 규격은 `#C9D3DF`지만 흰색이다. 아이콘의 `ThemedGraphic`이 테마 색으로 덮어쓰므로 `TabBar`에서 같이 바꿔야 한다 |
| 팝업 폭 | 설정·프로필·홈(784)과 장비 상세(700)를 840으로 맞춘다 |
| 디버그 표시 위치 | `[F1]` 문구와 `DEV` 버튼이 상단바를 가린다 |
| 화면 검수 | BattleTab, InventoryTab, 상점(구매 가능 상태), 메시지형 팝업만 캡처로 확인했다. 전투 창(일시정지, 행운열차, 결과)과 목록형 팝업(랭킹, 친구, 설정 등)은 일괄 변경만 했다 |
| Light 테마 검수 | 위에서 바꾼 화면과 상단바, 탭바를 모아 한 번에 검수한다 |
