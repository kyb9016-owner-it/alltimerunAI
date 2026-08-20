# 다음 구현 로드맵

현재 완료된 것: 오케스트레이터(game_state, monetization_runtime, level_table, personality, interaction), QA 테스트 47개, Unity 프로토타입(Home/In-Run/Result), Figma HUD 스펙.

## 우선순위

| 순서 | 항목 | 설명 | 산출물 |
|------|------|------|--------|
| 1 | **오프라인 보상** | ACCEPTANCE [OFFLINE EARNINGS]: 재접속 시 8시간 cap, 0.6 배율로 D/M 적립, E=Emax 복구 | orchestrator 오프라인 로직 + 테스트 |
| 2 | **Unity–오케스트레이터 연동** | 게임 상태/티크를 Python과 동기화하거나 Unity 내부에서 동일 규칙 재현 | ✅ 연동 스펙 + C# 상수 계약 완료 |
| 3 | **Figma HUD ↔ Unity** | 웹뷰/React HUD를 Unity에서 표시하거나, Unity UI로 동일 레이아웃 구현 | JarvisHudController 연동 문서/코드 |
| 4 | **몬티 캡/상태 검증** | MONETIZATION_STATES.yaml 캡(일 8회 인터스티셜 등)이 런타임과 일치하는지 테스트 보강 | qa 테스트 추가 |
| 5 | **BALANCE_MATRIX / PLAYTEST** | docs/BALANCE_MATRIX.yaml 확장, qa/PLAYTEST_SCRIPT.md 시나리오 추가 | 수정된 YAML/MD |

## 이번에 진행한 것 (Step 3)

- **오프라인 보상**: `GameSession.last_exit_timestamp_utc`, `apply_offline_earnings(now_utc)` 추가 및 테스트 2개 추가.
- **Unity–오케스트레이터 연동**: `docs/UNITY_ORCHESTRATOR_SYNC.md` (상태/필드/상수 매핑), `unity/Assets/Scripts/GameConstants.cs` (동기화용 상수) 추가.
