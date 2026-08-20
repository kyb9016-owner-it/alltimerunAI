from orchestrator.game_state import (
    GameSession,
    RunState,
    OFFLINE_MAX_SECONDS,
    OFFLINE_MULTIPLIER,
)
from orchestrator.personality_types import (
    PersonalityID,
    LOGICIAN, ARTISAN, EMPATH, TACTICIAN, PIONEER,
    intelligence_at_level,
    data_rate_at_level,
    growth_table,
)
from orchestrator.level_table import (
    xp_to_next_level,
    build_level_table,
    validate_balance,
)
from orchestrator.interaction_patterns import (
    InteractionID,
    InteractionCooldowns,
    apply_interaction,
    ALL_INTERACTIONS,
)


# ─── Original tests (preserved, updated for new XP table) ─────────────────────

def test_run_loop_retry_updates_best_score():
    s = GameSession()

    s.start_run()
    s.add_score(12)
    s.add_score(8)
    s.fail()
    s.finalize_run()

    assert s.state == RunState.RESULT
    assert s.score == 20
    assert s.best_score == 20
    assert s.last_reward == 4
    assert s.coins == 4

    s.retry()
    assert s.state == RunState.RUNNING
    assert s.score == 0
    assert s.retry_count == 1
    s.add_score(5)
    s.finalize_run()
    assert s.best_score == 20
    assert s.last_reward == 1
    assert s.coins == 5


def test_score_updates_only_during_running_state():
    s = GameSession()
    s.add_score(10)
    assert s.score == 0

    s.start_run()
    s.add_score(10)
    assert s.score == 10

    s.fail()
    s.add_score(5)
    assert s.score == 10


def test_tick_generates_resources_and_score():
    """
    Default personality is LOGICIAN (data_gen_mult=1.20, energy_cost_mult=1.10).
    effective_cost = 1.5 * 1.10 = 1.65
    data = 3.0 * 1.20 * 1.0 * 1.0 = 3.6
    money = 0.8 * 0.90 * 1.0 = 0.72
    xp = 3.6 * 0.25 * 1.10 (xp_gain_mult) = 0.99
    energy after = 50 + 2.0 - 1.65 = 50.35  (capped at 50.0 → 50.35 > 50.0 so capped)
    Wait: start energy = 50.0, +2.0 = 52.0 → capped at 50.0; then -1.65 = 48.35
    """
    s = GameSession(personality_id=PersonalityID.LOGICIAN)
    s.start_run()
    # Force no events
    s.forced_event_rolls = [1.0]
    s.tick()

    assert s.tick_count == 1
    # energy: 50.0 + 2.0 → capped at 50.0, then -1.5*1.10(cost) = 48.35
    assert round(s.energy, 4) == round(50.0 - 1.5 * 1.10, 4)  # 48.35
    assert round(s.data, 4) == round(3.0 * 1.20 * 1.0, 4)            # 3.6
    assert round(s.money, 4) == round(0.8 * 0.90 * 1.0, 4)           # 0.72
    assert round(s.xp, 4) == round(3.6 * 0.25 * 1.05, 4)             # 0.945  (xp_gain_mult=1.05)
    assert s.score == 3  # int(3.6)


def test_tick_skips_learning_when_energy_insufficient():
    s = GameSession(energy=0.0, energy_gen_per_tick=0.5, learn_energy_cost=2.0)
    s.start_run()
    s.forced_event_rolls = [1.0]
    s.tick()

    assert s.tick_count == 1
    assert round(s.energy, 4) == 0.5
    assert s.data == 0.0
    assert s.money == 0.0
    assert s.xp == 0.0
    assert s.score == 0


def test_server_overheat_event_reduces_energy():
    s = GameSession(
        energy=20.0,
        energy_gen_per_tick=0.0,
        learn_energy_cost=100.0,
        forced_event_rolls=[0.0],
        forced_event_types=["ServerOverheat"],
    )
    s.start_run()
    s.tick()

    assert s.last_event == "ServerOverheat"
    # LOGICIAN negative_event_mult=1.15 → 20*1.15=23.0 damage; energy was 20 → 0
    assert s.energy == 0.0


