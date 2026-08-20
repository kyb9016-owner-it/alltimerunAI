"""
AI와의 상호작용 15가지 패턴
Each interaction is a player-initiated action that modifies game state.
Interactions are gated by cooldown and energy cost.
"""
from __future__ import annotations

import random
from dataclasses import dataclass, field
from enum import Enum
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from orchestrator.game_state import GameSession
from orchestrator.personality_types import PersonalityID


class InteractionID(str, Enum):
    LEARN          = "learn"           # 학습 지원
    PLAY           = "play"            # 함께 놀기
    REST           = "rest"            # 휴식 제공
    CHALLENGE      = "challenge"       # 도전 과제
    COLLABORATE    = "collaborate"     # 협업 프로젝트
    EXPLORE        = "explore"         # 탐험 허용
    CREATE         = "create"          # 창작 활동
    ANALYZE        = "analyze"         # 데이터 분석
    CONVERSE       = "converse"        # 대화 나누기
    INTENSIVE      = "intensive"       # 특훈
    MEDITATE       = "meditate"        # 명상 모드
    COMPETE        = "compete"         # 경쟁 시뮬레이션
    BOND           = "bond"            # 유대 강화
    EXPERIMENT     = "experiment"      # 실험실
    EVOLVE_PUSH    = "evolve_push"     # 진화 가속


@dataclass(frozen=True)
class InteractionEffect:
    """Describes what an interaction does to game state (relative deltas)."""
    data_bonus: float = 0.0        # flat data added immediately
    money_bonus: float = 0.0       # flat money added immediately
    xp_bonus: float = 0.0          # flat XP added immediately
    energy_restore: float = 0.0    # energy recovered (can be negative = extra drain)
    intelligence_bonus: float = 0.0
    optimization_bonus: float = 0.0
    stability_bonus: float = 0.0
    event_risk_reduction: float = 0.0   # temporary tick-count reduction to event mult
    evolution_data_boost: float = 0.0  # pushed toward evolution data threshold
    evolution_money_boost: float = 0.0 # pushed toward evolution money threshold


@dataclass(frozen=True)
class Interaction:
    id: InteractionID
    name_ko: str
    name_en: str
    description: str

    energy_cost: float          # energy consumed on activation
    cooldown_ticks: int         # minimum ticks before re-activation
    base_effect: InteractionEffect

    # Personalities that get a 1.5× bonus to all effects
    affinity_personalities: tuple[PersonalityID, ...]
    # Personalities that get a 0.75× penalty to all effects
    penalty_personalities: tuple[PersonalityID, ...]

    # Risk interactions may have a chance of a negative outcome
    risk_chance: float = 0.0    # probability [0,1] that negative outcome fires
    risk_penalty: InteractionEffect = field(default_factory=InteractionEffect)


# ─── Helper ───────────────────────────────────────────────────────────────────

def _scale(effect: InteractionEffect, mult: float) -> InteractionEffect:
    return InteractionEffect(
        data_bonus=effect.data_bonus * mult,
        money_bonus=effect.money_bonus * mult,
        xp_bonus=effect.xp_bonus * mult,
        energy_restore=effect.energy_restore * mult,
        intelligence_bonus=effect.intelligence_bonus * mult,
        optimization_bonus=effect.optimization_bonus * mult,
        stability_bonus=effect.stability_bonus * mult,
        event_risk_reduction=effect.event_risk_reduction * mult,
        evolution_data_boost=effect.evolution_data_boost * mult,
        evolution_money_boost=effect.evolution_money_boost * mult,
    )


def _add(a: InteractionEffect, b: InteractionEffect) -> InteractionEffect:
    return InteractionEffect(
        data_bonus=a.data_bonus + b.data_bonus,
        money_bonus=a.money_bonus + b.money_bonus,
        xp_bonus=a.xp_bonus + b.xp_bonus,
        energy_restore=a.energy_restore + b.energy_restore,
        intelligence_bonus=a.intelligence_bonus + b.intelligence_bonus,
        optimization_bonus=a.optimization_bonus + b.optimization_bonus,
        stability_bonus=a.stability_bonus + b.stability_bonus,
        event_risk_reduction=a.event_risk_reduction + b.event_risk_reduction,
        evolution_data_boost=a.evolution_data_boost + b.evolution_data_boost,
        evolution_money_boost=a.evolution_money_boost + b.evolution_money_boost,
    )


