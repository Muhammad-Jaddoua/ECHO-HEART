# Native acceptance and release gates

**Checkpoint:** Jehad_V1.27. **Recorded native results:** not run in this environment.
The EditorOnly actions reset the run. Execute them separately and do not overlap
coroutines. Preserve real logs/screenshots with the tested commit and hardware.

## Ordered review route

1. Open [Jehad_V1.27.unity](../../Assets/_Project/Scenes/Jehad/Jehad_V1.27.unity)
   in Unity 6000.3.27f1. Wait for import/compilation and inspect Console errors.
2. On **Jehad V1.27 UI acceptance harness**, run **Final review → Run pickup
   pause feedback checks (resets run)**. It collects through the actual trigger
   while counter/presentation updates are temporarily held, checks the current
   pause summary, then verifies one deferred relic-sound request on resume and
   no replay on another pause/resume. It restores component enabled states and
   removes its observers in finally. Source bindings do not prove audible sound.
3. On **Jehad V1.27 world polish acceptance harness**, run **World polish → Run
   chest and asset checks (resets run)**. Inspect both chests manually as well,
   including paused opening, near-lid collection and reward persistence.
4. On the UI acceptance root, run the existing **UI review → Run navigation and
   settings checks (resets run)** and **Audio review → Run title cue cleanup
   checks (resets run)** separately. The latter uses real death/retry and
   Guardian/Heart event paths, with direct test damage rather than player combat.
5. Manually traverse the main-world routes, water edges, slopes, secrets and
   arena entry. Inspect collision, camera clearance, nature materials, visibility,
   navigation, particle placement, regional music, mute/unmute and overall mix.
6. After [production-player integration](INTEGRATION.md), complete the actual
   input-driven loop using Sword, Bow and Fire against the Guardian, including
   death/retry in both areas. Record build profiling and structured playtests.

Focused movement, sparkle, preference, objective, vitality, Guardian music and
pause-summary actions remain available on the inherited review components for
isolating a failure. Their version notes document their fixtures and limits.

## Sign-off matrix

| Gate | Required evidence | Current status |
|---|---|---|
| Unity import/compile | Clean Console for the tested commit | NOT RUN |
| World collision/navigation | Walkthrough and enemy path checks | NOT RUN |
| Relics/chests/secrets | 16 total, two chests, one collection per relic, reset rules | Authored/source checked; native pending |
| Pause snapshot/deferred feedback | New combined context action plus ordinary immediate pause | Fixture supplied; NOT RUN |
| Flow and menus | Title/pause/death/retry/complete, navigation and display revert | Pure policies checked; native pending |
| Audio/visual pass | Auditioned mix, music transitions, material/VFX/camera screenshots | Setup supplied; native pending |
| Production player/attacks | Real prefab, inhale, ability holder, successful-use feedback | Integration dependency |
| Guardian wins | Input-driven Sword/Bow/Fire wins and Heart ending | Integration/native dependency |
| Build performance | Declared PC/specs, target, CPU/GPU/frame/GC captures | NOT PROVIDED; see Performance |
| Playtests | At least three testers, observations, severity and fixes | NOT PROVIDED |
| Final build/media | Windows RC, matching fallback recording, tag/release | Team release evidence pending |
| Repo workflow | Milestone/issues and teammate review records | Team record verification pending |

For each native failure, record the commit, scene/build, reproduction steps,
expected/actual behavior, severity, fix and retest result. A source-implemented
fix remains **awaiting native retest** until that evidence exists. Do not mark
these gates passed from model-test counts or unexecuted context actions.
