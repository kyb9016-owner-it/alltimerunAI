# Scope
- Mobile 2D casual game based on: 스킬 확장 검증
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