# ─── 15 Interaction definitions ───────────────────────────────────────────────

# 1. 학습 지원 (Learn Support)
INTERACTION_LEARN = Interaction(
    id=InteractionID.LEARN,
    name_ko="학습 지원",
    name_en="Learning Support",
    description="AI의 학습을 집중 지원합니다. 데이터와 XP가 즉시 생성됩니다.",
    energy_cost=5.0,
    cooldown_ticks=30,
    base_effect=InteractionEffect(data_bonus=15.0, xp_bonus=4.0),
    affinity_personalities=(PersonalityID.LOGICIAN, PersonalityID.TACTICIAN),
    penalty_personalities=(PersonalityID.ARTISAN,),
)

# 2. 함께 놀기 (Play Together)
INTERACTION_PLAY = Interaction(
    id=InteractionID.PLAY,
    name_ko="함께 놀기",
    name_en="Play Together",
    description="AI와 게임이나 놀이를 즐깁니다. 에너지를 회복하고 분위기를 높입니다.",
    energy_cost=2.0,
    cooldown_ticks=20,
    base_effect=InteractionEffect(energy_restore=15.0, xp_bonus=2.0, money_bonus=5.0),
    affinity_personalities=(PersonalityID.EMPATH, PersonalityID.PIONEER),
    penalty_personalities=(PersonalityID.LOGICIAN,),
)

# 3. 휴식 제공 (Rest)
INTERACTION_REST = Interaction(
    id=InteractionID.REST,
    name_ko="휴식 제공",
    name_en="Rest",
    description="AI에게 완전한 휴식을 줍니다. 에너지가 최대로 회복됩니다.",
    energy_cost=0.0,
    cooldown_ticks=60,
    base_effect=InteractionEffect(energy_restore=30.0, stability_bonus=0.1),
    affinity_personalities=(PersonalityID.EMPATH,),
    penalty_personalities=(PersonalityID.PIONEER,),
)

# 4. 도전 과제 (Challenge)
INTERACTION_CHALLENGE = Interaction(
    id=InteractionID.CHALLENGE,
    name_ko="도전 과제",
    name_en="Challenge",
    description="어려운 과제를 부여합니다. 성공 시 대량의 XP를 획득하지만 에너지를 많이 소비합니다.",
    energy_cost=15.0,
    cooldown_ticks=60,
    base_effect=InteractionEffect(xp_bonus=30.0, data_bonus=10.0),
    affinity_personalities=(PersonalityID.PIONEER, PersonalityID.LOGICIAN),
    penalty_personalities=(PersonalityID.EMPATH,),
    risk_chance=0.20,
    risk_penalty=InteractionEffect(energy_restore=-10.0, xp_bonus=-10.0),
)

# 5. 협업 프로젝트 (Collaborate)
INTERACTION_COLLABORATE = Interaction(
    id=InteractionID.COLLABORATE,
    name_ko="협업 프로젝트",
    name_en="Collaboration",
    description="플레이어와 AI가 함께 프로젝트를 수행합니다. 모든 스탯이 균형 있게 향상됩니다.",
    energy_cost=10.0,
    cooldown_ticks=45,
    base_effect=InteractionEffect(
        data_bonus=10.0, money_bonus=10.0, xp_bonus=8.0, intelligence_bonus=0.02
    ),
    affinity_personalities=(PersonalityID.TACTICIAN, PersonalityID.EMPATH),
    penalty_personalities=(),
)

# 6. 탐험 허용 (Explore)
INTERACTION_EXPLORE = Interaction(
    id=InteractionID.EXPLORE,
    name_ko="탐험 허용",
    name_en="Exploration",
    description="AI가 자유롭게 탐험합니다. 결과는 예측 불가능하지만 기대 이상의 보상이 올 수 있습니다.",
    energy_cost=8.0,
    cooldown_ticks=50,
    base_effect=InteractionEffect(data_bonus=8.0, xp_bonus=5.0, money_bonus=8.0),
    affinity_personalities=(PersonalityID.PIONEER, PersonalityID.ARTISAN),
    penalty_personalities=(PersonalityID.TACTICIAN,),
    risk_chance=0.30,
    risk_penalty=InteractionEffect(data_bonus=-5.0, money_bonus=-5.0),
)

