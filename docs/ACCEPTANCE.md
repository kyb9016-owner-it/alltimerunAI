# Scope
- Mobile 2D casual game based on: [PROJECT] Mobile Idle Strategy Game / Unity 3D MVP
Title (temp): AI를 키운 건 나인데
Core concept: AI grows from JARVIS-like Core to ProtoHuman hologram via evolution. Strategy-focused (resources/optimization/risk). No emotion systems in MVP.

[TICK]
- tickInterval = 1s
- Game loop per tick: AI Process -> Resource Process -> Event check -> Evolution check -> UI refresh (MVP can refresh per tick)

[RESOURCES]
Energy(E), Data(D), Money(M)
Start: E=50, D=0, M=0
Energy production: E += EnergyGenPerTick (base 2.0)

[AI LEARNING / PRODUCTION]
If E >= LearnEnergyCost (base 1.5):
- E -= LearnEnergyCost
- DataGenerated = BaseDataPerTick(3.0) * IntelligenceMult * (1 + OptimizationBonus)
- D += DataGenerated
- MoneyGenerated = BaseMoneyPerTick(0.8) * IntelligenceMult
- M += MoneyGenerated
- XP += DataGenerated * 0.25

[LEVELING]
XPToLevel = 50 + (Level-1)*25
On level up:
- Level++
- IntelligenceMult += 0.04 (per level +4%)

[EVENTS]
Base event chance per tick: p=0.03
Stability reduces chance:
pFinal = p * (1 - 0.25 * clamp01((Stability-1)/3))
MVP events examples:
- ServerOverheat: E -20
- SecurityBreach: M -50 (Firewall reduces dmg)
- EfficiencyBoost: IntelligenceMult +0.05
(Use stability/firewall upgrades to mitigate risk)

[EVOLUTION MVP]
Only 1 evolution: Core -> ProtoHuman
Trigger condition (MVP tuned for 30~40min):
- Level >= 10
- Data >= 2500
- Money >= 1500
On evolve:
- stage = ProtoHuman
- IntelligenceMult *= 1.25
- event chance +10% (risk up)
Visual sequence 5~7s: slowmo -> emissive/particles up -> overlay text -> glitch(optional) -> crossfade models -> SFX -> resume

[OFFLINE EARNINGS]
Save lastTimestampUtc
On load:
offlineSeconds = clamp(now-lastTimestampUtc, 0, 8h)
offlineMultiplier = 0.6
Compute net rates (simplified):
D += NetDataPerSec * offlineSeconds * 0.6
M += NetMoneyPerSec * offlineSeconds * 0.6
Energy restore MVP option: set E = Emax on load (simple, good UX)

[UPGRADE TREE MVP]
3 categories, each up to Lv5 (use ScriptableObject recommended):
Power:
- Generator: +0.6 EnergyGenPerTick/level; cost M: 30,80,160,300,500
- Battery: +10 Emax/level; cost M: 20,60,120,220,380
Learning:
- ModelTraining: +0.8 BaseDataPerTick/level; cost M: 40,120,250,450,700
- Optimization: +0.06 OptimizationBonus/level; cost M: 60,160,330,600,900
Ops:
- Stability: +0.5 Stability/level; cost M: 50,140,300,520,800
- Firewall: -10% security dmg/level; cost M: 50,140,300,520,800

[SCENES]
Boot -> Game (+ optional DevTest)
Game scene hierarchy:
Systems(GameManager/Resource/AI/Event/Evolution/Upgrade/Save)
World(Room + AIVisualRoot(CorePrefab, ProtoHumanPrefab))
UI(HUD: topbar, upgrade panel, event popup, evolution overlay, log)
Audio(BGM/SFX)

[SAVE]
JSON in Application.persistentDataPath
Save on pause/quit + autosave every 30s.

[GOAL]
MVP playable 30~40 minutes, 1 evolution experience, idle/offline works, basic upgrades/events/stats functional.
- Core loop: start run -> interact/collect -> fail/success -> rewards -> retry.
- Monetization: rewarded ads, interstitial ads, and IAP.
- Deliverables: task plan, UI specs, art/story/QA docs, and first-task patch.

# Non-Goals
- Backend live service and account systems.
- Final economy balancing and full content production.
- Platform store submission assets.

# Quality Gates
- Build: no syntax/runtime errors in generated scripts and docs.
- Test: smoke checks for first loop and failure/retry flow.
- Lint: YAML/JSON parse successfully.
- Secret-scan: no API keys or secrets in generated files.

# Done Checklist
- [ ] tasks are dependency-consistent.
- [ ] UI flows/screens/components exist and are coherent.
- [ ] art direction + asset list exist.
- [ ] story bible + dialogue pack exist.
- [ ] QA plan/test cases/release checklist exist.
- [ ] first task patch file exists and can be reviewed/applied.
