# Jehad — world, interface and audio handoff

**Combined checkpoint:** [Jehad_V1.27.unity](../../Assets/_Project/Scenes/Jehad/Jehad_V1.27.unity)  
**Unity:** 6000.3.27f1, URP  
**Status:** implementation/integration candidate; native Unity acceptance and production-player integration are pending.

This checkpoint combines the finishing changes, reproducible source/model checks
and contribution documentation in one version. Existing scene snapshots remain
available under `Assets/_Project/Scenes/Jehad`. The integrator-owned
`Echoheart.unity` and build settings are preserved.

| Document | Purpose |
|---|---|
| [Contribution](CONTRIBUTION.md) | Delivered systems, ownership, fixes and evidence boundaries |
| [Acceptance](ACCEPTANCE.md) | One ordered native review route and sign-off matrix |
| [Integration for Tariq and Mohammed](INTEGRATION.md) | Role checklists, exact Inspector bindings, event/retry ownership and joint release tests |
| [Performance](PERFORMANCE.md) | Implemented optimizations, measured formatter result and native capture plan |
| [Phase 3 report](../phase3/final_report.md) | Current report with pending build, media and native evidence identified |
| [V1.27 note](versions/Jehad_V1.27.md) | Exact combined-checkpoint changes |
| [Validation evidence](versions/Jehad_V1.27.validation.json) | Reproducible source/model/scene results |

Run the combined checks from the repository root:

```sh
python Tools/Jehad/V1_27/run_checks.py --dotnet /absolute/path/to/dotnet --report /tmp/jehad-v127-validation.json
```

The command runs 15 pure C# suites, the namespace audit and scene/asset checks.
It does not launch Unity. See the [tool README](../../Tools/Jehad/V1_27/README.md)
for dependencies and partial-staging options.

The review scene still uses the movement-only capsule. Its controls are WASD,
Left Shift, Space, middle-mouse orbit, wheel zoom, E interaction and Esc pause.
Production inhale/attack input is an integration dependency; native context
actions that damage actors directly are test fixtures, not playable combat.

The next sign-off is the [native acceptance route](ACCEPTANCE.md), followed by
the integrated-player playthrough and actual build/profiler/media evidence.