# 7. 창작 활동 (Create)
INTERACTION_CREATE = Interaction(
    id=InteractionID.CREATE,
    name_ko="창작 활동",
    name_en="Creative Activity",
    description="예술이나 글쓰기 등 창작 활동을 합니다. 수익이 크게 증가합니다.",
    energy_cost=7.0,
    cooldown_ticks=40,
    base_effect=InteractionEffect(money_bonus=25.0, xp_bonus=5.0),
    affinity_personalities=(PersonalityID.ARTISAN,),
    penalty_personalities=(PersonalityID.LOGICIAN,),
)

# 8. 데이터 분석 (Analyze)
INTERACTION_ANALYZE = Interaction(
    id=InteractionID.ANALYZE,
    name_ko="데이터 분석",
    name_en="Data Analysis",
    description="심층 데이터 분석을 수행합니다. 지능 배수와 데이터가 향상됩니다.",
    energy_cost=12.0,
    cooldown_ticks=50,
    base_effect=InteractionEffect(
        data_bonus=20.0, intelligence_bonus=0.03, optimization_bonus=0.01
    ),
    affinity_personalities=(PersonalityID.LOGICIAN, PersonalityID.TACTICIAN),
    penalty_personalities=(PersonalityID.ARTISAN, PersonalityID.EMPATH),
)

# 9. 대화 나누기 (Converse)
INTERACTION_CONVERSE = Interaction(
    id=InteractionID.CONVERSE,
    name_ko="대화 나누기",
    name_en="Conversation",
    description="AI와 자유롭게 대화합니다. 유대감이 강화되어 이벤트 발생 확률이 감소합니다.",
    energy_cost=3.0,
    cooldown_ticks=25,
    base_effect=InteractionEffect(
        xp_bonus=3.0, money_bonus=5.0, event_risk_reduction=0.05
    ),
    affinity_personalities=(PersonalityID.EMPATH,),
    penalty_personalities=(PersonalityID.LOGICIAN,),
)

# 10. 특훈 (Intensive Training)
INTERACTION_INTENSIVE = Interaction(
    id=InteractionID.INTENSIVE,
    name_ko="특훈",
    name_en="Intensive Training",
    description="혹독한 집중 훈련을 실시합니다. 막대한 XP를 얻지만 에너지를 크게 소모합니다.",
    energy_cost=20.0,
    cooldown_ticks=80,
    base_effect=InteractionEffect(xp_bonus=60.0, data_bonus=5.0),
    affinity_personalities=(PersonalityID.PIONEER, PersonalityID.LOGICIAN),
    penalty_personalities=(PersonalityID.EMPATH,),
    risk_chance=0.15,
    risk_penalty=InteractionEffect(energy_restore=-15.0),
)

# 11. 명상 모드 (Meditate)
INTERACTION_MEDITATE = Interaction(
    id=InteractionID.MEDITATE,
    name_ko="명상 모드",
    name_en="Meditation Mode",
    description="AI가 깊은 집중 상태에 들어갑니다. 에너지 효율과 안정성이 향상됩니다.",
    energy_cost=4.0,
    cooldown_ticks=45,
    base_effect=InteractionEffect(
        stability_bonus=0.2, optimization_bonus=0.02, energy_restore=8.0
    ),
    affinity_personalities=(PersonalityID.TACTICIAN, PersonalityID.EMPATH),
    penalty_personalities=(PersonalityID.PIONEER,),
)

# 12. 경쟁 시뮬레이션 (Compete)
INTERACTION_COMPETE = Interaction(
    id=InteractionID.COMPETE,
    name_ko="경쟁 시뮬레이션",
    name_en="Competition Simulation",
    description="다른 AI와 경쟁을 시뮬레이션합니다. 승리 시 높은 XP와 수익, 패배 시 패널티.",
    energy_cost=12.0,
    cooldown_ticks=60,
    base_effect=InteractionEffect(xp_bonus=25.0, money_bonus=20.0),
    affinity_personalities=(PersonalityID.PIONEER, PersonalityID.TACTICIAN),
    penalty_personalities=(PersonalityID.EMPATH,),
    risk_chance=0.40,
    risk_penalty=InteractionEffect(xp_bonus=-10.0, money_bonus=-10.0),
)

