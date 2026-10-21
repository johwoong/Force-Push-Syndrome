---
name: server-api-feature
description: COU_Server에 새 REST API/기능을 추가할 때 사용. 패키지 레이어 구조, DTO 검증, 서버 권위 검증, 테스트 작성까지 이 프로젝트 방식으로 진행한다.
---

# 서버 API 기능 추가 절차

1. **읽기**: `COU_Server/Docs/WorkPlan/`에서 관련 계획, 같은 도메인 패키지(`com.couserver.<domain>`)의 기존 controller/service/dto를 먼저 읽고 스타일을 맞춘다.
2. **제안**: 엔드포인트(메서드·경로), 요청/응답 DTO, 필요한 DB 변경, 잠금 필요 여부를 5줄 이내로 정리해 사람에게 확인받는다.
3. **구현 순서**: entity/repository → service(트랜잭션 경계, 서버 권위 검증) → dto(`@Valid` 제약) → controller(얇게).
4. **필수 체크**
   - 인증: JWT 주체의 playerId만 사용한다. 요청 바디의 playerId를 신뢰하지 않는다.
   - 재화 변경이 있으면 `CurrencyLedgerService`를 사용하고 `currency-concurrency-check` 스킬로 점검한다.
   - 예외는 도메인 `exception` 패키지의 타입으로 던지고 공통 핸들러 응답 형식을 따른다.
5. **테스트**: `IntegrationTestSupport`를 상속해 정상 / 인증 실패 / 검증 실패 / (해당 시) 동시 요청 케이스를 작성하고 `./gradlew test`로 확인한다.
6. **문서**: API 명세서에 요청/응답 예시와 에러 코드를 추가한다 (`api-spec-writer` 에이전트에 위임 가능).
7. **마무리**: 변경 파일 목록과 확인하지 못한 항목을 보고한다. 커밋은 하지 않는다.
