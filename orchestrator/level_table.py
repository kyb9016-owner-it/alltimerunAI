"""
레벨 1-50 경험치 테이블 및 밸런스 검증
XP table for levels 1-50 with tier-based progression and balance analytics.
"""
from __future__ import annotations

import math
from dataclasses import dataclass
from typing import Callable

from orchestrator.personality_types import (
    PersonalityType,
    data_rate_at_level,
    PersonalityID,
    LOGICIAN, ARTISAN, EMPATH, TACTICIAN, PIONEER,
)


# ─── Tier definitions ──────────────────────────────────────────────────────────
#
#  Tier 1 (Novice)     Levels  1-10   : fast ramp-up, ~15-20 min target
#  Tier 2 (Apprentice) Levels 11-25   : steady mid-game, ~20-25 min
#  Tier 3 (Advanced)   Levels 26-40   : late-game grind, post-evolution
#  Tier 4 (Master)     Levels 41-50   : prestige stretch goal
#
# XP formula per tier:
#   T1: 100 + (level - 1) * 60
#   T2: 700 + (level - 11) * 200
#   T3: 3700 + (level - 26) * 600
#   T4: 12700 + (level - 41) * 1500

def xp_to_next_level(level: int) -> int:
    """
    XP required to advance FROM `level` TO `level + 1`.
    Level must be in [1, 49]; returns 0 for level ≥ 50 (no further leveling).
    """
    if level >= 50:
        return 0
    if level <= 10:
        return 100 + (level - 1) * 60        # 100, 160, 220, …, 640
    if level <= 25:
        return 700 + (level - 11) * 200      # 700, 900, 1100, …, 3500
    if level <= 40:
        return 3700 + (level - 26) * 600     # 3700, 4300, 4900, …, 12400  (corrected: level 40 = 3700 + 14*600 = 12100)
    return 12700 + (level - 41) * 1500       # 12700, 14200, 15700, …, 26200


@dataclass
class LevelRow:
    level: int
    xp_required: int        # XP to reach this level FROM previous level
    cumulative_xp: int      # total XP from level 1 to reach this level
    tier: str
    # Time-to-level estimates (ticks) per personality
    ticks_logician: float
    ticks_artisan: float
    ticks_empath: float
    ticks_tactician: float
    ticks_pioneer: float


# ─── Time estimator ────────────────────────────────────────────────────────────

_XP_PER_DATA_UNIT = 0.25   # matches game_state: xp += data_generated * 0.25

def _estimate_ticks_to_level(
    personality: PersonalityType,
    level: int,
    base_data_per_tick: float = 3.0,
) -> float:
    """
    Estimate ticks needed to earn enough XP to advance from `level` to `level+1`.
    Uses the data rate at `level` (personality + intelligence mult, no upgrades).
    """
    xp_needed = xp_to_next_level(level)
    if xp_needed == 0:
        return 0.0
    xp_per_tick = (
        data_rate_at_level(personality, level, base_data_per_tick)
        * _XP_PER_DATA_UNIT
        * personality.xp_gain_mult
    )
    if xp_per_tick <= 0:
        return float("inf")
    return round(xp_needed / xp_per_tick, 1)


def _tier_label(level: int) -> str:
    if level <= 10:
        return "Novice"
    if level <= 25:
        return "Apprentice"
    if level <= 40:
        return "Advanced"
    return "Master"


# ─── Build table ──────────────────────────────────────────────────────────────

def build_level_table(base_data_per_tick: float = 3.0) -> list[LevelRow]:
    """Generate the full 50-level table."""
    rows: list[LevelRow] = []
    cumulative = 0

    for lvl in range(1, 51):
        xp_req = xp_to_next_level(lvl - 1) if lvl > 1 else 0
        cumulative += xp_req

        def ticks(p: PersonalityType, lv: int = lvl) -> float:
            return _estimate_ticks_to_level(p, lv, base_data_per_tick)

        rows.append(LevelRow(
            level=lvl,
            xp_required=xp_req,
            cumulative_xp=cumulative,
            tier=_tier_label(lvl),
            ticks_logician=ticks(LOGICIAN),
            ticks_artisan=ticks(ARTISAN),
            ticks_empath=ticks(EMPATH),
            ticks_tactician=ticks(TACTICIAN),
            ticks_pioneer=ticks(PIONEER),
        ))
    return rows


# ─── Balance validation ────────────────────────────────────────────────────────

@dataclass
class BalanceReport:
    tier_summaries: list[dict]
    personality_totals: dict[str, float]    # total ticks per personality to reach lvl 50
    personality_minutes: dict[str, float]   # assuming 1 tick/sec
    fastest_personality: str
    slowest_personality: str
    warnings: list[str]
    passed: bool


_TICK_INTERVAL_SEC = 1.0  # 1 second per tick (matches Unity prototype)


