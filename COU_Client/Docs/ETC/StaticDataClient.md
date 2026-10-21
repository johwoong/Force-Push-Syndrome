# 정적 데이터 클라이언트 작업 가이드

정적 데이터 API(`StaticData.md`)를 쓰기 위해 클라이언트에서 바꿀 작업을 정리한다. 기준은 `DesignDecisions.md` 2번이다.

## 1. 작업 목록
| 작업 | 위치 | 비고 |
|---|---|---|
| 데이터 로드 흐름 변경 | `JsonDataManager` | 빌드 사본 → 저장본 → 서버 응답 순으로 덮어쓴다 |
| 정적 데이터 API 호출 | 로그인 전 단계(위치 미정) | 클라이언트에 아직 서버 통신 코드가 없다 |
| Unity Exporter 제거 | `Assets/Scripts/Parser/` | 서버 레포 `Tools/SheetExporter`로 대체한다(3장). jyj8943 작성 코드라서 작성자와 협의한다 |

## 2. 런타임: 데이터 로드

### 2.1 현재 구조
- 정적 데이터는 모두 `JsonDataManager`가 보관한다. 다른 클래스는 데이터를 들고 있지 않다.
- `JsonDataManager.Awake`가 `LoadTable`로 12개 테이블을 동기로 읽는다. 읽기 경로는 `ReadTableText` 하나다(쓸 수 있는 저장본 → 빌드 사본).
- 시작 로드는 테이블별로 실패를 허용한다. 실패한 테이블은 로그를 남기고 null로 둔다.
- `JsonDataManager`는 LogInScene에만 배치돼 있다(`isDontDestroy`). 다른 씬을 직접 실행하면 처음 `GameManager.JsonData`에 접근할 때 생성되고, 그때 `Awake`가 바로 실행된다.

### 2.2 서버 테이블과 클라이언트 로드 위치
서버는 시트의 모든 탭을 내려준다.

| 서버 테이블 | 클라이언트 파서(`JsonDataManager`) | 경로 상수 |
|---|---|---|
| AccountConst | `ParseAccountConstData` | `AccountConstData_Json_Path` |
| Stage | `ParseStageData` | `StageData_Json_Path` |
| Wave | `ParseWaveData` | `WaveData_Json_Path` |
| SpawnPattern | `ParseSpawnPatternData` | `SpawnPatternData_Json_Path` |
| Monster | `ParseMonsterData` | `MonsterData_Json_Path` |
| MonsterAttack | `ParseMonsterAttackData` | `MonsterAttackData_Json_Path` |
| DropTable | `ParseDropTableData` | `DropTableData_Json_Path` |
| DropItem | `ParseDropItemData` | `DropItemData_Json_Path` |
| Item | `ParseItemData` | `ItemData_Json_Path` |
| Skill | `ParseSkillData` | `SkillData_Json_Path` |
| PlayerBaseStat | `ParsePlayerBaseStatData` | `PlayerBaseStatData_Json_Path` |
| ItemConst | `ParseItemConstData` | `ItemConstData_Json_Path` |
| Shop | 없음 | 없음. 서버에서 받아 저장만 하고 읽지 않는다 |

### 2.3 바꿀 흐름
흐름도는 `StaticData.md` 4번에 있다.

1. 시작할 때 서버 테이블은 저장본을 읽고, 저장본이 없으면 빌드 사본을 읽는다.
2. 로그인 전에 `GET /api/static-data?version=<저장된 버전>`을 호출한다. 저장된 버전이 없으면 `version` 없이 호출한다.
3. 204면 가진 데이터를 그대로 쓴다.
4. 200이면 응답 `version`의 첫째 자리를 빌드 값과 비교한다.
   - 다르면 앱 업데이트를 요구한다. 받은 데이터는 쓰지 않는다.
   - 같으면 `tables`를 파싱하고, 성공하면 저장한 뒤 메모리 데이터를 바꾼다.
5. 요청이 실패하면(네트워크 오류, 5xx) 덮어쓰지 않고 가진 데이터를 유지한다.

`tables.<이름>`의 값은 빌드 사본 `Resources/JsonFiles/<이름>.json`과 형식이 같다(`{ "datas": [...] }`). `Load*`가 텍스트나 `JObject`를 인자로 받게 바꾸면 빌드 사본, 저장본, 응답을 같은 파서로 읽는다.

### 2.4 요청 예시
UnityWebRequest 기준이다. 204도 `Result.Success`로 끝나므로 상태 코드로 나눈다.

```csharp
IEnumerator FetchStaticData(string baseUrl, string savedVersion)
{
    string url = baseUrl + "/api/static-data";
    if (!string.IsNullOrEmpty(savedVersion))
        url += "?version=" + UnityWebRequest.EscapeURL(savedVersion);

    using (var request = UnityWebRequest.Get(url))
    {
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
            yield break;    // 가진 데이터 유지

        if (request.responseCode == 204)
            yield break;    // 가진 데이터가 최신

        JObject body = JObject.Parse(request.downloadHandler.text);
        string version = body.Value<string>("version");
        JObject tables = (JObject)body["tables"];
        // 첫째 자리 확인 → 테이블 파싱 → 저장 → 메모리 교체
    }
}
```

