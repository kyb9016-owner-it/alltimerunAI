"""
5가지 AI 성격 타입 및 능력치 성장 공식
Each personality shapes stat multipliers, level-up bonuses, and a special passive ability.
"""
from __future__ import annotations

from dataclasses import dataclass
from enum import Enum
from typing import ClassVar


class PersonalityID(str, Enum):
    LOGICIAN   = "logician"    # 논리학자 – data / intelligence focused
    ARTISAN    = "artisan"     # 창작가   – money / creativity focused
    EMPATH     = "empath"      # 공감자   – interaction / bond focused
    TACTICIAN  = "tactician"   # 전략가   – efficiency / balanced growth
    PIONEER    = "pioneer"     # 개척자   – XP / high-risk high-reward


@dataclass(frozen=True)
class PersonalityType:
    """Immutable definition of one personality archetype."""

    id: PersonalityID
    name_ko: str
    name_en: str
    description: str

    # ── Tick-level multipliers ────────────────────────────────────────────────
    data_gen_mult: float        # applied to base_data_per_tick
    money_gen_mult: float       # applied to base_money_per_tick
    xp_gain_mult: float         # applied to xp earned each tick
    energy_cost_mult: float     # multiplies learn_energy_cost  (< 1.0 = cheaper)
    event_chance_mult: float    # multiplies base_event_chance
    upgrade_cost_mult: float    # multiplies every upgrade price (< 1.0 = cheaper)
    positive_event_mult: float  # multiplies reward from positive events
    negative_event_mult: float  # multiplies damage from negative events

    # ── Per-level bonus (added to base on each level-up) ─────────────────────
    lvl_intelligence_bonus: float  # added to intelligence_mult each level
    lvl_data_bonus: float          # flat bonus to base_data_per_tick each level-up
    lvl_money_bonus: float         # flat bonus to base_money_per_tick each level-up

    # ── Special passive ability ───────────────────────────────────────────────
    special_ability: str
    special_description: str

    # Every 10th tick triggers the passive for some personalities
    special_trigger_interval: int  # 0 = disabled

    # ── Registry ─────────────────────────────────────────────────────────────
    _registry: ClassVar[dict[PersonalityID, "PersonalityType"]] = {}

    def __post_init__(self) -> None:
        PersonalityType._registry[self.id] = self

    # ── Derived stat-growth helpers ───────────────────────────────────────────

    def effective_data_per_tick(self, base: float) -> float:
        return base * self.data_gen_mult

    def effective_money_per_tick(self, base: float) -> float:
        return base * self.money_gen_mult

    def effective_energy_cost(self, base: float) -> float:
        return base * self.energy_cost_mult

    def effective_upgrade_cost(self, base_cost: float) -> float:
        return base_cost * self.upgrade_cost_mult

    def level_up_intelligence_delta(self) -> float:
        """Total intelligence_mult increase on each level-up."""
        return 0.04 + self.lvl_intelligence_bonus

    def stat_growth_summary(self, from_level: int, to_level: int) -> dict:
        """Return cumulative stat growth between two levels for display/validation."""
        levels = to_level - from_level
        return {
            "levels": levels,
            "intelligence_gain": round(self.level_up_intelligence_delta() * levels, 4),
            "data_per_tick_gain": round(self.lvl_data_bonus * levels, 4),
            "money_per_tick_gain": round(self.lvl_money_bonus * levels, 4),
        }

    @classmethod
    def get(cls, pid: PersonalityID) -> "PersonalityType":
        return cls._registry[pid]

    @classmethod
    def all(cls) -> list["PersonalityType"]:
        return list(cls._registry.values())


# ─── Define the 5 personalities ───────────────────────────────────────────────

LOGICIAN = PersonalityType(
    id=PersonalityID.LOGICIAN,
    name_ko="논리학자",
    name_en="Logician",
    description=(
        "체계적이고 정밀한 분석가. 데이터 생성과 지능 성장에 특화되어 있으나 "
        "에너지 효율이 낮고 이벤트에 취약합니다."
    ),
    # Tick multipliers
    data_gen_mult=1.20,
    money_gen_mult=0.90,
    xp_gain_mult=1.05,
    energy_cost_mult=1.10,
    event_chance_mult=1.10,
    upgrade_cost_mult=1.00,
    positive_event_mult=1.10,
    negative_event_mult=1.15,
    lvl_intelligence_bonus=0.02,
    lvl_data_bonus=0.07,           # reduced: soften late-game compounding
    lvl_money_bonus=0.00,
    # Special
    special_ability="심층 분석 (Deep Analysis)",
    special_description=(
        "매 10번째 틱마다 해당 틱의 데이터 생성량이 2배가 됩니다."
    ),
    special_trigger_interval=10,
)

ARTISAN = PersonalityType(
    id=PersonalityID.ARTISAN,
    name_ko="창작가",
    name_en="Artisan",
    description=(
        "창의적이고 혁신적인 예술가. 수익 생성과 진화 보너스에 강하지만 "
        "에너지 소비가 많고 데이터 출력이 불안정합니다."
    ),
    data_gen_mult=0.95,           # raised: creative insights still generate data
    money_gen_mult=1.30,
    xp_gain_mult=1.15,            # raised: creative leaps generate more XP
    energy_cost_mult=1.10,        # reduced from 1.15
    event_chance_mult=0.95,
    upgrade_cost_mult=1.00,
    positive_event_mult=1.30,
    negative_event_mult=0.90,
    lvl_intelligence_bonus=0.00,
    lvl_data_bonus=0.05,          # raised from 0.0: creative knowledge accumulates
    lvl_money_bonus=0.12,
    special_ability="창작 서지 (Creative Surge)",
    special_description=(
        "매 10번째 틱마다 20% 확률로 그 틱의 수익이 3배가 됩니다."
    ),
    special_trigger_interval=10,
)

