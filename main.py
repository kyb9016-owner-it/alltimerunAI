import argparse
import json
import os
import pathlib
import re
from typing import Dict, List, Set

import yaml
from openai import OpenAI
from rich import print

ROOT = pathlib.Path(__file__).parent
DOCS = ROOT / "docs"
PATCHES = ROOT / "patches"
ASSETS = ROOT / "assets"
QA = ROOT / "qa"
STORY = ROOT / "story"

DOCS.mkdir(exist_ok=True)
PATCHES.mkdir(exist_ok=True)
ASSETS.mkdir(exist_ok=True)
QA.mkdir(exist_ok=True)
STORY.mkdir(exist_ok=True)

MODEL = os.environ.get("OPENAI_MODEL", "gpt-5")
client = None

OWNER_INSTRUCTIONS = {
    "pm": ROOT / "agents/pm.md",
    "ui": ROOT / "agents/ui.md",
    "art": ROOT / "agents/art.md",
    "story": ROOT / "agents/story.md",
    "qa": ROOT / "agents/qa.md",
    "coder": ROOT / "agents/coder.md",
}

DEFAULT_OWNER_OUTPUTS = {
    "pm": ["docs/TASKS.yaml", "docs/ACCEPTANCE.md"],
    "ui": ["ui/flows.md", "ui/screens.md", "ui/components.json"],
    "art": ["assets/ART_DIRECTION.md", "assets/asset_list.json"],
    "story": ["story/STORY_BIBLE.md", "story/DIALOGUES.md"],
    "qa": ["qa/TEST_PLAN.md", "qa/TEST_CASES.yaml", "qa/RELEASE_CHECKLIST.md"],
    "coder": ["patches/changes.patch"],
}

FILE_BLOCK_PATTERN = r"--- file:\s*(.+?)\s*---\n(.*?)\n--- end ---"


def _read(path: pathlib.Path) -> str:
    return path.read_text(encoding="utf-8")