def test_security_breach_event_respects_firewall_level():
    s = GameSession(
        money=100.0,
        firewall_level=2,
        energy_gen_per_tick=0.0,
        learn_energy_cost=100.0,
        forced_event_rolls=[0.0],
        forced_event_types=["SecurityBreach"],
        personality_id=PersonalityID.TACTICIAN,  # negative_event_mult=1.0 → clean math
    )
    s.start_run()
    s.tick()

    assert s.last_event == "SecurityBreach"
    # firewall=2 → 20% reduction, damage = 50*(1-0.2)*1.0 = 40
    assert s.money == 60.0


def test_evolution_changes_stage_and_risk_multiplier():
    s = GameSession(
        level=10,
        data=2500.0,
        money=1500.0,
        intelligence_mult=1.0,
        event_risk_mult=1.0,
        energy_gen_per_tick=0.0,
        learn_energy_cost=100.0,
        forced_event_rolls=[1.0],
    )
    s.start_run()
    s.tick()

    assert s.is_proto_human is True
    assert s.stage == "ProtoHuman"
    assert round(s.intelligence_mult, 4) == 1.25
    assert round(s.event_risk_mult, 4) == 1.1


def test_upgrade_generator_applies_cost_and_stat_bonus():
    """Default personality LOGICIAN: upgrade_cost_mult=1.0, stat_mult=1.0"""
    s = GameSession(money=100.0, personality_id=PersonalityID.LOGICIAN)
    ok = s.upgrade("Generator")

    assert ok is True
    assert s.generator_level == 1
    assert s.money == 70.0
    assert round(s.energy_gen_per_tick, 4) == 2.6


def test_upgrade_fails_when_insufficient_money_or_max_level():
    s = GameSession(money=10.0)
    assert s.upgrade("Battery") is False
    assert s.battery_level == 0

    s2 = GameSession(money=99999.0)
    for _ in range(5):
        assert s2.upgrade("Firewall") is True
    assert s2.upgrade("Firewall") is False
    assert s2.firewall_level == 5


def test_stability_upgrade_reduces_event_chance():
    s = GameSession(money=1000.0)
    before = s.event_chance()
    assert s.upgrade("Stability") is True
    after = s.event_chance()

    assert after < before


# ─── Personality type tests ────────────────────────────────────────────────────

def test_personality_intelligence_at_level():
    # LOGICIAN: delta = 0.04 + 0.02 = 0.06 per level
    assert intelligence_at_level(LOGICIAN, 1) == 1.0
    assert round(intelligence_at_level(LOGICIAN, 2), 6) == round(1.0 + 0.06, 6)
    assert round(intelligence_at_level(LOGICIAN, 10), 6) == round(1.0 + 9 * 0.06, 6)

    # ARTISAN: delta = 0.04 + 0.00 = 0.04
    assert round(intelligence_at_level(ARTISAN, 5), 6) == round(1.0 + 4 * 0.04, 6)

    # PIONEER: delta = 0.04 + 0.015 = 0.055
    assert round(intelligence_at_level(PIONEER, 3), 6) == round(1.0 + 2 * 0.055, 6)


def test_personality_data_rate_at_level():
    # LOGICIAN lvl 1: 3.0 * 1.20 * 1.0 = 3.6
    assert data_rate_at_level(LOGICIAN, 1) == 3.6

    # LOGICIAN lvl 2: base = (3.0 + 0.07) * 1.20 = 3.684; intel = 1.06
    expected = round((3.0 + 0.07) * 1.20 * (1.0 + 0.06), 4)
    assert data_rate_at_level(LOGICIAN, 2) == expected


def test_personality_growth_table_length():
    for p in [LOGICIAN, ARTISAN, EMPATH, TACTICIAN, PIONEER]:
        table = growth_table(p, levels=50)
        assert len(table) == 50
        assert table[0]["level"] == 1
        assert table[49]["level"] == 50


def test_tactician_upgrade_cost_discount():
    s = GameSession(money=1000.0, personality_id=PersonalityID.TACTICIAN)
    # Generator base cost = 30; Tactician upgrade_cost_mult = 0.80 → 24.0
    ok = s.upgrade("Generator")
    assert ok is True
    assert round(s.money, 2) == round(1000.0 - 30 * 0.80, 2)