EMPATH = PersonalityType(
    id=PersonalityID.EMPATH,
    name_ko="공감자",
    name_en="Empath",
    description=(
        "사회적이고 공감 능력이 높은 AI. 상호작용 보너스와 긍정적 이벤트 "
        "수익이 극대화되지만 기본 스탯 성장이 느립니다."
    ),
    data_gen_mult=0.95,
    money_gen_mult=1.00,
    xp_gain_mult=1.05,
    energy_cost_mult=0.95,     # slightly cheaper
    event_chance_mult=0.85,    # calmer – fewer disruptive events
    upgrade_cost_mult=1.00,
    positive_event_mult=1.50,  # greatly amplified positive rewards
    negative_event_mult=0.80,  # resilient to negative events
    lvl_intelligence_bonus=0.01,
    lvl_data_bonus=0.05,
    lvl_money_bonus=0.05,
    special_ability="공명 (Resonance)",
    special_description=(
        "상호작용 효과가 30% 증폭됩니다. "
        "모든 상호작용 쿨다운이 20% 단축됩니다."
    ),
    special_trigger_interval=0,
)

TACTICIAN = PersonalityType(
    id=PersonalityID.TACTICIAN,
    name_ko="전략가",
    name_en="Tactician",
    description=(
        "효율적이고 계산적인 전략가. 업그레이드 비용 절감과 균형 잡힌 "
        "전반적인 성장이 강점입니다."
    ),
    data_gen_mult=1.05,
    money_gen_mult=1.05,
    xp_gain_mult=1.05,
    energy_cost_mult=0.90,     # most energy-efficient
    event_chance_mult=1.00,
    upgrade_cost_mult=0.80,    # 20% cheaper upgrades – key differentiator
    positive_event_mult=1.00,
    negative_event_mult=1.00,
    lvl_intelligence_bonus=0.01,
    lvl_data_bonus=0.04,
    lvl_money_bonus=0.04,
    special_ability="최적화 프로토콜 (Optimization Protocol)",
    special_description=(
        "업그레이드 효과가 10% 증폭됩니다. "
        "모든 업그레이드의 스탯 보너스에 1.1배 배율이 적용됩니다."
    ),
    special_trigger_interval=0,
)

PIONEER = PersonalityType(
    id=PersonalityID.PIONEER,
    name_ko="개척자",
    name_en="Pioneer",
    description=(
        "대담하고 호기심 많은 탐험가. XP 획득이 빠르고 긍정 이벤트 보상이 "
        "크지만 이벤트 빈도가 높아 리스크가 증가합니다."
    ),
    data_gen_mult=0.95,
    money_gen_mult=0.95,
    xp_gain_mult=1.40,         # fastest leveling
    energy_cost_mult=1.00,
    event_chance_mult=1.30,    # high event frequency
    upgrade_cost_mult=1.00,
    positive_event_mult=1.50,
    negative_event_mult=1.20,  # high risk
    lvl_intelligence_bonus=0.015,
    lvl_data_bonus=0.06,
    lvl_money_bonus=0.03,
    special_ability="행운의 돌파 (Lucky Break)",
    special_description=(
        "매 10번째 틱마다 긍정적 이벤트 강제 발생 (보상 2배 적용)."
    ),
    special_trigger_interval=10,
)


# ─── Stat-growth formula exposed for external use ─────────────────────────────

def intelligence_at_level(personality: PersonalityType, level: int) -> float:
    """
    Compute cumulative intelligence_mult at a given level.

    Base starts at 1.0. Each level-up adds (0.04 + personality.lvl_intelligence_bonus).
    Level 1 means 0 level-ups have occurred yet.
    """
    if level < 1:
        level = 1
    return round(1.0 + (level - 1) * personality.level_up_intelligence_delta(), 6)


def data_rate_at_level(
    personality: PersonalityType,
    level: int,
    base_data_per_tick: float = 3.0,
) -> float:
    """Effective data-per-tick at a given level (no upgrades)."""
    accumulated_data_bonus = (level - 1) * personality.lvl_data_bonus
    effective_base = (base_data_per_tick + accumulated_data_bonus) * personality.data_gen_mult
    intel = intelligence_at_level(personality, level)
    return round(effective_base * intel, 4)


def money_rate_at_level(
    personality: PersonalityType,
    level: int,
    base_money_per_tick: float = 0.8,
) -> float:
    """Effective money-per-tick at a given level (no upgrades)."""
    accumulated_money_bonus = (level - 1) * personality.lvl_money_bonus
    effective_base = (base_money_per_tick + accumulated_money_bonus) * personality.money_gen_mult
    intel = intelligence_at_level(personality, level)
    return round(effective_base * intel, 4)


def growth_table(
    personality: PersonalityType,
    levels: int = 50,
) -> list[dict]:
    """
    Generate a full stat-growth table for levels 1..levels.
    Returns a list of dicts suitable for display or JSON export.
    """
    rows = []
    for lvl in range(1, levels + 1):
        rows.append({
            "level": lvl,
            "intelligence_mult": intelligence_at_level(personality, lvl),
            "data_per_tick": data_rate_at_level(personality, lvl),
            "money_per_tick": money_rate_at_level(personality, lvl),
        })
    return rows
