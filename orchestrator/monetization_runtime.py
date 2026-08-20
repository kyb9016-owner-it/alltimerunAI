from __future__ import annotations

from dataclasses import dataclass

from orchestrator.game_state import GameSession, RunState


@dataclass(frozen=True)
class RuntimeDecision:
    action: str
    message: str
    offer_id: str | None = None
    reward_coins: int | None = None


class MonetizationRuntime:
    """UI-facing monetization flow orchestration over GameSession methods."""

    def __init__(self, session: GameSession):
        self.session = session

    def on_fail_continue_modal(
        self,
        wants_revive: bool,
        provider_result: str | None = None,
    ) -> RuntimeDecision:
        if self.session.state != RunState.FAILED:
            return RuntimeDecision(
                action="noop",
                message="run_not_failed",
            )

        if not wants_revive:
            return RuntimeDecision(
                action="show_result",
                message="revive_declined",
            )

        outcome = self.session.try_rewarded_revive(provider_result or "ad_unavailable")
        if outcome == "resume_run":
            return RuntimeDecision(
                action="resume_run",
                message="revive_success",
            )

        return RuntimeDecision(
            action="show_result",
            message="revive_fallback_continue",
            reward_coins=self.session.fallback_currency_grant,
        )

    def on_pre_result_reward_modal(
        self,
        wants_x2: bool,
        provider_result: str | None = None,
    ) -> RuntimeDecision:
        if self.session.state not in (RunState.RUNNING, RunState.FAILED):
            return RuntimeDecision(
                action="noop",
                message="run_not_finalizable",
            )

        if wants_x2:
            self.session.try_rewarded_x2(provider_result or "ad_unavailable")
        self.session.finalize_run()
        return RuntimeDecision(
            action="show_result",
            message="result_ready",
            reward_coins=self.session.last_reward,
        )

    def on_post_result_interstitial(self, provider_result: str | None = None) -> RuntimeDecision:
        outcome = self.session.try_interstitial(provider_result or "ad_unavailable")
        if outcome == "next_screen":
            return RuntimeDecision(action="to_next_screen", message="interstitial_closed")
        return RuntimeDecision(action="to_next_screen", message="interstitial_skipped")

    def maybe_show_offer_on_run_end(self, milestone_day: bool = False) -> RuntimeDecision:
        offer_id = self._select_offer_for_run_end(milestone_day=milestone_day)
        if not offer_id:
            return RuntimeDecision(action="no_offer", message="no_eligible_offer")
        if not self.session.show_offer(offer_id):
            return RuntimeDecision(action="no_offer", message="offer_interval_blocked")
        return RuntimeDecision(action="show_offer", message="offer_impression_logged", offer_id=offer_id)

    def on_shop_purchase(self, offer_id: str, provider_result: str) -> RuntimeDecision:
        outcome = self.session.purchase(offer_id, provider_result)
        if outcome == "post_purchase_continue":
            return RuntimeDecision(action="purchase_done", message="purchase_success", offer_id=offer_id)
        return RuntimeDecision(action="purchase_done", message="purchase_not_completed", offer_id=offer_id)

    def _select_offer_for_run_end(self, milestone_day: bool) -> str | None:
        s = self.session

        # Day 1 first offer: after third run end.
        if s.current_day == 1 and s.runs_today >= 3 and not s.starter_pack_purchased:
            return "starter_pack"

        # Remove ads: after meaningful ad exposure.
        if not s.remove_ads_purchased and s.interstitials_today >= 3 and s.app_session_id >= 2:
            return "remove_ads"

        # Weekly value pack on milestone days only.
        if milestone_day and s.current_day in (3, 7, 10):
            return "weekly_value_pack"

        return None
