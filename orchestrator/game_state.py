from dataclasses import dataclass
from dataclasses import field
from enum import Enum
import random

from orchestrator.personality_types import PersonalityID, PersonalityType
from orchestrator.level_table import xp_to_next_level as _xp_table_lookup

OFFLINE_MAX_SECONDS = 8.0 * 3600.0
OFFLINE_MULTIPLIER = 0.6


class RunState(str, Enum):
    HOME = "home"
    RUNNING = "running"
    FAILED = "failed"
    RESULT = "result"


@dataclass
class GameSession:
    state: RunState = RunState.HOME
    score: int = 0
    best_score: int = 0
    coins: int = 0
    retry_count: int = 0
    last_reward: int = 0
    reward_per_score: float = 0.2
    tick_count: int = 0

    energy: float = 50.0
    energy_max: float = 50.0
    data: float = 0.0
    money: float = 0.0

    level: int = 1
    xp: float = 0.0
    intelligence_mult: float = 1.0
    optimization_bonus: float = 0.0

    energy_gen_per_tick: float = 2.0
    learn_energy_cost: float = 1.5
    base_data_per_tick: float = 3.0
    base_money_per_tick: float = 0.8
    base_event_chance: float = 0.03
    event_risk_mult: float = 1.0
    stability: float = 1.0
    firewall_level: int = 0
    stage: str = "Core"
    is_proto_human: bool = False
    last_event: str = ""
    generator_level: int = 0
    battery_level: int = 0
    model_training_level: int = 0
    optimization_level: int = 0
    stability_level: int = 0
    max_upgrade_level: int = 5

    # Personality system
    personality_id: PersonalityID = PersonalityID.LOGICIAN

    forced_event_rolls: list[float] = field(default_factory=list)
    forced_event_types: list[str] = field(default_factory=list)
    generator_costs: list[float] = field(default_factory=lambda: [30, 80, 160, 300, 500])
    battery_costs: list[float] = field(default_factory=lambda: [20, 60, 120, 220, 380])
    model_training_costs: list[float] = field(default_factory=lambda: [40, 120, 250, 450, 700])
    optimization_costs: list[float] = field(default_factory=lambda: [60, 160, 330, 600, 900])
    ops_costs: list[float] = field(default_factory=lambda: [50, 140, 300, 520, 800])
    telemetry_events: list[dict[str, object]] = field(default_factory=list)

    current_day: int = 1
    runs_today: int = 0
    interstitials_today: int = 0
    rewarded_ads_today: int = 0
    interstitials_this_session: int = 0
    app_session_id: int = 1
    last_interstitial_tick: int = -10_000
    last_offer_tick: int = -10_000
    remove_ads_purchased: bool = False
    starter_pack_purchased: bool = False
    weekly_pack_purchase_count: int = 0
    rewarded_revive_used_this_run: int = 0
    rewarded_x2_used_this_run: int = 0
    pending_rewarded_x2: bool = False

    interstitial_cooldown_sec: int = 150
    interstitial_max_per_session: int = 2
    interstitial_max_per_day: int = 8
    interstitial_blocked_first_runs_per_day: int = 2
    rewarded_max_per_run_revive: int = 1
    rewarded_max_per_run_x2: int = 1
    rewarded_max_per_day_shared: int = 6
    offer_min_interval_sec: int = 600
    fallback_currency_grant: int = 1

    # Offline earnings (ACCEPTANCE: save lastTimestampUtc, 8h cap, 0.6 multiplier, E=Emax on load)
    last_exit_timestamp_utc: float = 0.0

    telemetry_event_names: tuple[str, ...] = (
        "ad_attempt",
        "ad_result",
        "offer_impression",
        "offer_click",
        "purchase_start",
        "purchase_result",
    )

    def set_day(self, day: int) -> None:
        if day <= 0 or day == self.current_day:
            return
        self.current_day = day
        self.runs_today = 0
        self.interstitials_today = 0
        self.rewarded_ads_today = 0

    def set_last_exit_timestamp(self, utc_seconds: float) -> None:
        """Call on save/exit so offline earnings can be computed on next load."""
        self.last_exit_timestamp_utc = max(0.0, utc_seconds)

    def _passive_data_money_per_tick(self) -> tuple[float, float]:
        """Data and money per tick without events or personality specials (for offline calc)."""
        p = self.personality
        if self.energy < self.learn_energy_cost * p.energy_cost_mult:
            return (0.0, 0.0)
        data = (
            self.base_data_per_tick
            * p.data_gen_mult
            * self.intelligence_mult
            * (1.0 + self.optimization_bonus)
        )
        money = (
            self.base_money_per_tick
            * p.money_gen_mult
            * self.intelligence_mult
        )
        return (data, money)

    def apply_offline_earnings(self, now_utc_seconds: float) -> None:
        """Apply offline D/M per ACCEPTANCE: clamp 0..8h, 0.6 multiplier; set E=Emax."""
        if self.last_exit_timestamp_utc <= 0:
            return
        offline_sec = now_utc_seconds - self.last_exit_timestamp_utc
        offline_sec = max(0.0, min(offline_sec, OFFLINE_MAX_SECONDS))
        data_per_tick, money_per_tick = self._passive_data_money_per_tick()
        # Tick interval = 1s in spec
        self.data += data_per_tick * offline_sec * OFFLINE_MULTIPLIER
        self.money += money_per_tick * offline_sec * OFFLINE_MULTIPLIER
        self.energy = self.energy_max
        self.last_exit_timestamp_utc = 0.0  # consumed

    def start_app_session(self) -> None:
        self.app_session_id += 1
        self.interstitials_this_session = 0

    def log_event(self, name: str, **props: object) -> None:
        if name not in self.telemetry_event_names:
            return
        event = {"name": name, "tick": self.tick_count, "day": self.current_day}
        event.update(props)
        self.telemetry_events.append(event)

    def start_run(self) -> None:
        if self.state == RunState.RUNNING:
            return
        self.state = RunState.RUNNING
        self.score = 0
        self.last_reward = 0
        self.runs_today += 1
        self.rewarded_revive_used_this_run = 0
        self.rewarded_x2_used_this_run = 0
        self.pending_rewarded_x2 = False

    def add_score(self, value: int) -> None:
        if self.state != RunState.RUNNING:
            return
        if value > 0:
            self.score += value

    @property
    def personality(self) -> PersonalityType:
        return PersonalityType.get(self.personality_id)

    def personality_level_up_delta(self) -> float:
        """intelligence_mult increment on each level-up for this personality."""
        return self.personality.level_up_intelligence_delta()

    def xp_to_next_level(self) -> float:
        """XP required to advance from current level to next (tiered table)."""
        return float(_xp_table_lookup(self.level))

    @staticmethod
    def _clamp01(value: float) -> float:
        if value < 0:
            return 0.0
        if value > 1:
            return 1.0
        return value

    def event_chance(self) -> float:
        stability_reduction = 0.25 * self._clamp01((self.stability - 1.0) / 3.0)
        return self.base_event_chance * self.event_risk_mult * (1.0 - stability_reduction)

    def _next_event_roll(self) -> float:
        if self.forced_event_rolls:
            return self.forced_event_rolls.pop(0)
        return random.random()

    def _next_event_type(self) -> str:
        if self.forced_event_types:
            return self.forced_event_types.pop(0)
        return random.choice(["ServerOverheat", "SecurityBreach", "EfficiencyBoost"])

    def _apply_event(self, event_type: str) -> None:
        self._apply_event_with_personality(event_type, self.personality)

    def _apply_event_with_personality(
        self, event_type: str, p: PersonalityType
    ) -> None:
        if event_type == "ServerOverheat":
            damage = 20.0 * p.negative_event_mult
            self.energy = max(0.0, self.energy - damage)
        elif event_type == "SecurityBreach":
            reduced_ratio = min(1.0, max(0.0, self.firewall_level * 0.1))
            damage = 50.0 * (1.0 - reduced_ratio) * p.negative_event_mult
            self.money = max(0.0, self.money - damage)
        elif event_type == "EfficiencyBoost":
            self.intelligence_mult += 0.05 * p.positive_event_mult
        else:
            return
        self.last_event = event_type

    def _process_evolution(self) -> None:
        if self.is_proto_human:
            return
        if self.level < 10:
            return
        if self.data < 2500.0:
            return
        if self.money < 1500.0:
            return

        self.is_proto_human = True
        self.stage = "ProtoHuman"
        self.intelligence_mult *= 1.25
        self.event_risk_mult *= 1.1

    def _try_pay(self, cost: float) -> bool:
        if self.money < cost:
            return False
        self.money -= cost
        return True

    def _upgrade_stat_mult(self) -> float:
        """Tactician gets +10% to all upgrade stat bonuses."""
        return 1.1 if self.personality_id == PersonalityID.TACTICIAN else 1.0

    def _effective_cost(self, base_cost: float) -> float:
        return base_cost * self.personality.upgrade_cost_mult

    def upgrade(self, name: str) -> bool:
        stat_m = self._upgrade_stat_mult()

        if name == "Generator":
            if self.generator_level >= self.max_upgrade_level:
                return False
            cost = self._effective_cost(self.generator_costs[self.generator_level])
            if not self._try_pay(cost):
                return False
            self.generator_level += 1
            self.energy_gen_per_tick += 0.6 * stat_m
            return True

        if name == "Battery":
            if self.battery_level >= self.max_upgrade_level:
                return False
            cost = self._effective_cost(self.battery_costs[self.battery_level])
            if not self._try_pay(cost):
                return False
            self.battery_level += 1
            self.energy_max += 10.0 * stat_m
            self.energy = min(self.energy_max, self.energy + 10.0 * stat_m)
            return True

        if name == "ModelTraining":
            if self.model_training_level >= self.max_upgrade_level:
                return False
            cost = self._effective_cost(self.model_training_costs[self.model_training_level])
            if not self._try_pay(cost):
                return False
            self.model_training_level += 1
            self.base_data_per_tick += 0.8 * stat_m
            return True

        if name == "Optimization":
            if self.optimization_level >= self.max_upgrade_level:
                return False
            cost = self._effective_cost(self.optimization_costs[self.optimization_level])
            if not self._try_pay(cost):
                return False
            self.optimization_level += 1
            self.optimization_bonus += 0.06 * stat_m
            return True

        if name == "Stability":
            if self.stability_level >= self.max_upgrade_level:
                return False
            cost = self._effective_cost(self.ops_costs[self.stability_level])
            if not self._try_pay(cost):
                return False
            self.stability_level += 1
            self.stability += 0.5 * stat_m
            return True

        if name == "Firewall":
            if self.firewall_level >= self.max_upgrade_level:
                return False
            cost = self._effective_cost(self.ops_costs[self.firewall_level])
            if not self._try_pay(cost):
                return False
            self.firewall_level += 1
            return True

        return False

    def tick(self, ticks: int = 1) -> None:
        if self.state != RunState.RUNNING:
            return
        if ticks <= 0:
            return

        p = self.personality
        for _ in range(ticks):
            self.tick_count += 1

            self.energy = min(self.energy_max, self.energy + self.energy_gen_per_tick)
            effective_cost = self.learn_energy_cost * p.energy_cost_mult
            if self.energy >= effective_cost:
                self.energy -= effective_cost

                # Personality special: Logician / Pioneer trigger on interval ticks
                special_data_mult = 1.0
                special_money_chance_triggered = False
                interval = p.special_trigger_interval
                if interval > 0 and self.tick_count % interval == 0:
                    if self.personality_id == PersonalityID.LOGICIAN:
                        special_data_mult = 2.0
                    elif self.personality_id == PersonalityID.PIONEER:
                        # Force a positive event bonus tick
                        self._apply_event("EfficiencyBoost")
                    elif self.personality_id == PersonalityID.ARTISAN:
                        if random.random() < 0.20:
                            special_money_chance_triggered = True

                data_generated = (
                    self.base_data_per_tick
                    * p.data_gen_mult
                    * self.intelligence_mult
                    * (1.0 + self.optimization_bonus)
                    * special_data_mult
                )
                money_mult = 3.0 if special_money_chance_triggered else 1.0
                money_generated = (
                    self.base_money_per_tick
                    * p.money_gen_mult
                    * self.intelligence_mult
                    * money_mult
                )
                xp_gained = data_generated * 0.25 * p.xp_gain_mult

                self.data += data_generated
                self.money += money_generated
                self.xp += xp_gained
                self.score += int(data_generated)

                while self.xp >= self.xp_to_next_level():
                    self.xp -= self.xp_to_next_level()
                    self.level += 1
                    self.intelligence_mult += self.personality_level_up_delta()
                    # Per-level stat bonuses
                    self.base_data_per_tick += p.lvl_data_bonus
                    self.base_money_per_tick += p.lvl_money_bonus

            roll = self._next_event_roll()
            effective_event_chance = self.event_chance() * p.event_chance_mult
            if roll < effective_event_chance:
                event_type = self._next_event_type()
                self._apply_event_with_personality(event_type, p)

            self._process_evolution()

    def fail(self) -> None:
        if self.state != RunState.RUNNING:
            return
        self.state = RunState.FAILED

    def finalize_run(self) -> None:
        if self.state not in (RunState.FAILED, RunState.RUNNING):
            return
        self.state = RunState.RESULT
        self.best_score = max(self.best_score, self.score)
        self.last_reward = int(self.score * self.reward_per_score)
        if self.pending_rewarded_x2:
            self.last_reward *= 2
            self.pending_rewarded_x2 = False
        self.coins += self.last_reward

    def retry(self) -> None:
        self.retry_count += 1
        self.start_run()

    def _can_use_rewarded(self) -> bool:
        return self.rewarded_ads_today < self.rewarded_max_per_day_shared

    def try_rewarded_revive(self, provider_result: str) -> str:
        self.log_event("ad_attempt", placement="rewarded_revive")
        if self.rewarded_revive_used_this_run >= self.rewarded_max_per_run_revive or not self._can_use_rewarded():
            self.log_event("ad_result", placement="rewarded_revive", result="cap_blocked")
            return "fallback_continue"

        if provider_result in ("ad_unavailable", "ad_error", "closed_unrewarded", "show_error"):
            self.coins += self.fallback_currency_grant
            self.log_event("ad_result", placement="rewarded_revive", result=provider_result)
            return "fallback_continue"

        if provider_result != "rewarded":
            self.log_event("ad_result", placement="rewarded_revive", result="invalid_result")
            return "fallback_continue"

        self.rewarded_revive_used_this_run += 1
        self.rewarded_ads_today += 1
        if self.state == RunState.FAILED:
            self.state = RunState.RUNNING
        self.log_event("ad_result", placement="rewarded_revive", result="rewarded")
        return "resume_run"

    def try_rewarded_x2(self, provider_result: str) -> str:
        self.log_event("ad_attempt", placement="rewarded_x2")
        if self.rewarded_x2_used_this_run >= self.rewarded_max_per_run_x2 or not self._can_use_rewarded():
            self.log_event("ad_result", placement="rewarded_x2", result="cap_blocked")
            return "fallback_base_reward"

        if provider_result in ("ad_unavailable", "ad_error", "closed_unrewarded", "show_error"):
            self.log_event("ad_result", placement="rewarded_x2", result=provider_result)
            return "fallback_base_reward"

        if provider_result != "rewarded":
            self.log_event("ad_result", placement="rewarded_x2", result="invalid_result")
            return "fallback_base_reward"

        self.rewarded_x2_used_this_run += 1
        self.rewarded_ads_today += 1
        self.pending_rewarded_x2 = True
        self.log_event("ad_result", placement="rewarded_x2", result="rewarded")
        return "grant_rewards"

    def can_show_interstitial(self) -> bool:
        if self.remove_ads_purchased:
            return False
        if self.runs_today <= self.interstitial_blocked_first_runs_per_day:
            return False
        if self.interstitials_this_session >= self.interstitial_max_per_session:
            return False
        if self.interstitials_today >= self.interstitial_max_per_day:
            return False
        if (self.tick_count - self.last_interstitial_tick) < self.interstitial_cooldown_sec:
            return False
        return True

    def try_interstitial(self, provider_result: str) -> str:
        self.log_event("ad_attempt", placement="interstitial")
        if not self.can_show_interstitial():
            self.log_event("ad_result", placement="interstitial", result="blocked")
            return "skip"

        if provider_result in ("ad_unavailable", "ad_error"):
            self.log_event("ad_result", placement="interstitial", result=provider_result)
            return "skip"

        if provider_result not in ("closed", "show_error"):
            self.log_event("ad_result", placement="interstitial", result="invalid_result")
            return "skip"

        self.interstitials_today += 1
        self.interstitials_this_session += 1
        self.last_interstitial_tick = self.tick_count
        self.log_event("ad_result", placement="interstitial", result=provider_result)
        return "next_screen"

    def can_show_offer(self) -> bool:
        return (self.tick_count - self.last_offer_tick) >= self.offer_min_interval_sec

    def show_offer(self, offer_id: str) -> bool:
        if not self.can_show_offer():
            return False
        self.last_offer_tick = self.tick_count
        self.log_event("offer_impression", offer_id=offer_id)
        return True

    def purchase(self, offer_id: str, provider_result: str) -> str:
        self.log_event("offer_click", offer_id=offer_id)
        self.log_event("purchase_start", offer_id=offer_id)
        if provider_result == "purchase_cancel":
            self.log_event("purchase_result", offer_id=offer_id, result="cancel")
            return "no_purchase_continue"
        if provider_result == "purchase_error":
            self.log_event("purchase_result", offer_id=offer_id, result="error")
            return "no_purchase_continue"
        if provider_result != "purchase_success":
            self.log_event("purchase_result", offer_id=offer_id, result="invalid_result")
            return "no_purchase_continue"

        if offer_id == "remove_ads":
            self.remove_ads_purchased = True
        elif offer_id == "starter_pack":
            if not self.starter_pack_purchased:
                self.starter_pack_purchased = True
                self.coins += 120
                self.money += 300.0
        elif offer_id == "weekly_value_pack":
            self.weekly_pack_purchase_count += 1
            self.coins += 80
            self.money += 200.0

        self.log_event("purchase_result", offer_id=offer_id, result="success")
        return "post_purchase_continue"
