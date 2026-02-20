import os, re, pathlib
from rich import print
from openai import OpenAI

ROOT = pathlib.Path(__file__).parent
DOCS = ROOT / "docs"
PATCHES = ROOT / "patches"
DOCS.mkdir(exist_ok=True)
PATCHES.mkdir(exist_ok=True)

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
    run_coder(game_request)

    print("\n[cyan]Next steps:[/cyan]")
    print("1) Review generated files in docs/, ui/, patches/")
    print("2) Apply patch:  git apply patches/changes.patch")
    print("3) Commit:       git add . && git commit -m \"agent: initial batch\"")

if __name__ == "__main__":
    main()