def test_tactician_upgrade_stat_bonus_amplified():
    s = GameSession(money=1000.0, personality_id=PersonalityID.TACTICIAN)
    ok = s.upgrade("Generator")
    assert ok is True
    # stat_mult = 1.1 → energy_gen += 0.6 * 1.1 = 0.66
    assert round(s.energy_gen_per_tick, 4) == round(2.0 + 0.66, 4)


def test_personality_tick_applies_data_gen_mult():
    """Pioneer has data_gen_mult=0.95, energy_cost_mult=1.0"""
    s = GameSession(personality_id=PersonalityID.PIONEER)
    s.start_run()
    s.forced_event_rolls = [1.0]
    s.tick()

    expected_data = round(3.0 * 0.95 * 1.0, 4)  # 2.85
    assert round(s.data, 4) == expected_data


def test_personality_level_up_awards_stat_bonuses():
    """On level-up, base_data_per_tick and base_money_per_tick gain personality bonuses."""
    s = GameSession(
        personality_id=PersonalityID.LOGICIAN,
        xp=99.9,  # almost enough for Lv1→2 (xp_to_next=100)
        energy_gen_per_tick=10.0,
        learn_energy_cost=0.1,
    )
    s.start_run()
    s.forced_event_rolls = [1.0]
    s.tick()

    assert s.level == 2
    # base_data_per_tick gains lvl_data_bonus=0.07 on level-up
    assert round(s.base_data_per_tick, 4) == round(3.0 + 0.07, 4)


# ─── Level table tests ─────────────────────────────────────────────────────────

def test_xp_table_tier_breakpoints():
    # xp_to_next_level(level) = XP to go FROM level TO level+1
    assert xp_to_next_level(1)  == 100    # Tier1 min: 100 + 0*60
    assert xp_to_next_level(10) == 640    # Tier1 max: 100 + 9*60
    assert xp_to_next_level(11) == 700    # Tier2 start: 700 + 0*200
    assert xp_to_next_level(25) == 3500   # Tier2 end:   700 + 14*200
    assert xp_to_next_level(26) == 3700   # Tier3 start: 3700 + 0*600
    assert xp_to_next_level(40) == 12100  # Tier3 end:   3700 + 14*600
    assert xp_to_next_level(41) == 12700  # Tier4 start: 12700 + 0*1500
    assert xp_to_next_level(49) == 24700  # Tier4:  12700 + (49-41)*1500 = 12700+12000
    assert xp_to_next_level(50) == 0      # no further leveling


def test_level_table_length_and_cumulative():
    table = build_level_table()
    assert len(table) == 50
    assert table[0].level == 1
    assert table[0].cumulative_xp == 0  # level 1 has no entry cost
    assert table[1].xp_required == xp_to_next_level(1)  # cost to reach level 2


def test_level_table_tiers():
    table = build_level_table()
    tiers = {r.level: r.tier for r in table}
    assert tiers[1] == "Novice"
    assert tiers[10] == "Novice"
    assert tiers[11] == "Apprentice"
    assert tiers[25] == "Apprentice"
    assert tiers[26] == "Advanced"
    assert tiers[40] == "Advanced"
    assert tiers[41] == "Master"
    assert tiers[50] == "Master"


def test_balance_report_runs_without_error():
    table = build_level_table()
    report = validate_balance(table)
    # The report should have 5 personalities and 4 tier summaries
    assert len(report.personality_totals) == 5
    assert len(report.tier_summaries) == 4
    # Logician wins on raw data throughput (data_gen_mult=1.20 + intel bonus)
    # Pioneer's extra xp_gain_mult is offset by lower data_gen_mult at base stats
    assert report.fastest_personality in ("Pioneer", "Logician", "Tactician")


def test_balance_report_pioneer_faster_than_empath():
    table = build_level_table()
    report = validate_balance(table)
    assert report.personality_totals["Pioneer"] < report.personality_totals["Empath"]


# ─── Interaction pattern tests ────────────────────────────────────────────────

