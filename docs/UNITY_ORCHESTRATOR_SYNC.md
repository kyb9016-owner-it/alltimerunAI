# Unity ↔ 오케스트레이터 연동 스펙

Unity 클라이언트는 디바이스에서 단독 실행되므로, **런타임에 Python과 동기화하지 않고** Unity 내부에서 오케스트레이터와 **동일 규칙**을 재현하는 것이 목표입니다.

---

## 1. 상태 매핑

| Unity `ViewState` | 오케스트레이터 `RunState` | 비고 |
|-------------------|---------------------------|------|
| Home              | HOME                      | 대기, Start Run 전 |
| Run               | RUNNING                   | 틱 루프 진행 중 |
| (Run 내 Fail)     | FAILED                    | Unity는 Stop 시 바로 Result로 전환 가능 |
| Result            | RESULT                    | 정산 후 Retry/Home |
| Shop              | (없음)                    | Unity 전용, 몬티는 별도 플로우 |

---

## 2. 필드/저장 데이터 매핑

| 의미           | Unity (SaveData / 필드)     | 오케스트레이터 (GameSession)   |
|----------------|-----------------------------|--------------------------------|
| 에너지         | energy, energyMax           | energy, energy_max             |
| 데이터         | data                        | data                           |
| 돈             | money                       | money                          |
| 레벨           | level                       | level                          |
| 경험치(현재)   | xpInLevel                   | xp                             |
| 인텔 배율      | intelligenceMult            | intelligence_mult              |
| 최적화 보너스  | optimizationBonus           | optimization_bonus              |
| 이벤트 배율    | eventChanceMultiplier       | event_risk_mult                |
| 안정성         | stability                   | stability                      |
| 방화벽 레벨    | firewallLevel               | firewall_level                 |
| 진화 여부      | isProtoHuman                | is_proto_human, stage          |
| 업그레이드 Lv   | generatorLevel, batteryLevel, … | generator_level, battery_level, … |
| 점수/코인      | sessionScore, bestScore, coins, lastReward, retryCount | score, best_score, coins, last_reward, retry_count |
| 오프라인 기준  | lastTimestampUtc           | last_exit_timestamp_utc       |

---

## 3. 반드시 맞출 상수 (Single Source of Truth)

아래 값은 `orchestrator/game_state.py` 및 `docs/ACCEPTANCE.md`와 **동일**하게 유지합니다.

| 상수 | 값 | 비고 |
|------|-----|------|
| 틱 간격 | 1초 | TickInterval = 1.0f |
| 오프라인 최대 시간 | 8시간 | 8 * 3600 초 |
| 오프라인 배율 | 0.6 | D/M 적립량 = rate * offlineSec * 0.6 |
| 에너지 회복(재접속) | E = Emax | 로드 시 에너지 풀 충전 |
| 에너지 생성/틱 | 2.0 | base |
| 학습 에너지 비용 | 1.5 | base |
| 기본 데이터/틱 | 3.0 | base |
| 기본 돈/틱 | 0.8 | base |
| 기본 이벤트 확률 | 0.03 | base |
| 진화 조건 (MVP) | Level≥10, Data≥2500, Money≥1500 | Core → ProtoHuman |
| 진화 후 INT 배율 | ×1.25 | intelligence_mult *= 1.25 |
| 진화 후 이벤트 위험 | ×1.10 | event_risk_mult *= 1.10 |
| 업그레이드 최대 레벨 | 5 | 모든 업그레이드 공통 |

### 업그레이드 비용 (Money)

- Generator: 30, 80, 160, 300, 500  
- Battery: 20, 60, 120, 220, 380  
- ModelTraining: 40, 120, 250, 450, 700  
- Optimization: 60, 160, 330, 600, 900  
- Stability / Firewall: 50, 140, 300, 520, 800  

### 이벤트

- ServerOverheat: Energy -20  
- SecurityBreach: Money -50 × (1 - firewall×0.1)  
- EfficiencyBoost: IntelligenceMult +0.05  

---

## 4. XP 테이블 (선택)

- **Unity MVP**: 단순 공식 `50 + (level - 1) * 25` (ACCEPTANCE 초안과 동일).  
- **오케스트레이터**: `orchestrator/level_table.py`의 tiered 테이블 (L1–10: 100+(L-1)*60 등).  
- **완전 동기화**가 필요하면 Unity도 tiered 테이블을 C#으로 이식하거나, 레벨업 요구치만 JSON/상수로 로드하도록 확장할 수 있음.

---

## 5. 오프라인 보상 로직 (로드 시 1회)

1. `lastTimestampUtc`(저장값)과 현재 UTC 시간으로 `offlineSeconds = clamp(now - lastTimestampUtc, 0, 8*3600)` 계산.  
2. 패시브 데이터/돈 생성률(에너지 충분 시 1틱당 D, M) × offlineSeconds × 0.6 만큼 D, M 가산.  
3. Energy = EnergyMax 로 설정.  
4. (선택) 적용 후 `lastTimestampUtc`는 저장 시점에만 갱신하고, 오프라인 적용 여부는 플래그나 “이미 적용됨”으로 처리.

Unity `AiPetGamePrototype`의 `ApplyOfflineEarnings` / `LoadState`가 위와 일치하는지 확인하면 됩니다.

---

## 6. 몬티/광고·IAP

- 인터스티셜: 런 종료 후, 150초 쿨다운, 일 8회·세션 2회 캡 등은 `docs/MONETIZATION_STATES.yaml` 및 `MonetizationRuntime`과 일치해야 함.  
- Unity에서 광고/결제 SDK 연동 시, **캡과 상태 전이**는 위 스펙 또는 `monetization_runtime.py` 로직을 C#으로 이식해 사용 권장.

---

## 7. 다음 단계

- [ ] Unity에서 XP를 tiered 테이블로 바꿀지 결정 (밸런스 통일 시).  
- [ ] 몬티 캡(일 8회 등)을 Unity에 상수/테이블로 두고, `MONETIZATION_STATES.yaml`과 주기적으로 비교.  
- [ ] Figma HUD(JARVIS)와 연동 시, 표시할 E/D/M/Level/Stage 등은 위 필드명과 동일하게 맞추면 연동 문서화가 쉬움.
