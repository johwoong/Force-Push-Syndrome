# 드롭아이템 성능 보고서

WorkPlan 01(드롭아이템) #12 성능 검증 결과다.

## 측정 환경
| 항목 | 값 |
|---|---|
| Unity | 6000.3.24f1, URP 2D Renderer |
| Entities | 1.4.8 |
| 실행 | 에디터 Play Mode, `DropItemTestScene` |
| 프레임 | 약 30fps(33ms). 에디터 창이 포커스되지 않은 상태로 `Application.runInBackground = true`를 두어 스로틀링됐다 |
| 잡 워커 수 | 19 |
| 조작 | Unity CLI `eval`로 Spawn, 자석, 튜닝값 변경 |
| 수집 | `ProfilerRecorder`(600프레임 버퍼). 구간마다 약 240~300프레임을 기록했다 |

**측정 대상 마커**
- 시스템(메인 스레드): `Default World LootDetectionSystem`, `Default World AttractMovementSystem`
- 잡(전체 스레드 합): `LootDetectionJob (Burst)`, `AttractMovementJob (Burst)`, `DropItemManager:ViewSyncJob (Burst)`
- 병렬 분배 확인용: 위 잡 마커를 `CollectOnlyOnCurrentThread`로 한 번 더 기록해 메인 스레드 몫만 따로 수집했다
- 매니저: `Update.ScriptRunBehaviourUpdate`, `PreLateUpdate.ScriptRunBehaviourLateUpdate`. 모든 MonoBehaviour 합계이며, 씬에는 매니저와 테스트 드라이버만 있다
- 메모리·렌더링: `GC Allocated In Frame`, `Batches Count`, `SetPass Calls Count`

## 시나리오
| 구간 | 내용 |
|---|---|
| A. 기준값 | 드롭 0개 |
| B. Grounded 5000 | 플레이어에서 5~25 거리의 링에 잼·골드 8종 5000개를 스폰했다. 루팅거리(2) 밖이다 |
| C. 자석 흡수 | Grounded 5000(잼 2500 + 골드 2500) 상태에서 `AttractAllExpGems()`를 호출해 잼 2500개를 흡수했다. 테스트 드라이버의 OnLoot 로그는 껐다. 흡수가 보이도록 추적 속도를 낮췄다(ChaseStartSpeed 1, ChaseAcceleration 3) |

## 결과
시간 단위는 ms이고, 값은 **전체 프레임 평균 / 최대**다.

| 항목 | A. 드롭 0 | B. Grounded 5000 | C. 자석 흡수 |
|---|---|---|---|
| Main Thread 프레임 | 33.06 / 35.27 | 33.12 / 36.36 | 35.00 / 52.80 |
| LootDetectionSystem (메인) | 0.002 / 0.029 | 0.003 / 0.016 | 0.004 / 0.017 |
| LootDetectionJob (Burst) | 0 | 0.039 / 0.256 | 0.031 / 0.125 |
| AttractMovementSystem (메인) | 0.003 / 0.017 | 0.003 / 0.029 | 0.019 / 0.123 |
| AttractMovementJob (Burst) | 0 | 0 | 0.029 / 0.198 |
| └ 메인 스레드 몫 | - | - | 0.004 / 0.049 |
| ViewSyncJob (Burst) | 0 | 0 | 0.117 / 0.917 |
| └ 메인 스레드 몫 | - | - | 0.045 / 0.247 |
| Update 전체 | 0.051 / 0.17 | 0.060 / 0.18 | 0.058 / 0.16 |
| LateUpdate 전체 | 0.051 / 0.24 | 0.050 / 0.17 | 0.58 / 3.91 |
| GC Allocated In Frame (중앙값 / 최대) | 14,793 B / 97 KB | 14,793 B / 1.86 MB | 14,793 B / 1.98 MB |
| Batches (중앙값) | 2 | 1,568 | 1,367 |
| SetPass Calls (중앙값) | 2 | 10 | 10 |

**단발성 측정**
| 항목 | 값 |
|---|---|
| `Spawn` 5000회 연속 | 95.4ms(개당 약 19µs). 첫 생성이라 Instantiate가 포함된다 |
| `AttractAllExpGems()` 호출(잼 2500) | 0.11~0.12ms |
| 흡수 중 아이템 없이 Spawn 5000 | `GetComponentOrderVersion<Attract>()`가 0 → 0으로 그대로다. TransformAccessArray 재구성이 일어나지 않는다 |

