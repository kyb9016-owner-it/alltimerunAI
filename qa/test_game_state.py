from orchestrator.game_state import GameSession, RunState


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

    s.retry()
    assert s.state == RunState.RUNNING
    assert s.score == 0
    s.add_score(5)
    s.finalize_run()
    assert s.best_score == 20
