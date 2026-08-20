from orchestrator.game_state import GameSession, RunState
from orchestrator.monetization_runtime import MonetizationRuntime


def test_fail_continue_modal_revive_success_resumes_run():
    s = GameSession()
    s.start_run()
    s.fail()
    rt = MonetizationRuntime(s)

    d = rt.on_fail_continue_modal(wants_revive=True, provider_result="rewarded")

    assert d.action == "resume_run"
    assert s.state == RunState.RUNNING


def test_fail_continue_modal_decline_goes_result_path():
    s = GameSession()
    s.start_run()
    s.fail()
    rt = MonetizationRuntime(s)

    d = rt.on_fail_continue_modal(wants_revive=False)

    assert d.action == "show_result"
    assert d.message == "revive_declined"


def test_pre_result_reward_modal_finalizes_with_x2():
    s = GameSession()
    s.start_run()
    s.add_score(20)  # base reward 4
    rt = MonetizationRuntime(s)

    d = rt.on_pre_result_reward_modal(wants_x2=True, provider_result="rewarded")

    assert d.action == "show_result"
    assert d.reward_coins == 8
    assert s.last_reward == 8
    assert s.state == RunState.RESULT


def test_post_result_interstitial_skipped_when_blocked():
    s = GameSession(runs_today=1)
    rt = MonetizationRuntime(s)

    d = rt.on_post_result_interstitial(provider_result="closed")

    assert d.action == "to_next_screen"
    assert d.message == "interstitial_skipped"


def test_offer_selection_day1_third_run_prefers_starter_pack():
    s = GameSession(current_day=1, runs_today=3)
    rt = MonetizationRuntime(s)

    d = rt.maybe_show_offer_on_run_end(milestone_day=False)

    assert d.action == "show_offer"
    assert d.offer_id == "starter_pack"


def test_offer_selection_remove_ads_after_ad_exposure():
    s = GameSession(
        current_day=4,
        runs_today=5,
        interstitials_today=3,
        app_session_id=2,
        starter_pack_purchased=True,
    )
    rt = MonetizationRuntime(s)

    d = rt.maybe_show_offer_on_run_end()

    assert d.action == "show_offer"
    assert d.offer_id == "remove_ads"


def test_shop_purchase_remove_ads_success_updates_session():
    s = GameSession()
    rt = MonetizationRuntime(s)

    d = rt.on_shop_purchase("remove_ads", "purchase_success")

    assert d.message == "purchase_success"
    assert s.remove_ads_purchased is True
