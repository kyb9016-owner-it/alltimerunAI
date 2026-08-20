# Monetization Plan

## 1) Elevator Pitch
Train and evolve a cozy AI Buddy through short run sessions, where monetization improves comfort and speed but never blocks fair progression.

## 2) Fixed Parameters

| Category | Value |
|---|---|
| Session length | 3 to 6 min (target avg 4.5 min) |
| Interstitial | End-of-run only, 150s cooldown, max 2 per session, max 8 per day |
| Rewarded Revive | Optional, max 1 per run, max 6 per day (shared rewarded cap) |
| Rewarded x2 | Optional, max 1 per run, max 6 per day (shared rewarded cap) |
| Remove Ads | KRW 7,500 |
| Starter Pack (one-time) | KRW 4,400 |
| Weekly Value Pack | KRW 6,600 |
| First offer timing | Day 1, after 3rd run end |

## 3) 14-Day Scenario Table

| Day | Core Goal | Emotional Reward | Next-Day Hook | Monetization Touchpoint |
|---|---|---|---|---|
| 1 | Clear FTUE + first evolution shard | "I can grow this Buddy fast" | Trait branch unlock teaser | First offer after run 3: Starter Pack intro |
| 2 | Learn risk/reward route choices | "My choices matter" | Rare route appears tomorrow | Optional rewarded x2 on first successful run |
| 3 | First mini-boss run | "I overcame a spike" | New passive slot opens | Weekly Value Pack #1 + revive ad prompt on fail |
| 4 | Build preferred playstyle (speed or collector) | "This build feels like mine" | Style-specific mission chain | Remove Ads shown only if interstitial exposure >= 3 |
| 5 | Hit 3-run streak objective | "I am improving" | Buddy expression cosmetic unlock | Starter Pack reminder (soft, non-blocking) |
| 6 | Complete daily mission trio | "I can optimize short sessions" | Weekend challenge preview | Rewarded ad CTA after mission completion |
| 7 | First major evolution milestone | "My Buddy transformed" | New chapter with fresh hazards | Weekly Value Pack #2 + chapter pass teaser |
| 8 | Adapt to new obstacle set | "I can relearn and master" | Dual-reward node appears | Remove Ads reminder for high-ad viewers only |
| 9 | Resource efficiency challenge | "Smart play beats grind" | Upgrade cap increase tomorrow | Rewarded x2 offered on high-skill completion |
| 10 | Mid-cycle event mission | "Limited event, low pressure" | Bonus story scene preview | Weekly Value Pack #3 (no countdown pressure text) |
| 11 | Precision run objective | "I can execute cleanly" | Advanced trait branch unlock | Optional revive for near-miss failures |
| 12 | Team quest (light async social) | "I contributed with others" | Shared goal payout tomorrow | Cosmetic bundle cross-sell only |
| 13 | Pre-finale mastery chain | "I am ready for finale" | Finale prep rewards | Soft starter replacement pack (if not purchased) |
| 14 | Finale run + recap rewards | "I completed an arc" | New 14-day arc preview | Weekly Value Pack refresh + no hard gate |

## 4) Core Loop

### 10-Minute Loop
Run Start -> choose route risk -> collect/avoid -> fail or clear -> claim rewards -> Buddy upgrade choice -> queue next run.

### 3-Day Meta Loop
Day N onboarding objective -> Day N+1 build specialization -> Day N+2 milestone unlock + soft economy spike -> reset with new variant objective.

## 5) Monetization Ladder (Free -> Low -> Mid)

| Stage | Offer | Trigger | Value Promise | Free Alternative |
|---|---|---|---|---|
| Free | Rewarded Revive / x2 | Fail near checkpoint or successful run completion | Recovery and faster progression | Retry run, daily missions, weekly goals |
| Low | Starter Pack (KRW 4,400) | After first upgrade and first fail-recovery learning moment | Early friction reduction + cosmetic identity | 2-3 day free grind path with same core power ceiling |
| Mid | Remove Ads (KRW 7,500) | After user has seen >= 3 interstitials and played >= 2 sessions | Convenience and uninterrupted flow | Keep capped interstitial policy |
| Mid | Weekly Value Pack (KRW 6,600) | D3/D7/D10 milestone completions | Time-save + curated resource bundle | Earn equivalent resources by mission chain over week |

## 6) Event Trigger Spec

| Trigger Window | Condition | Surface | Message Rule |
|---|---|---|---|
| FTUE (D1 run 3 end) | Tutorial complete and first reward claimed | Starter Pack modal | "Speed up setup", no loss framing |
| D1 retention | First return within 24h | Lightweight mission popup | Focus on progression continuity |
| D3 retention | First mini-boss clear/fail | Weekly Value Pack #1 | Highlight optional efficiency, no urgency language |
| D7 retention | First major evolution complete | Weekly Value Pack #2 | Celebrate milestone first, offer second |
| High ad exposure | Interstitial seen >= 3 and no remove_ads purchase | Remove Ads card | Convenience framing only |

## 7) Fallback and Safety Rules

- If rewarded ad is unavailable/fails/cancels, give one fallback: small currency grant + immediate retry access.
- Never block core retry flow behind ads or IAP.
- Purchase cancel/error returns user to same gameplay context with no penalty.
- Interstitial disabled during first 2 runs of each day.
- No sequential full-screen ads.
- No pay-to-win stat sales in competitive contexts.

## 8) Risk Checklist (8)

1. Daily ad impressions under hard cap (rewarded + interstitial).
2. No offer shown more than once in a 10-minute window.
3. All paid resources have documented free earn path.
4. Offer copy avoids "last chance", "fall behind", or guilt framing.
5. Fail state is recoverable without spending.
6. Purchase decline does not reduce reward tables.
7. New users are not shown interstitial before run 3.
8. Ad/IAP technical failures always resolve to playable state.

## 9) Next Step: A/B Tests

| Test ID | Variant A | Variant B | Success KPI | Guardrail |
|---|---|---|---|---|
| AB-01 Price | Starter KRW 4,400 | Starter KRW 5,500 | D3 payer conversion | D1 retention drop < 1.5pp |
| AB-02 Cooldown | Interstitial 150s | Interstitial 180s | ARPDAU | Session length drop < 3% |
| AB-03 First Offer Timing | D1 run 3 end | D1 run 5 end | Offer CTR and purchase rate | Tutorial completion drop < 1pp |

## 10) Next Step: Execution Plan

1. Implement runtime caps and fallback state machine from `docs/MONETIZATION_STATES.yaml`.
2. Instrument analytics: `offer_impression`, `offer_click`, `purchase_start`, `purchase_result`, `ad_attempt`, `ad_result`.
3. Ship to 10% cohort with AB-01 only for 3 days.
4. Review D1 retention, ARPDAU, ad error rate; proceed to AB-02 and AB-03 only if guardrails pass.