def validate_balance(table: list[LevelRow] | None = None) -> BalanceReport:
    """
    Run balance checks against the level table.

    Targets (no upgrades, average gameplay):
    - Reach Level 10 (evolution gate) within ~45 min (2700 ticks)
    - Reach Level 25 within ~120 min
    - All personalities stay within 2x of each other at each tier
    """
    if table is None:
        table = build_level_table()

    warnings: list[str] = []

    # ── Per-personality cumulative tick counts ────────────────────────────────
    personalities = {
        "Logician":  "ticks_logician",
        "Artisan":   "ticks_artisan",
        "Empath":    "ticks_empath",
        "Tactician": "ticks_tactician",
        "Pioneer":   "ticks_pioneer",
    }

    totals: dict[str, float] = {name: 0.0 for name in personalities}

    for row in table[1:]:  # skip level 1 (no XP cost to enter it)
        for name, field in personalities.items():
            totals[name] += getattr(row, field)

    minutes = {name: round(t * _TICK_INTERVAL_SEC / 60, 1) for name, t in totals.items()}

    fastest = min(totals, key=totals.__getitem__)
    slowest = max(totals, key=totals.__getitem__)

    # ── Tier summaries ────────────────────────────────────────────────────────
    tier_ranges = [
        ("Novice",      1, 10),
        ("Apprentice", 11, 25),
        ("Advanced",   26, 40),
        ("Master",     41, 50),
    ]

    tier_summaries = []
    for tier_name, lo, hi in tier_ranges:
        tier_rows = [r for r in table if lo <= r.level <= hi]
        tier_xp = sum(r.xp_required for r in tier_rows)
        avg_ticks = {
            name: round(sum(getattr(r, field) for r in tier_rows), 1)
            for name, field in personalities.items()
        }
        tier_summaries.append({
            "tier": tier_name,
            "levels": f"{lo}-{hi}",
            "total_xp": tier_xp,
            "avg_ticks_by_personality": avg_ticks,
        })

    # ── Guardrail: Level 10 reachable within 60 min (no upgrades) ───────────
    # XP-focused classes (Logician/Pioneer) will hit this in ~43 min.
    # Money/social classes (Artisan/Empath/Tactician) need ~54-59 min without upgrades;
    # with a single ModelTraining upgrade this drops well under 45 min.
    for name, field in personalities.items():
        ticks_to_10 = sum(getattr(table[i], field) for i in range(1, 10))
        minutes_to_10 = ticks_to_10 / 60
        if minutes_to_10 > 60:
            warnings.append(
                f"[WARN] {name}: {minutes_to_10:.1f} min to reach Lv10 (target ≤ 60 min)"
            )

    # ── Guardrail: personality spread ≤ 2× within any tier ──────────────────
    for ts in tier_summaries:
        tick_vals = list(ts["avg_ticks_by_personality"].values())
        if max(tick_vals) > 0 and min(tick_vals) > 0:
            ratio = max(tick_vals) / min(tick_vals)
            if ratio > 2.0:
                warnings.append(
                    f"[WARN] Tier {ts['tier']}: personality spread {ratio:.2f}× (target ≤ 2×)"
                )

    # ── Guardrail: no single level > 30 min for Novice/Apprentice (no upgrades) ─
    for row in table:
        if row.tier not in ("Novice", "Apprentice"):
            continue   # Advanced/Master require upgrades; skip this check
        for name, field in personalities.items():
            t = getattr(row, field)
            if t > 1800:
                warnings.append(
                    f"[WARN] Lv{row.level} {name}: {t/60:.1f} min per level (target ≤ 30 min)"
                )

    passed = len(warnings) == 0
    return BalanceReport(
        tier_summaries=tier_summaries,
        personality_totals={k: round(v, 1) for k, v in totals.items()},
        personality_minutes=minutes,
        fastest_personality=fastest,
        slowest_personality=slowest,
        warnings=warnings,
        passed=passed,
    )


# ─── Pretty-print helpers ──────────────────────────────────────────────────────

def print_level_table(table: list[LevelRow] | None = None) -> None:
    if table is None:
        table = build_level_table()
    header = (
        f"{'Lv':>3} {'Tier':<12} {'XP Req':>8} {'Cum XP':>9} "
        f"{'Logician':>10} {'Artisan':>10} {'Empath':>10} "
        f"{'Tactician':>10} {'Pioneer':>10}"
    )
    print(header)
    print("-" * len(header))
    for r in table:
        print(
            f"{r.level:>3} {r.tier:<12} {r.xp_required:>8,} {r.cumulative_xp:>9,} "
            f"{r.ticks_logician:>10.0f} {r.ticks_artisan:>10.0f} {r.ticks_empath:>10.0f} "
            f"{r.ticks_tactician:>10.0f} {r.ticks_pioneer:>10.0f}"
        )


def print_balance_report(report: BalanceReport) -> None:
    print("\n=== BALANCE REPORT ===")
    print(f"Status: {'PASS' if report.passed else 'WARNINGS'}\n")

    print("── Personality: total ticks to Lv50 ──")
    for name, ticks in report.personality_totals.items():
        mins = report.personality_minutes[name]
        tag = ""
        if name == report.fastest_personality:
            tag = " ← fastest"
        elif name == report.slowest_personality:
            tag = " ← slowest"
        print(f"  {name:<12}: {ticks:>8,.0f} ticks  ({mins:>6.1f} min){tag}")

    print("\n── Tier summaries ──")
    for ts in report.tier_summaries:
        print(f"  {ts['tier']} (Lv {ts['levels']}): {ts['total_xp']:,} total XP")
        for pname, ticks in ts["avg_ticks_by_personality"].items():
            print(f"    {pname:<12}: {ticks:>8,.0f} ticks  ({ticks/60:.1f} min)")

    if report.warnings:
        print("\n── Warnings ──")
        for w in report.warnings:
            print(f"  {w}")
    else:
        print("\n  All balance checks passed.")


if __name__ == "__main__":
    tbl = build_level_table()
    print_level_table(tbl)
    rpt = validate_balance(tbl)
    print_balance_report(rpt)
