# Force Push Syndrome — 프로젝트 규칙 (AI에게 가장 먼저 전달하는 파일)

탕탕특공대 스타일 로그라이크 모바일/WebGL 게임. 7조 "코딩특공대".
- `COU_Client/` : Unity 6000.3.24f1 (C#, Entities 1.4.8 + Burst)
- `COU_Server/` : Spring Boot 4.1.1 / Java 21 / JPA / MySQL 8.4 / Redis 7.4

## 0. 작업 순서 (반드시 지킨다)
1. 코드를 바꾸기 전에 관련 파일과 `Docs/WorkPlan/`의 해당 계획을 **먼저 읽는다**.
2. 변경 범위와 접근 방식을 짧게 제안하고, 설계 결정이 필요하면 **사람이 고르게 한다** (결정은 사람, 구현은 AI).
3. 구현 후 서버는 `./gradlew test`로 검증한다. Unity는 컴파일·실행을 AI가 할 수 없으므로 "확인하지 못했다"고 명시한다.
4. 코드나 Git 기록으로 확인되지 않은 내용은 사실처럼 쓰지 않는다 (문서·README 포함).

## 1. 서버 규칙 (COU_Server)
- **서버 권위 원칙**: 재화·보상·스테이지 클리어·전투 결과는 서버가 계산한다. 클라이언트 값은 입력일 뿐, 검증 없이 저장하지 않는다.
- **레이어 구조**: `controller → service → repository → entity`, 요청/응답은 `dto`, 도메인 예외는 패키지별 `exception`. Controller에 비즈니스 로직을 두지 않는다. Entity를 API 응답으로 직접 반환하지 않는다.
- **재화 변경은 `CurrencyLedgerService`만 통해서** 한다. 잔액을 직접 set 하지 않는다 (원장에 이유·증감이 남아야 한다).
- **동시성**: 같은 플레이어의 재화/인벤토리/상점/전투 보상 변경은 `findForUpdate`(PESSIMISTIC_WRITE)로 플레이어 행을 먼저 잠근 뒤 처리한다. 잠금 순서는 항상 `플레이어 → 하위 엔티티`.
- **Redis**: 랭킹(ZSET)·접속 상태(`presence:{id}`, TTL 60s)·로그인 제한 용도다. Redis 장애가 로그인/게임 진행을 막으면 안 된다 (fail-open). 랭킹 갱신은 `AFTER_COMMIT`에서 한다.
- **정적 데이터**: 밸런스 수치는 Google Sheet → `SheetExporter` → JSON → 메모리 Map 경로만 사용한다. 코드에 매직 넘버로 박지 않는다.
- **보안**: JWT 24h, 비밀번호 BCrypt. 비밀번호·토큰·`.env` 값·EC2 IP 같은 비밀/인프라 정보를 로그, 코드, 문서, AI 프롬프트에 넣지 않는다.
- **테스트**: 통합 테스트는 `IntegrationTestSupport` + Testcontainers(MySQL)를 쓴다. 새 API는 정상/권한/검증 실패/동시성 중 해당하는 케이스를 반드시 포함한다.

## 2. 클라이언트 규칙 (COU_Client)
- 서버 통신 상수(BASE_URL 등)는 `Assets/Scripts/Utils/GameConstants.cs`에서만 관리한다.
- **ECS는 드랍 아이템 처럼 개수가 많고 단순한 대상에만** 쓴다. SpriteRenderer/2D Renderer는 Entities Graphics와 호환되지 않으므로 렌더링은 풀링된 GameObject로 한다.
- **오브젝트 풀링 책임 분리**: `ObjectPoolManager`는 보관/활성화만 한다. 꺼낸 직후 초기화(`Init`)는 사용하는 쪽(예: `MonsterSpawner`)의 책임이고, 반납 시 정리(`OnDespawn`)는 반납하는 쪽이 호출해야 한다.
- `Update`에서 `Find`/`GetComponent`/LINQ/문자열 연결 등 할당을 만들지 않는다.
- `.meta` 파일, `Library/`, `Temp/`는 직접 수정하지 않는다. 씬/프리팹 YAML 직접 편집은 최후의 수단이다.

## 3. Git / 협업 규칙
- 커밋 메시지: `feat : 설명`, `fix : 설명`, `docs : 설명`, `refactor : 설명`, `test : 설명`.
- AI가 `git commit`/`git push`를 직접 실행하지 않는다. 커밋은 사람이 diff를 확인한 뒤 한다.
- 팀원 담당 영역을 고칠 때는 변경 전에 먼저 공유한다 (예: 풀링/스포너는 담당자와 협의).
- 문서·README에 팀원 작업을 쓸 때는 `git log --author`로 실제 작성자를 확인한다.

## 4. 자주 쓰는 명령
- 서버 테스트: `cd COU_Server && ./gradlew test`
- 서버 컴파일만: `cd COU_Server && ./gradlew compileJava`
- 로컬 DB/Redis: `cd COU_Server && docker compose up -d` (`.env` 필요, `.env.example` 참고)

## 5. 이 레포의 AI 설정 (`.claude/`)
- `skills/` : server-api-feature, currency-concurrency-check, unity-pooled-enemy
- `agents/` : concurrency-reviewer(읽기 전용 리뷰), api-spec-writer(명세서 작성)
- `hooks/`  : 비밀 파일 수정 차단, 위험 Git 명령 차단, 커밋 메시지 형식 검사, 서버 Java 수정 후 컴파일 검사
- `.mcp.json` : Notion(팀 문서/브로셔 조회)