def _write(path: pathlib.Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")


def _extract_files(block: str):
    return re.findall(FILE_BLOCK_PATTERN, block, re.DOTALL)


def ask(instructions: str, user_input: str) -> str:
    global client
    if client is None:
        client = OpenAI(api_key=os.environ.get("OPENAI_API_KEY"))
    response = client.responses.create(
        model=MODEL,
        instructions=instructions,
        input=user_input,
    )
    return response.output_text


def _template_tasks_yaml() -> str:
    tasks = {
        "tasks": [
            {
                "id": "T001",
                "owner": "ui",
                "title": "Create core UI flow and screen spec",
                "inputs": ["docs/ACCEPTANCE.md"],
                "outputs": ["ui/flows.md", "ui/screens.md", "ui/components.json"],
                "dependencies": [],
            },
            {
                "id": "T002",
                "owner": "art",
                "title": "Define art direction and base asset list",
                "inputs": ["ui/screens.md"],
                "outputs": ["assets/ART_DIRECTION.md", "assets/asset_list.json"],
                "dependencies": ["T001"],
            },
            {
                "id": "T003",
                "owner": "story",
                "title": "Define world and character dialogue pack",
                "inputs": ["ui/flows.md"],
                "outputs": ["story/STORY_BIBLE.md", "story/DIALOGUES.md"],
                "dependencies": ["T001"],
            },
            {
                "id": "T004",
                "owner": "qa",
                "title": "Generate QA plan and release checklist",
                "inputs": ["docs/ACCEPTANCE.md", "ui/flows.md", "ui/screens.md"],
                "outputs": ["qa/TEST_PLAN.md", "qa/TEST_CASES.yaml", "qa/RELEASE_CHECKLIST.md"],
                "dependencies": ["T001"],
            },
            {
                "id": "T005",
                "owner": "coder",
                "title": "Create first playable task patch",
                "inputs": ["docs/TASKS.yaml", "docs/ACCEPTANCE.md", "ui/screens.md"],
                "outputs": ["patches/changes.patch"],
                "dependencies": ["T001", "T004"],
            },
        ]
    }
    return yaml.safe_dump(tasks, allow_unicode=True, sort_keys=False)


def _template_acceptance_md(game_request: str) -> str:
    return f"""# Scope
- Mobile 2D casual game based on: {game_request}
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
"""


def _template_ui_flows() -> str:
    return """# Flows
1. Home -> Start -> In-Run -> Result -> Retry/Home
2. In-Run Fail -> Continue Modal (Reward Ad) -> Resume -> Result
3. Result -> Interstitial (if eligible) -> Home
4. Home -> Shop -> IAP purchase flow -> Home
"""


def _template_ui_screens() -> str:
    return """# Screens
- Home: start, shop, settings, currency, best score
- In-Run HUD: score, energy/progress, pause
- Fail/Continue Modal: watch ad / skip
- Result: score summary, rewards, retry, home
- Shop: remove ads, coin packs, restore purchase
- Settings: sound, vibration, policy links
"""


def _template_ui_components_json() -> str:
    data = {
        "components": [
            {"id": "btn_start", "type": "Button", "screen": "Home", "action": "start_run"},
            {"id": "hud_score", "type": "Text", "screen": "In-Run HUD", "binding": "run.score"},
            {"id": "modal_continue", "type": "Modal", "screen": "Fail/Continue Modal", "actions": ["watch_ad", "skip"]},
            {"id": "btn_retry", "type": "Button", "screen": "Result", "action": "retry"},
            {"id": "iap_remove_ads", "type": "IAPCard", "screen": "Shop", "productId": "remove_ads"},
        ]
    }
    return json.dumps(data, ensure_ascii=False, indent=2) + "\n"


def _template_art_direction() -> str:
    return """# Art Direction
- Style: cute casual, high readability, soft outlines
- Palette: warm pastel base, high-contrast interactive CTA colors
- Character: round silhouette, 3-frame idle + 5-frame action
- Environment: layered parallax background, simple shape language
- Export: PNG, power-of-two where relevant, mobile-friendly atlas batching
"""


def _template_asset_list_json() -> str:
    return """{
  "characters": ["ai_pet_base", "ai_pet_happy", "ai_pet_tired"],
  "ui": ["btn_primary", "btn_secondary", "panel_card", "icon_coin", "icon_ad"],
  "environment": ["bg_layer_1", "bg_layer_2", "obstacle_set_a", "pickup_set_a"],
  "fx": ["fx_collect", "fx_fail", "fx_levelup"]
}
"""


def _template_story_bible() -> str:
    return """# Story Bible
- Premise: player raises a small AI companion that learns from each run.
- Tone: cozy, playful, optimistic.
- World Rule: energy from daily adventures improves AI traits.
- Characters:
  - Player: guide and trainer.
  - AI Buddy: curious learner evolving through milestones.
"""


def _template_dialogues() -> str:
    return """# Dialogues
- Start: "오늘도 같이 성장해보자!"
- Fail: "괜찮아, 이번 경험도 데이터야!"
- Reward Ad Prompt: "광고를 보고 한 번 더 도전할까?"
- Upgrade: "새로운 기능을 배웠어!"
- Return Home: "휴식하고 다음 탐험을 준비하자."
"""


def _template_qa_plan() -> str:
    return """# QA Plan
- Scope: gameplay loop, ad flow, IAP flow, error handling.
- Devices: at least 2 Android classes + 1 iOS simulator baseline.
- Priority:
  - P0: crash, progression block, purchase failure.
  - P1: ad edge cases, UI overlap, localization truncation.
  - P2: balancing and polish defects.
"""


def _template_qa_cases_yaml() -> str:
    cases = {
        "cases": [
            {"id": "QA-001", "title": "Start run from home", "steps": ["Open app", "Tap Start"], "expected": "Run scene starts"},
            {"id": "QA-002", "title": "Fail and retry loop", "steps": ["Start run", "Trigger fail", "Tap Retry"], "expected": "New run starts"},
            {"id": "QA-003", "title": "Reward ad fallback", "steps": ["Fail", "Open continue modal", "Ad unavailable"], "expected": "User can continue via normal retry path"},
            {"id": "QA-004", "title": "IAP remove ads toggle", "steps": ["Open shop", "Purchase remove_ads"], "expected": "Interstitial disabled"},
        ]
    }
    return yaml.safe_dump(cases, allow_unicode=True, sort_keys=False)


def _template_release_checklist() -> str:
    return """# Release Checklist
- [ ] Smoke test pass (start, fail, retry, home)
- [ ] Reward ad and interstitial behavior verified
- [ ] IAP purchase and restore tested
- [ ] No secrets/API keys in repository
- [ ] Patch review completed
"""


def _template_patch() -> str:
    return """diff --git a/orchestrator/local_first_task.txt b/orchestrator/local_first_task.txt
new file mode 100644
index 0000000..1111111
--- /dev/null
+++ b/orchestrator/local_first_task.txt
@@ -0,0 +1,5 @@
+First playable task scaffold (local template mode)
+- Define state: HOME -> RUNNING -> RESULT
+- Track score and retry count
+- Add one smoke test entry in qa plan
+- Replace this scaffold with production diff in API mode
"""


def _local_content_for_output(rel_path: str, game_request: str, task: Dict) -> str:
    path = rel_path.strip()
    if path == "docs/TASKS.yaml":
        return _template_tasks_yaml()
    if path == "docs/ACCEPTANCE.md":
        return _template_acceptance_md(game_request)
    if path == "ui/flows.md":
        return _template_ui_flows()
    if path == "ui/screens.md":
        return _template_ui_screens()
    if path == "ui/components.json":
        return _template_ui_components_json()
    if path == "assets/ART_DIRECTION.md":
        return _template_art_direction()
    if path == "assets/asset_list.json":
        return _template_asset_list_json()
    if path == "story/STORY_BIBLE.md":
        return _template_story_bible()
    if path == "story/DIALOGUES.md":
        return _template_dialogues()
    if path == "qa/TEST_PLAN.md":
        return _template_qa_plan()
    if path == "qa/TEST_CASES.yaml":
        return _template_qa_cases_yaml()
    if path == "qa/RELEASE_CHECKLIST.md":
        return _template_release_checklist()
    if path == "patches/changes.patch":
        return _template_patch()

    if path.endswith(".json"):
        return "{}\n"
    if path.endswith(".yaml") or path.endswith(".yml"):
        return "items: []\n"
    if path.endswith(".md"):
        return f"# {task.get('title') or task.get('id')}\n\n- request: {game_request}\n- owner: {task.get('owner')}\n"
    return f"# generated for {task.get('id')}\n"


def run_pm_bootstrap_api(game_request: str) -> None:
    pm_instructions = _read(OWNER_INSTRUCTIONS["pm"])
    prompt = f"""Create the files as specified.
User request:
{game_request}

Return content in file blocks, exactly:
--- file: docs/TASKS.yaml ---
...yaml...
--- end ---
--- file: docs/ACCEPTANCE.md ---
...markdown...
--- end ---
"""
    output = ask(pm_instructions, prompt)
    files = _extract_files(output)
    if not files:
        raise RuntimeError("PM bootstrap did not include file blocks.")
    for rel_path, content in files:
        _write(ROOT / rel_path.strip(), content.rstrip() + "\n")
    print("[green]PM bootstrap generated docs/TASKS.yaml and docs/ACCEPTANCE.md[/green]")


def run_pm_bootstrap_local(game_request: str) -> None:
    _write(DOCS / "TASKS.yaml", _template_tasks_yaml())
    _write(DOCS / "ACCEPTANCE.md", _template_acceptance_md(game_request))
    print("[green]PM bootstrap generated docs/TASKS.yaml and docs/ACCEPTANCE.md (local templates)[/green]")


def _normalize_tasks(data) -> List[Dict]:
    if isinstance(data, dict) and isinstance(data.get("tasks"), list):
        tasks = data["tasks"]
    elif isinstance(data, list):
        tasks = data
    else:
        raise RuntimeError("docs/TASKS.yaml must be a list or contain a top-level 'tasks' list.")

    normalized: List[Dict] = []
    for idx, task in enumerate(tasks):
        if not isinstance(task, dict):
            raise RuntimeError(f"TASKS.yaml task at index {idx} must be a mapping.")
        if "id" not in task or "owner" not in task:
            raise RuntimeError(f"TASKS.yaml task at index {idx} requires 'id' and 'owner'.")
        task_id = str(task["id"]).strip()
        owner = str(task["owner"]).strip()
        dependencies = task.get("dependencies", [])
        inputs = task.get("inputs", [])
        outputs = task.get("outputs", [])
        if not isinstance(dependencies, list) or not isinstance(inputs, list) or not isinstance(outputs, list):
            raise RuntimeError(f"TASKS.yaml task '{task_id}' has invalid list fields.")
        normalized.append(
            {
                "id": task_id,
                "owner": owner,
                "title": str(task.get("title", "")).strip(),
                "dependencies": [str(dep).strip() for dep in dependencies],
                "inputs": [str(i).strip() for i in inputs],
                "outputs": [str(o).strip() for o in outputs],
            }
        )
    return normalized


def load_tasks() -> List[Dict]:
    tasks_path = DOCS / "TASKS.yaml"
    if not tasks_path.exists():
        raise RuntimeError("docs/TASKS.yaml not found after PM bootstrap.")
    parsed = yaml.safe_load(_read(tasks_path))
    tasks = _normalize_tasks(parsed)
    if not tasks:
        raise RuntimeError("docs/TASKS.yaml has no tasks to execute.")
    return tasks


def load_tasks_with_fallback(mode: str, game_request: str) -> List[Dict]:
    try:
        tasks = load_tasks()
        owners = {t["owner"] for t in tasks}
        unknown = sorted([o for o in owners if o not in OWNER_INSTRUCTIONS])
        if unknown:
            raise RuntimeError(f"Unsupported owners in TASKS.yaml: {unknown}")
        return tasks
    except Exception as exc:
        if mode == "api":
            raise RuntimeError(f"Failed to parse TASKS.yaml in api mode: {exc}") from exc
        print(f"[yellow]Invalid TASKS.yaml ({exc}); regenerating local template TASKS.yaml[/yellow]")
        run_pm_bootstrap_local(game_request)
        tasks = load_tasks()
        owners = {t["owner"] for t in tasks}
        unknown = sorted([o for o in owners if o not in OWNER_INSTRUCTIONS])
        if unknown:
            raise RuntimeError(f"Local fallback TASKS.yaml has unsupported owners: {unknown}")
        return tasks


def _read_inputs(inputs: List[str]) -> str:
    chunks: List[str] = []
    for rel in inputs:
        path = ROOT / rel
        if path.exists() and path.is_file():
            chunks.append(f"\n\nInput file: {rel}\n{_read(path)}")
    return "".join(chunks)


def _base_context() -> str:
    parts: List[str] = []
    if (DOCS / "TASKS.yaml").exists():
        parts.append("\n\nCurrent docs/TASKS.yaml:\n" + _read(DOCS / "TASKS.yaml"))
    if (DOCS / "ACCEPTANCE.md").exists():
        parts.append("\n\nCurrent docs/ACCEPTANCE.md:\n" + _read(DOCS / "ACCEPTANCE.md"))
    return "".join(parts)


def run_owner_task_api(game_request: str, task: Dict) -> None:
    owner = task["owner"]
    if owner not in OWNER_INSTRUCTIONS:
        raise RuntimeError(f"Unknown owner '{owner}' in task '{task['id']}'.")

    instructions = _read(OWNER_INSTRUCTIONS[owner])
    requested_outputs = task["outputs"] or DEFAULT_OWNER_OUTPUTS.get(owner, [])
    if not requested_outputs:
        raise RuntimeError(f"Task '{task['id']}' has no outputs and no default outputs for owner '{owner}'.")

    output_blocks = "\n".join([f"--- file: {rel} ---\n...\n--- end ---" for rel in requested_outputs])
    task_title = task["title"] or task["id"]
    task_inputs = _read_inputs(task["inputs"])
    context = _base_context() + task_inputs

    prompt = f"""Execute this one task and output only the requested file blocks.
User request:
{game_request}

Task:
- id: {task["id"]}
- owner: {owner}
- title: {task_title}
- dependencies: {task["dependencies"]}
- inputs: {task["inputs"]}
- outputs: {requested_outputs}

Return content in file blocks, exactly:
{output_blocks}
{context}
"""
    output = ask(instructions, prompt)
    files = _extract_files(output)
    if not files:
        raise RuntimeError(f"Owner '{owner}' task '{task['id']}' did not include file blocks.")

    written: Set[str] = set()
    for rel_path, content in files:
        normalized = rel_path.strip()
        _write(ROOT / normalized, content.rstrip() + "\n")
        written.add(normalized)

    missing = [rel for rel in requested_outputs if rel not in written]
    if missing:
        raise RuntimeError(f"Task '{task['id']}' missing outputs: {missing}")

    print(f"[green]Task {task['id']} ({owner}) completed via API[/green]")


def run_owner_task_local(game_request: str, task: Dict) -> None:
    owner = task["owner"]
    requested_outputs = task["outputs"] or DEFAULT_OWNER_OUTPUTS.get(owner, [])
    if not requested_outputs:
        raise RuntimeError(f"Task '{task['id']}' has no outputs and no default outputs for owner '{owner}'.")

    for rel in requested_outputs:
        content = _local_content_for_output(rel, game_request, task)
        _write(ROOT / rel, content if content.endswith("\n") else content + "\n")
    print(f"[green]Task {task['id']} ({owner}) completed via local template[/green]")


def run_task_pipeline(game_request: str, tasks: List[Dict], mode: str) -> None:
    completed: Set[str] = set()
    total = len(tasks)

    while len(completed) < total:
        progressed = False
        for task in tasks:
            task_id = task["id"]
            if task_id in completed:
                continue
            deps = task["dependencies"]
            if any(dep not in completed for dep in deps):
                continue

            if mode == "local":
                run_owner_task_local(game_request, task)
            elif mode == "api":
                run_owner_task_api(game_request, task)
            else:
                try:
                    run_owner_task_api(game_request, task)
                except Exception as exc:
                    print(f"[yellow]Task {task_id} API failed ({exc}); falling back to local template[/yellow]")
                    run_owner_task_local(game_request, task)

            completed.add(task_id)
            progressed = True

        if not progressed:
            unresolved = [t["id"] for t in tasks if t["id"] not in completed]
            raise RuntimeError(f"Could not resolve remaining tasks due to dependency issues: {unresolved}")


def _parse_args():
    parser = argparse.ArgumentParser(description="Task-driven game orchestrator")
    parser.add_argument("game_request", help='예: "게임 만들어줘: 모바일 2D ... "')
    parser.add_argument(
        "--mode",
        choices=["local", "api", "auto"],
        default="local",
        help="local: template only, api: OpenAI only, auto: api then fallback local",
    )
    return parser.parse_args()


def main() -> None:
    args = _parse_args()
    game_request = args.game_request.strip()
    mode = args.mode

    if mode == "local":
        run_pm_bootstrap_local(game_request)
    elif mode == "api":
        run_pm_bootstrap_api(game_request)
    else:
        try:
            run_pm_bootstrap_api(game_request)
        except Exception as exc:
            print(f"[yellow]PM bootstrap API failed ({exc}); falling back to local template[/yellow]")
            run_pm_bootstrap_local(game_request)
            mode = "local"

    tasks = load_tasks_with_fallback(mode, game_request)
    run_task_pipeline(game_request, tasks, mode)

    print("\n[cyan]Next steps:[/cyan]")
    print("1) Review generated files from docs/, ui/, assets/, story/, qa/, patches/")
    print("2) Apply patch if present: git apply patches/changes.patch")
    print('3) Commit: git add . && git commit -m "agent: task-driven batch"')


if __name__ == "__main__":
    main()
