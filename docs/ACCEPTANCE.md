# Scope
- Unity 2D mobile portrait game, cute casual style.
- Core loop: Start run -> dodge/collect -> score -> fail -> reward/retry -> progression.
- Monetization: rewarded ad (continue/reward), interstitial (between runs), IAP (remove ads + coin pack).
- Deliverables: task plan, UI flow/screens/components, one first-task implementation patch.

# Non-Goals
- Full production art pipeline.
- Backend account/auth system.
- LiveOps events, battle pass, social features.
- Final balancing for retention/LTV.

# Quality Gates
- Build: Unity Android build succeeds in CI and local.
- Test: basic playmode smoke test for boot, start run, fail, retry.
- Lint: C# analyzers/format checks pass.
- Secret-scan: no API keys or signing secrets in repo/history.

# Done Checklist
- [ ] `docs/TASKS.yaml` exists and task dependencies are coherent.
- [ ] `ui/flows.md` includes happy path, fail path, ad path, error states.
- [ ] `ui/screens.md` defines required screens and interaction states.
- [ ] `ui/components.json` is valid JSON and implementable in Unity uGUI.
- [ ] `patches/changes.patch` applies cleanly with `git apply`.
- [ ] First playable task is represented in patch and testable locally.
- [ ] Ads/IAP are stubbed with clear integration points.
- [ ] No secrets committed; repository passes secret scan.