## 분석

### 1. Grounded 비용: 목표 달성
- 드롭 0개와 Grounded 5000개 사이에 메인 스레드 비용 차이가 없다. 시스템 메인 비용은 0.003ms 수준이고, Update·LateUpdate도 변화가 없다.
- 감지 잡은 B 구간 301프레임 중 151프레임에서만 실행됐다. 30fps에서 15Hz다. 실행된 프레임의 워커 비용은 평균 약 0.077ms다.
- AttractMovementJob과 ViewSyncJob은 Attract 청크가 없으면 샘플이 0이다. 아키타입 분리 덕분에 이동 쿼리는 Grounded 청크를 순회하지 않는다.

### 2. GC
- 매 프레임 약 14.8KB가 잡히는데, 이는 에디터 자체 할당(Pipeline eval 서버, 인스펙터 등)이다. 드롭 0개일 때도 같은 값이 나온다.
- A·B·C 세 구간의 중앙값이 14,793B로 같다. 그래서 정상 상태에서 드롭 로직의 추가 할당은 0으로 판단한다.
- 최대값 스파이크(1.86~1.98MB)는 드롭 로직이 거의 돌지 않는 B 구간에서도 발생해 에디터 쪽 원인으로 보인다. 에디터 안에서는 분리할 수 없어 개발 빌드 Profiler로 재확인이 필요하다.

### 3. 자석 스파이크
- 호출 자체(청크 단위 AddComponent/RemoveComponent)는 2500개에 0.12ms로 작다.
- 흡수 구간의 최대 비용은 **LateUpdate 3.91ms**다. 한 프레임에 수백 개가 도달해 `OnLoot` + `ObjectPool.ReturnObject`(SetActive false) + 엔티티 파괴가 몰린 프레임이다.
- 테스트 드라이버의 OnLoot `Debug.Log`를 켜면 같은 구간 최대가 10.98ms로 오른다. 실제 플레이어 OnLoot에서 로그나 무거운 처리를 피해야 한다.
- 이 수치는 추적 속도를 낮춘 조건이다. 기본값(5 / 40)에서는 도달이 더 적은 프레임에 몰리므로 스파이크가 더 커질 수 있다.

### 4. 병렬 분배 / Burst
- ViewSyncJob의 전체 스레드 합은 0.117ms, 메인 스레드 몫은 0.045ms로, 약 60%가 워커에서 실행됐다. 뷰가 부모 없는 루트 오브젝트라서 IJobParallelForTransform이 분배된다.
- 세 잡 모두 `(Burst)` 마커에만 샘플이 있고, 관리 코드 마커(`LootDetectionJob` 등)는 0이다. 시스템 OnUpdate도 `Burst Jobs/Default World ...System` 마커로 등록돼 있다.
- Profiler Timeline에서 눈으로는 확인하지 않았다.

### 5. 렌더링
- Grounded 5000개에서 Batches가 약 1,568이고, 로직 비용과 별개로 가장 큰 비용원이다.
- 임시 스프라이트가 서로 다른 텍스처 4종(IsometricDiamond, Circle, Square, Capsule 등)을 쓰고, 뷰가 무작위 위치에 섞여 있어 배칭이 끊긴다.
- 정식 리소스를 하나의 SpriteAtlas로 묶은 뒤 다시 측정해야 한다.

## 측정 한계
- 에디터 Play Mode 측정이다. 에디터 오버헤드와 30fps 스로틀링이 섞여 있어 절대값보다 구간 간 비교가 의미 있다.
- ProfilerRecorder의 합산 값이라 Timeline 상의 스레드별 배치나 대기 시간은 보지 못했다.
- 모바일 기기 측정은 하지 않았다.

## 후속 권장
1. 개발 빌드(가능하면 모바일 기기)에서 Profiler로 GC 0과 Timeline을 재확인한다.
2. 기본 튜닝값으로 자석 흡수 구간의 LateUpdate 최대값을 측정한다. 스파이크가 문제가 되면 도달 처리의 프레임당 상한이나 풀 반환 방식을 검토한다.
3. SpriteAtlas 도입 후 Batches·SetPass를 재측정한다.
4. 대량 드롭 시 Spawn 비용(개당 약 19µs)과 풀 선생성 부재에 따른 첫 Instantiate 스파이크를 실제 전투 흐름에서 측정한다.
