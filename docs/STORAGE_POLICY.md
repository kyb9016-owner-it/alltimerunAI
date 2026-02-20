# Storage Policy

## Keep In Git (source of truth)
- `Assets/` source files you edit directly (scripts, prefabs, scenes, configs)
- `ProjectSettings/`, `Packages/`
- docs/specs/checklists needed for team handoff

## Never Track In Git
- Unity generated caches: `Library/`, `Temp/`, `Logs/`, `Obj/`, `Build/`, `Builds/`, `UserSettings/`
- local Python environments: `venv/`, `source/`
- IDE/project cache files (`*.csproj`, `*.sln`, etc.)

## Move To Cloud Storage
- build artifacts (`.apk`, `.aab`, `.ipa`, zipped builds)
- long-term backups and old generated batches
- large media captures and profiling dumps

## Recommended Cloud Layout
- `releases/` : signed builds by version
- `backups/` : periodic project snapshots
- `artifacts/` : QA logs, recordings, perf dumps

## Operational Rule
- keep only recent working set locally
- archive old builds/logs to cloud weekly
- restore from cloud only when needed