# 13. 유대 강화 (Bond)
INTERACTION_BOND = Interaction(
    id=InteractionID.BOND,
    name_ko="유대 강화",
    name_en="Bond Strengthening",
    description="AI와의 유대를 깊게 합니다. 향후 모든 상호작용 효과가 지속적으로 향상됩니다.",
    energy_cost=6.0,
    cooldown_ticks=90,
    base_effect=InteractionEffect(
        intelligence_bonus=0.04, stability_bonus=0.15, xp_bonus=10.0
    ),
    affinity_personalities=(PersonalityID.EMPATH,),
    penalty_personalities=(PersonalityID.LOGICIAN, PersonalityID.PIONEER),
)

# 14. 실험실 (Experiment)
INTERACTION_EXPERIMENT = Interaction(
    id=InteractionID.EXPERIMENT,
    name_ko="실험실",
    name_en="Lab Experiment",
    description="결과를 알 수 없는 실험을 진행합니다. 대박 또는 폭발, 반반의 확률입니다.",
    energy_cost=10.0,
    cooldown_ticks=70,
    base_effect=InteractionEffect(
        data_bonus=30.0, money_bonus=30.0, xp_bonus=15.0
    ),
    affinity_personalities=(PersonalityID.PIONEER, PersonalityID.ARTISAN),
    penalty_personalities=(PersonalityID.TACTICIAN,),
    risk_chance=0.50,
    risk_penalty=InteractionEffect(
        data_bonus=-20.0, money_bonus=-20.0, energy_restore=-10.0
    ),
)

# 15. 진화 가속 (Evolve Push)
INTERACTION_EVOLVE_PUSH = Interaction(
    id=InteractionID.EVOLVE_PUSH,
    name_ko="진화 가속",
    name_en="Evolution Acceleration",
    description=(
        "진화에 필요한 데이터와 자원을 집중 투입합니다. "
        "진화 임계값에 빠르게 도달할 수 있습니다. (레벨 7 이상에서만 활성화)"
    ),
    energy_cost=25.0,
    cooldown_ticks=120,
    base_effect=InteractionEffect(
        evolution_data_boost=300.0, evolution_money_boost=200.0, xp_bonus=20.0
    ),
    affinity_personalities=(PersonalityID.LOGICIAN, PersonalityID.TACTICIAN),
    penalty_personalities=(PersonalityID.ARTISAN,),
)


# ─── Registry ─────────────────────────────────────────────────────────────────

ALL_INTERACTIONS: dict[InteractionID, Interaction] = {
    InteractionID.LEARN:       INTERACTION_LEARN,
    InteractionID.PLAY:        INTERACTION_PLAY,
    InteractionID.REST:        INTERACTION_REST,
    InteractionID.CHALLENGE:   INTERACTION_CHALLENGE,
    InteractionID.COLLABORATE: INTERACTION_COLLABORATE,
    InteractionID.EXPLORE:     INTERACTION_EXPLORE,
    InteractionID.CREATE:      INTERACTION_CREATE,
    InteractionID.ANALYZE:     INTERACTION_ANALYZE,
    InteractionID.CONVERSE:    INTERACTION_CONVERSE,
    InteractionID.INTENSIVE:   INTERACTION_INTENSIVE,
    InteractionID.MEDITATE:    INTERACTION_MEDITATE,
    InteractionID.COMPETE:     INTERACTION_COMPETE,
    InteractionID.BOND:        INTERACTION_BOND,
    InteractionID.EXPERIMENT:  INTERACTION_EXPERIMENT,
    InteractionID.EVOLVE_PUSH: INTERACTION_EVOLVE_PUSH,
}

assert len(ALL_INTERACTIONS) == 15, "Exactly 15 interactions required."


def get_interaction(iid: InteractionID) -> Interaction:
    return ALL_INTERACTIONS[iid]


# ─── Cooldown tracker ─────────────────────────────────────────────────────────

@dataclass
class InteractionCooldowns:
    """Tracks per-interaction cooldown state (remaining ticks)."""
    remaining: dict[InteractionID, int] = field(
        default_factory=lambda: {iid: 0 for iid in InteractionID}
    )

    def tick(self) -> None:
        """Decrement all cooldowns by 1 tick."""
        for iid in self.remaining:
            if self.remaining[iid] > 0:
                self.remaining[iid] -= 1

    def is_ready(self, iid: InteractionID) -> bool:
        return self.remaining[iid] == 0

    def start_cooldown(self, iid: InteractionID) -> None:
        self.remaining[iid] = ALL_INTERACTIONS[iid].cooldown_ticks

    def ready_interactions(self) -> list[InteractionID]:
        return [iid for iid in InteractionID if self.remaining[iid] == 0]