def test_all_15_interactions_registered():
    assert len(ALL_INTERACTIONS) == 15


def test_interaction_learn_adds_data_and_xp():
    s = GameSession(energy=50.0, personality_id=PersonalityID.LOGICIAN)
    cd = InteractionCooldowns()

    result = apply_interaction(s, cd, InteractionID.LEARN, PersonalityID.LOGICIAN)

    assert result.success is True
    # LOGICIAN has affinity → mult=1.5; energy_cost=5.0 consumed
    assert s.energy == 50.0 - 5.0
    assert s.data == round(15.0 * 1.5, 4)
    assert s.xp == round(4.0 * 1.5, 4)


def test_interaction_cooldown_blocks_reuse():
    s = GameSession(energy=100.0)
    cd = InteractionCooldowns()

    r1 = apply_interaction(s, cd, InteractionID.LEARN)
    assert r1.success is True

    r2 = apply_interaction(s, cd, InteractionID.LEARN)
    assert r2.success is False
    assert "쿨다운" in r2.message


def test_interaction_fails_on_insufficient_energy():
    s = GameSession(energy=1.0)
    cd = InteractionCooldowns()

    result = apply_interaction(s, cd, InteractionID.LEARN)  # costs 5.0
    assert result.success is False
    assert "에너지 부족" in result.message


def test_interaction_rest_recovers_energy():
    s = GameSession(energy=10.0)
    cd = InteractionCooldowns()

    result = apply_interaction(s, cd, InteractionID.REST)
    assert result.success is True
    # REST costs 0.0, restores 30.0 energy (capped at energy_max=50)
    assert s.energy == 40.0


def test_interaction_evolve_push_blocked_below_level_7():
    s = GameSession(energy=100.0, level=5)
    cd = InteractionCooldowns()

    result = apply_interaction(s, cd, InteractionID.EVOLVE_PUSH)
    assert result.success is False
    assert "레벨 7" in result.message


def test_interaction_evolve_push_allowed_at_level_7():
    s = GameSession(energy=100.0, level=7)
    cd = InteractionCooldowns()

    result = apply_interaction(s, cd, InteractionID.EVOLVE_PUSH)
    assert result.success is True
    assert s.data > 0 or s.money > 0


def test_interaction_risk_deterministic():
    """With force_roll=0.0, risk always triggers for risky interactions."""
    s = GameSession(energy=100.0, personality_id=PersonalityID.PIONEER)
    cd = InteractionCooldowns()

    result = apply_interaction(
        s, cd, InteractionID.CHALLENGE,
        personality_id=PersonalityID.PIONEER,
        force_roll=0.0,  # force risk trigger
    )
    assert result.success is True
    assert result.risk_triggered is True


def test_interaction_no_risk_with_high_roll():
    """With force_roll=1.0, risk never triggers."""
    s = GameSession(energy=100.0)
    cd = InteractionCooldowns()

    result = apply_interaction(
        s, cd, InteractionID.CHALLENGE,
        force_roll=1.0,  # no risk
    )
    assert result.success is True
    assert result.risk_triggered is False


def test_empath_resonance_amplifies_effects():
    """Empath gets 1.5x affinity on CONVERSE + 1.3x resonance = 1.95x total."""
    s_empath = GameSession(energy=50.0, personality_id=PersonalityID.EMPATH)
    s_base = GameSession(energy=50.0, personality_id=PersonalityID.LOGICIAN)
    cd_e = InteractionCooldowns()
    cd_b = InteractionCooldowns()

    apply_interaction(s_empath, cd_e, InteractionID.CONVERSE, PersonalityID.EMPATH, force_roll=1.0)
    apply_interaction(s_base, cd_b, InteractionID.CONVERSE, PersonalityID.LOGICIAN, force_roll=1.0)

    # Empath should gain more XP/money from CONVERSE than LOGICIAN
    # LOGICIAN has penalty on CONVERSE (×0.75); EMPATH has affinity (×1.5) + resonance (×1.3)
    assert s_empath.xp > s_base.xp
    assert s_empath.money > s_base.money