### 2.5 저장
- `Application.persistentDataPath` 아래에 저장한다. 계정·플레이어 세이브(`SaveLoadHelper`)와 같은 위치다.
- 테이블마다 `tables.<이름>` 값을 그대로 `<이름>.json`으로 쓴다. 버전은 별도 파일에 문자열로 쓴다.
- 버전 파일은 테이블을 모두 쓴 뒤 마지막에 쓴다. 중간에 실패하면 버전이 이전 값으로 남아, 다음 요청에서 다시 받는다.

### 2.6 메모리 교체
- `ApplyStaticDataResponse`가 받은 테이블을 모두 파싱한 뒤 한 번에 바꾼다. 하나라도 실패하면 기존 데이터를 유지하고 저장하지 않는다. 응답에 없는 테이블은 가진 데이터를 유지한다.
- 교체 뒤 부수 처리는 `RebuildMonsterAttackIndex`뿐이다. 데이터를 따로 들고 있는 클래스가 없으므로 다른 곳에 알릴 필요가 없다.
- 갱신은 로그인 전에 끝나므로 전투 중에 데이터가 바뀌는 경우는 없다.

### 2.7 버전
- 빌드는 자신이 읽을 수 있는 첫째 자리 값을 상수로 가진다(`STATIC_DATA_MAJOR_VERSION`). 현재 `2`다.
- 저장본 버전의 첫째 자리가 빌드 값과 다르면(앱 업데이트 직후) 저장본을 쓰지 않는다. 빌드 사본으로 시작하고 `version` 없이 요청한다.
- 서버는 버전을 문자열 완전 일치로만 비교한다. 클라이언트는 첫째 자리만 숫자로 비교한다.

## 3. 데이터 내보내기: SheetExporter
클라이언트의 `Tools > Google Sheets > JSON Exporter`는 제거하고, 서버 레포의 Windows 도구 `Tools/SheetExporter/publish/SheetExporter.exe`로 내보낸다. 절차와 버전 규칙은 `StaticData.md` 5.2, 입력 항목은 5.3에 있다.

### 3.1 동작
- 시트의 모든 탭을 받아 검증하고 저장 위치(서버 `src/main/resources/data/`)에 쓴다. 탭 하나라도 실패하면 아무 파일도 쓰지 않는다.
- "클라이언트 복제"를 체크하면 모든 탭을 클라이언트 데이터 위치(`Assets/Resources/JsonFiles`)에도 쓴다. 처음에는 체크되어 있지 않고, 이후에는 마지막 상태를 유지한다.
- 내용이 같은 파일은 쓰지 않는다. 줄바꿈(CRLF/LF) 차이는 무시한다.
- 파일을 지우지 않는다. 시트에 탭이 없는 파일(Spawn.json 등)은 그대로 남는다.
- 커밋하지 않는다. 사람이 diff를 확인하고 커밋한다.

### 3.2 빌드 사본을 갱신할 때
- 실행 중에는 서버 응답이 빌드 사본을 덮어쓰므로, 빌드 사본이 조금 오래돼도 동작한다.
- 갱신이 필요한 때: 클라이언트 코드가 새 열을 읽도록 바뀌었을 때, 서버 없이 전투 씬을 실행해 최신 데이터를 확인할 때, 릴리스 빌드 전.
- 클라이언트 데이터 위치에 클라이언트 레포의 `Assets/Resources/JsonFiles` 폴더를 지정한다. 입력값은 사용자별로 `%LocalAppData%\COU\SheetExporter\settings.json`에 저장된다.
- 새 탭의 JSON(ItemConst 등)은 Unity가 임포트할 때 `.meta`를 만든다. 커밋할 때 함께 넣는다.
- 열 이름이 바뀐 탭을 빌드 사본에 쓰면 그 테이블을 읽는 클라이언트 코드가 함께 바뀌어야 한다.

## 4. 선행·확인 사항
2026-10-08에 시트 전체를 변환해 기존 파일과 비교한 결과다. 모든 탭이 검증을 통과한다.

| 탭 | 상태 | 영향 |
|---|---|---|
| Monster | monsterExp 열이 빠졌고 행이 7개에서 49개로 늘었다 | 서버 `monster.data.MonsterData`와 클라이언트 `MonsterData`에 monsterExp 필드가 남아 있다(읽는 곳은 없다). 서버 `MonsterTable`이 이 열 없이 읽히는지 내보낸 뒤 서버를 실행해 확인한다 |
| Stage | stage 1만 있다(JSON에는 stage 2 Desert가 있다) | 내보내면 stage 2가 사라진다. 지금 JSON의 stage 2는 Wave에 없는 waveId 2를 써서 서버 시작 검사에 걸린다 |
| Skill | 열 이름이 바뀌었다(`Id`, `Name` → `skillId`, `skillName` 등, 열 추가) | 빌드 사본에 쓰면 `JsonDataManager.LoadSkillData`, `SkillDataBase`가 읽지 못한다. 클라이언트 코드를 바꾸기 전에는 첫째 자리 변경이다 |

- AccountConst, BossAttack, DropItem, DropTable, Item, SpawnPattern, Wave는 시트와 서버·클라이언트 파일이 같다.
- 새 Monster 기준으로 Wave의 monsterId는 모두 Monster에 있고, BossAttack의 monsterId(13001)는 Boss다.
- Skill과 ItemConst는 서버 `data/`에 아직 없다. 처음 내보낼 때 추가된다.

## 5. 미정
- 저장본 폴더·파일 이름
- API를 호출할 클래스와 씬 위치, 서버 주소 설정 방법(로그인 연동과 함께 정한다)
- 요청 실패 시 재시도하거나 진행을 막을지
- 앱 업데이트 요구 화면