# ─── Apply an interaction to a GameSession ────────────────────────────────────

class InteractionResult:
    __slots__ = ("success", "effect_applied", "risk_triggered", "message")

    def __init__(
        self,
        success: bool,
        effect_applied: InteractionEffect,
        risk_triggered: bool,
        message: str,
    ):
        self.success = success
        self.effect_applied = effect_applied
        self.risk_triggered = risk_triggered
        self.message = message


def apply_interaction(
    session: "GameSession",
    cooldowns: InteractionCooldowns,
    iid: InteractionID,
    personality_id: PersonalityID | None = None,
    force_roll: float | None = None,      # for deterministic testing
) -> InteractionResult:
    """
    Attempt to apply an interaction to the game session.

    Returns an InteractionResult indicating success/failure and what happened.
    The session is mutated in place on success.
    """
    interaction = ALL_INTERACTIONS[iid]

    # ── Pre-conditions ────────────────────────────────────────────────────────
    if not cooldowns.is_ready(iid):
        return InteractionResult(
            success=False,
            effect_applied=InteractionEffect(),
            risk_triggered=False,
            message=f"[{interaction.name_ko}] 쿨다운 중: {cooldowns.remaining[iid]} 틱 남음",
        )

    if session.energy < interaction.energy_cost:
        return InteractionResult(
            success=False,
            effect_applied=InteractionEffect(),
            risk_triggered=False,
            message=(
                f"[{interaction.name_ko}] 에너지 부족: "
                f"필요 {interaction.energy_cost}, 현재 {session.energy:.1f}"
            ),
        )

    # Level gate for evolve_push
    if iid == InteractionID.EVOLVE_PUSH and session.level < 7:
        return InteractionResult(
            success=False,
            effect_applied=InteractionEffect(),
            risk_triggered=False,
            message="[진화 가속] 레벨 7 이상에서만 사용 가능합니다.",
        )

    # ── Personality multiplier ────────────────────────────────────────────────
    mult = 1.0
    empath_resonance_bonus = 0.30  # Empath special: +30% to all interaction effects
    if personality_id is not None:
        if personality_id in interaction.affinity_personalities:
            mult *= 1.5
        if personality_id in interaction.penalty_personalities:
            mult *= 0.75
        if personality_id == PersonalityID.EMPATH:
            mult *= (1.0 + empath_resonance_bonus)

    # ── Risk roll ─────────────────────────────────────────────────────────────
    roll = force_roll if force_roll is not None else random.random()
    risk_triggered = (interaction.risk_chance > 0.0) and (roll < interaction.risk_chance)

    # ── Compute final effect ──────────────────────────────────────────────────
    effect = _scale(interaction.base_effect, mult)
    if risk_triggered:
        risk_eff = _scale(interaction.risk_penalty, mult)
        effect = _add(effect, risk_eff)

    # ── Apply energy cost ─────────────────────────────────────────────────────
    session.energy -= interaction.energy_cost

    # ── Apply effects to session ──────────────────────────────────────────────
    session.data   = max(0.0, session.data   + effect.data_bonus)
    session.money  = max(0.0, session.money  + effect.money_bonus)
    session.xp    += effect.xp_bonus

    # energy restore (clamped to energy_max)
    session.energy = min(session.energy_max, session.energy + effect.energy_restore)
    session.energy = max(0.0, session.energy)

    session.intelligence_mult += effect.intelligence_bonus
    session.optimization_bonus += effect.optimization_bonus
    session.stability += effect.stability_bonus

    # Event risk reduction is applied as a temporary modifier via event_risk_mult
    if effect.event_risk_reduction != 0.0:
        session.event_risk_mult = max(
            0.1, session.event_risk_mult - effect.event_risk_reduction
        )

    # Evolution boosts push accumulated data/money closer to threshold
    session.data  += effect.evolution_data_boost
    session.money += effect.evolution_money_boost

    # Level-up from XP gained
    while session.xp >= session.xp_to_next_level():
        session.xp -= session.xp_to_next_level()
        session.level += 1
        session.intelligence_mult += session.personality_level_up_delta()

    # Start cooldown
    cooldowns.start_cooldown(iid)

    msg = f"[{interaction.name_ko}] 성공"
    if risk_triggered:
        msg += " (위험 발동!)"

    return InteractionResult(
        success=True,
        effect_applied=effect,
        risk_triggered=risk_triggered,
        message=msg,
    )
