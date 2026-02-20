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


def run_pm_bootstrap(game_request: str) -> None:
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


def run_owner_task(game_request: str, task: Dict) -> None:
    owner = task["owner"]
    if owner not in OWNER_INSTRUCTIONS:
        raise RuntimeError(f"Unknown owner '{owner}' in task '{task['id']}'.")

    instructions = _read(OWNER_INSTRUCTIONS[owner])
    requested_outputs = task["outputs"] or DEFAULT_OWNER_OUTPUTS.get(owner, [])
    if not requested_outputs:
        raise RuntimeError(f"Task '{task['id']}' has no outputs and no default outputs for owner '{owner}'.")

    output_blocks = "\n".join(
        [f"--- file: {rel} ---\n...\n--- end ---" for rel in requested_outputs]
    )
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

    print(f"[green]Task {task['id']} ({owner}) completed[/green]")


def run_task_pipeline(game_request: str, tasks: List[Dict]) -> None:
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
            run_owner_task(game_request, task)
            completed.add(task_id)
            progressed = True

        if not progressed:
            unresolved = [t["id"] for t in tasks if t["id"] not in completed]
            raise RuntimeError(f"Could not resolve remaining tasks due to dependency issues: {unresolved}")


def main() -> None:
    import sys

    if len(sys.argv) < 2:
        print('[red]Usage: python main.py "게임 만들어줘: ..."[/red]')
        raise SystemExit(1)

    game_request = sys.argv[1].strip()
    run_pm_bootstrap(game_request)
    tasks = load_tasks()
    run_task_pipeline(game_request, tasks)

    print("\n[cyan]Next steps:[/cyan]")
    print("1) Review generated files from docs/, ui/, assets/, story/, qa/, patches/")
    print("2) Apply patch if present: git apply patches/changes.patch")
    print('3) Commit: git add . && git commit -m "agent: task-driven batch"')


if __name__ == "__main__":
    main()
