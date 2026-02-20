import os, re, pathlib
from rich import print
from openai import OpenAI

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
client = OpenAI(api_key=os.environ.get("OPENAI_API_KEY"))

def _read(p: pathlib.Path) -> str:
    return p.read_text(encoding="utf-8")

def _write(p: pathlib.Path, s: str):
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(s, encoding="utf-8")

def _extract_files(block: str):
    pattern = r"--- file:\s*(.+?)\s*---\n(.*?)\n--- end ---"
    return re.findall(pattern, block, re.DOTALL)

def ask(instructions: str, user_input: str) -> str:
    r = client.responses.create(
        model=MODEL,
        instructions=instructions,
        input=user_input,
    )
    return r.output_text

def run_pm(game_request: str):
    pm_instructions = _read(ROOT / "agents/pm.md")
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
    out = ask(pm_instructions, prompt)
    files = _extract_files(out)
    if not files:
        raise RuntimeError("PM output did not include file blocks.")
    for path, content in files:
        _write(ROOT / path.strip(), content.rstrip() + "\n")
    print("[green]PM generated docs/TASKS.yaml and docs/ACCEPTANCE.md[/green]")

def run_ui(game_request: str):
    ui_instructions = _read(ROOT / "agents/ui.md")
    context = ""
    tasks_path = DOCS / "TASKS.yaml"
    if tasks_path.exists():
        context = "\n\nTASKS.yaml:\n" + _read(tasks_path)

    prompt = f"""Generate UI artifacts as specified.
User request:
{game_request}
{context}

Return content in file blocks, exactly:
--- file: ui/flows.md ---
...
--- end ---
--- file: ui/screens.md ---
...
--- end ---
--- file: ui/components.json ---
...valid json...
--- end ---
"""
    out = ask(ui_instructions, prompt)
    files = _extract_files(out)
    if not files:
        raise RuntimeError("UI output did not include file blocks.")
    for path, content in files:
        _write(ROOT / path.strip(), content.rstrip() + "\n")
    print("[green]UI generated ui/flows.md, ui/screens.md, ui/components.json[/green]")

def run_art(game_request: str):
    art_instructions = _read(ROOT / "agents/art.md")
    context = ""
    tasks_path = DOCS / "TASKS.yaml"
    if tasks_path.exists():
        context += "\n\nTASKS.yaml:\n" + _read(tasks_path)
    screens_path = ROOT / "ui/screens.md"
    if screens_path.exists():
        context += "\n\nui/screens.md:\n" + _read(screens_path)

    prompt = f"""Generate art artifacts as specified.
User request:
{game_request}
{context}

Return content in file blocks, exactly:
--- file: assets/ART_DIRECTION.md ---
...
--- end ---
--- file: assets/asset_list.json ---
...valid json...
--- end ---
"""
    out = ask(art_instructions, prompt)
    files = _extract_files(out)
    if not files:
        raise RuntimeError("Art output did not include file blocks.")
    for path, content in files:
        _write(ROOT / path.strip(), content.rstrip() + "\n")
    print("[green]Art generated assets/ART_DIRECTION.md, assets/asset_list.json[/green]")

def run_story(game_request: str):
    story_instructions = _read(ROOT / "agents/story.md")
    context = ""
    tasks_path = DOCS / "TASKS.yaml"
    if tasks_path.exists():
        context += "\n\nTASKS.yaml:\n" + _read(tasks_path)

    prompt = f"""Generate story artifacts as specified.
User request:
{game_request}
{context}

Return content in file blocks, exactly:
--- file: story/STORY_BIBLE.md ---
...
--- end ---
--- file: story/DIALOGUES.md ---
...
--- end ---
"""
    out = ask(story_instructions, prompt)
    files = _extract_files(out)
    if not files:
        raise RuntimeError("Story output did not include file blocks.")
    for path, content in files:
        _write(ROOT / path.strip(), content.rstrip() + "\n")
    print("[green]Story generated story/STORY_BIBLE.md, story/DIALOGUES.md[/green]")

def run_qa(game_request: str):
    qa_instructions = _read(ROOT / "agents/qa.md")
    tasks = (DOCS / "TASKS.yaml").read_text(encoding="utf-8") if (DOCS / "TASKS.yaml").exists() else ""
    acceptance = (DOCS / "ACCEPTANCE.md").read_text(encoding="utf-8") if (DOCS / "ACCEPTANCE.md").exists() else ""
    ui_flows = (ROOT / "ui/flows.md").read_text(encoding="utf-8") if (ROOT / "ui/flows.md").exists() else ""
    ui_screens = (ROOT / "ui/screens.md").read_text(encoding="utf-8") if (ROOT / "ui/screens.md").exists() else ""

    prompt = f"""Generate QA artifacts as specified.
User request:
{game_request}

TASKS.yaml:
{tasks}

ACCEPTANCE.md:
{acceptance}

ui/flows.md:
{ui_flows}

ui/screens.md:
{ui_screens}

Return content in file blocks, exactly:
--- file: qa/TEST_PLAN.md ---
...
--- end ---
--- file: qa/TEST_CASES.yaml ---
...valid yaml...
--- end ---
--- file: qa/RELEASE_CHECKLIST.md ---
...
--- end ---
"""
    out = ask(qa_instructions, prompt)
    files = _extract_files(out)
    if not files:
        raise RuntimeError("QA output did not include file blocks.")
    for path, content in files:
        _write(ROOT / path.strip(), content.rstrip() + "\n")
    print("[green]QA generated qa/TEST_PLAN.md, qa/TEST_CASES.yaml, qa/RELEASE_CHECKLIST.md[/green]")

def run_coder(game_request: str):
    coder_instructions = _read(ROOT / "agents/coder.md")
    tasks = (DOCS / "TASKS.yaml").read_text(encoding="utf-8") if (DOCS / "TASKS.yaml").exists() else ""
    acceptance = (DOCS / "ACCEPTANCE.md").read_text(encoding="utf-8") if (DOCS / "ACCEPTANCE.md").exists() else ""
    prompt = f"""Implement ONE small task only.
Constraints:
- Output ONLY a unified diff patch in a file block for patches/changes.patch
- Keep patch minimal
- No secrets

User request:
{game_request}

TASKS.yaml:
{tasks}

ACCEPTANCE.md:
{acceptance}

Return exactly:
--- file: patches/changes.patch ---
...unified diff...
--- end ---
"""
    out = ask(coder_instructions, prompt)
    files = _extract_files(out)
    if not files:
        raise RuntimeError("Coder output did not include a patch file block.")
    for path, content in files:
        _write(ROOT / path.strip(), content.rstrip() + "\n")
    print("[green]Coder generated patches/changes.patch[/green]")

def main():
    import sys
    if len(sys.argv) < 2:
        print("[red]Usage: python main.py \"게임 만들어줘: ...\"[/red]")
        raise SystemExit(1)
    game_request = sys.argv[1].strip()

    run_pm(game_request)
    run_ui(game_request)
    run_art(game_request)
    run_story(game_request)
    run_qa(game_request)
    run_coder(game_request)

    print("\n[cyan]Next steps:[/cyan]")
    print("1) Review generated files in docs/, ui/, assets/, story/, qa/, patches/")
    print("2) Apply patch:  git apply patches/changes.patch")
    print("3) Commit:       git add . && git commit -m \"agent: initial batch\"")

if __name__ == "__main__":
    main()
