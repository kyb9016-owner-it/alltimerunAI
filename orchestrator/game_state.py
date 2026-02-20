from dataclasses import dataclass
from enum import Enum


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

    def start_run(self) -> None:
        self.state = RunState.RUNNING
        self.score = 0

    def add_score(self, value: int) -> None:
        if self.state != RunState.RUNNING:
            return
        if value > 0:
            self.score += value

    def fail(self) -> None:
        if self.state != RunState.RUNNING:
            return
        self.state = RunState.FAILED

    def finalize_run(self) -> None:
        if self.state not in (RunState.FAILED, RunState.RUNNING):
            return
        self.state = RunState.RESULT
        self.best_score = max(self.best_score, self.score)

    def retry(self) -> None:
        self.start_run()