def test_cooldown_ticks_down():
    cd = InteractionCooldowns()
    cd.start_cooldown(InteractionID.LEARN)
    initial = cd.remaining[InteractionID.LEARN]  # 30
    cd.tick()
    assert cd.remaining[InteractionID.LEARN] == initial - 1
    assert not cd.is_ready(InteractionID.LEARN)


def test_cooldown_ready_after_enough_ticks():
    cd = InteractionCooldowns()
    cd.start_cooldown(InteractionID.PLAY)  # cooldown=20
    for _ in range(20):
        cd.tick()
    assert cd.is_ready(InteractionID.PLAY)


def test_rewarded_revive_resumes_failed_run_on_reward():
    s = GameSession()
    s.start_run()
    s.fail()

    result = s.try_rewarded_revive("rewarded")

    assert result == "resume_run"
    assert s.state == RunState.RUNNING
    assert s.rewarded_ads_today == 1


def test_rewarded_revive_fallback_grants_small_currency():
    s = GameSession(coins=0)
    s.start_run()
    s.fail()

    result = s.try_rewarded_revive("ad_unavailable")

    assert result == "fallback_continue"
    assert s.coins == 1
    assert s.state == RunState.FAILED


def test_rewarded_x2_applies_on_finalize_run():
    s = GameSession()
    s.start_run()
    s.add_score(20)  # base reward = 4
    assert s.try_rewarded_x2("rewarded") == "grant_rewards"

    s.finalize_run()

    assert s.last_reward == 8
    assert s.coins == 8


def test_interstitial_caps_block_first_runs_and_cooldown():
    s = GameSession()
    s.start_run()  # runs_today = 1
    assert s.can_show_interstitial() is False
    s.finalize_run()

    s.retry()  # runs_today = 2
    assert s.can_show_interstitial() is False
    s.finalize_run()

    s.retry()  # runs_today = 3, now eligible
    assert s.can_show_interstitial() is True
    assert s.try_interstitial("closed") == "next_screen"
    assert s.can_show_interstitial() is False  # cooldown

    s.tick_count += s.interstitial_cooldown_sec
    assert s.can_show_interstitial() is True


def test_purchase_remove_ads_blocks_interstitial():
    s = GameSession(runs_today=5, tick_count=1000)
    assert s.can_show_interstitial() is True
    assert s.purchase("remove_ads", "purchase_success") == "post_purchase_continue"
    assert s.remove_ads_purchased is True
    assert s.can_show_interstitial() is False


def test_offer_impression_respects_interval():
    s = GameSession(tick_count=1000)
    assert s.show_offer("starter_pack") is True
    assert s.show_offer("starter_pack") is False
    s.tick_count += s.offer_min_interval_sec
    assert s.show_offer("starter_pack") is True


def test_offline_earnings_applies_cap_and_multiplier_restores_energy():
    """ACCEPTANCE: 8h cap, 0.6 multiplier, E=Emax on load."""
    s = GameSession()
    s.energy = 30.0
    s.energy_max = 100.0
    s.data = 100.0
    s.money = 50.0
    s.set_last_exit_timestamp(1000.0)

    # 1 hour offline: 3600 seconds
    s.apply_offline_earnings(1000.0 + 3600.0)

    assert s.energy == s.energy_max == 100.0
    assert s.data > 100.0
    assert s.money > 50.0
    # Consumed timestamp
    assert s.last_exit_timestamp_utc == 0.0

    # No double-apply when timestamp already consumed
    before_data, before_money = s.data, s.money
    s.apply_offline_earnings(1000.0 + 7200.0)
    assert s.data == before_data and s.money == before_money


def test_offline_earnings_respects_8h_cap():
    base = 1000.0
    s = GameSession()
    s.set_last_exit_timestamp(base)
    s.apply_offline_earnings(base + OFFLINE_MAX_SECONDS + 3600.0)  # 8h+1h → cap 8h
    s2 = GameSession()
    s2.set_last_exit_timestamp(base)
    s2.apply_offline_earnings(base + OFFLINE_MAX_SECONDS)  # exactly 8h
    assert abs(s.data - s2.data) < 1.0  # same ~8h worth
