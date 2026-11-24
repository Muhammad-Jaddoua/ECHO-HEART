# Phase 2 Checkpoint — Systems Vertical Slice

**Project:** ECHOHEART · SWE 402, KFUPM · Term 261  
**Team:** Tariq Alothman — player, camera and integration; Muhammad Jaddoua — combat and AI; Jehad Alharbi — world, flow, interface and audio  
**Checkpoint updated:** 24 November 2026 · **Planned Phase 2 milestone:** 19 November 2026

## 1. Checkpoint summary and reviewed versions

The latest integrated slice is **`Assets/_Project/Scenes/Echoheart.unity` on `feature/20-integration`**, submitted in [PR #36][pr36]. It combines the production player with Jehad V1.25's world, menus, HUD, audio and retry/Heart flow. Tariq reports an end-to-end Windows RC1 playthrough, including death/retry and Play Again. The integration is awaiting merge and review; the tested branch and merged `main` are therefore recorded separately.

| Snapshot | Revision | Evidence / limitation |
|---|---|---|
| Integrated playable candidate | [`75cd32f` — Echoheart.unity][integrated-scene] | PR #36 reports Editor checks and a 136 MB Windows RC1 with zero build errors; no downloadable build or matching recording is linked in the inspected PR |
| Merged repository | [`798ec6b`][main-commit] | Movement/camera PR #34 is merged; inhale PR #35 and integration PR #36 remain open |
| Latest Jehad review checkpoint | [Jehad_V1.27.unity](../../Assets/_Project/Scenes/Jehad/Jehad_V1.27.unity) | Movement-only review capsule; additional V1.26/V1.27 fixes need reconciliation with the V1.25-based integration |

Repository and PR evidence was reviewed for this report. Native results below are attributed to the teammate who recorded them; this documentation update did not rerun Unity or the Windows build. Older handoff documents describe their own snapshots, so their pending flags should be read alongside the newer PR evidence here.

## 2. Playable slice summary

**Scope:** a single-player, third-person ability-copying adventure in one Unity scene containing two connected areas: a semi-open island and a compact Guardian arena. Sword, Bow and Fire are the only powers, with one held at a time. The intended run is approximately 8–10 minutes; representative completion times have not yet been recorded.

| Boundary / stage | Currently implemented behavior | Verification status |
|---|---|---|
| Start | Title → Begin adventure/New Game → main entry, full health and no power | Reported exercised in integrated RC1 |
| Main loop | Explore → encounter an enemy → inhale to replace the held power, or defeat it → use the power → find optional rewards/secrets → approach the arena | Real capture and attacks reported in PRs #35/#36; first-time usability testing pending |
| Progression | Replenishing sources supply all three powers near entry/arena approach; the Guardian marker identifies the objective | Scene bindings inspected; route clarity still needs outside testers |
| Boss boundary | Enter arena → gate closes, arena retry becomes active, Guardian begins telegraph/attack/recovery cycles | Real arena entry reported in PR #36 |
| Win | Defeat Guardian → approach Heart → press E → restoration/completion screen; Play Again starts a new run | Reported end-to-end by Tariq; Editor report includes a 14-hit Bow defeat |
| Fail | Zero player HP → death/retry menu; falls use safe-position recovery and damage | Both area retries included in Tariq's reported run; repeatability stress pass remains |

Defeating an ordinary enemy does **not** award its power: successful inhale does. The boss cannot be inhaled, has no power immunity and can be fought using any one of the three powers. Recorded human wins with Sword and Fire are still required to validate fairness, alongside a recorded Bow run.

### Retry and session behavior

| Action | Health / power | World progression |
|---|---|---|
| Pause/resume | Preserved; gameplay suspended and menu navigation available | Relics/discoveries preserved |
| Main-area retry | Full player HP, clear power, return to entry | Reset main enemies; preserve collected relics, opened chests and discoveries |
| Boss-area retry | Full player and boss HP, retain selected power, re-arm encounter | Preserve optional rewards/discoveries; clear active projectiles |
| New Game / Play Again | Full HP and no power | Reset relics, chests, discoveries and completion state |
| Fall recovery | Safe teleport plus existing damage logic; reset motion/camera | No second run reset from the teleport callback |

**Known limitations:** the complete integration is still on an open PR; the world snapshot contains **16 optional relics and two secret chests**, exceeding the original 8–12 collectible target; models remain partly graybox; final animation, mix and performance acceptance are incomplete. Optional landmarks do not add a required collectible hunt or additional game areas. Further content expansion is frozen while integration, route length and stability are verified.

## 3. Systems implementation status

| System | Implementation and integration boundary | Current evidence |
|---|---|---|
| Controls and camera | `PlayerInputReader` reads the Input System map. `PlayerMotor` provides CharacterController movement, acceleration, gravity, buffered jump/coyote time. Cinemachine orbit/deocclusion maintains the player view | Merged [PR #34][pr34]: simulated-input movement/ledge/camera checks reported; camera feel needs outside testing |
| Inhale and power slot | `PlayerInhale`: Ready → Pulling → WaitRelease. Initial target checks range, cone and line of sight; pull refreshes the enemy protocol and cancels safely. `PlayerAbilityHolder` replaces one power and dispatches attacks through `AbilityAttackRunner` | [PR #35][pr35]: real enemy captures, blocked/out-of-range rejection, replacement and cancellation reported; awaiting merge |
| Interactions | Layer-filtered relic triggers, once-per-session IDs, proximity E prompts for chests/Heart, boss-gated completion | Authored in Jehad checkpoint; integrated completion reported, exhaustive reward/reset route pending |
| Health and attacks | Shared `IDamageable`/`Health`, team filtering, attack IDs, Sword hit window and a shared configurable Bow/Fire projectile | Muhammad's sandbox self-tests and scripted run; merged combat PRs |
| Enemies and challenge | Three data-driven enemy variants; replenishing spawners share a six-enemy cap. Runtime main navigation validates all six source positions; arena navigation is separate | Sandbox behavior verified in Muhammad's report; integrated traversal/path coverage pending |
| Boss | Explicit Idle → Telegraph → Attack → Recover cycle, with Defeated handling; close slam or ranged shot, warning visuals and recovery windows | Sandbox tests plus integrated arena/Bow evidence; three human power-specific wins pending |
| State and reset flow | `SessionFlowV13` owns Title, Playing, Paused, Dead and Complete. `RetryServiceV13` clears projectiles and performs one reset transaction; spawners own resets of their enemies | Pure flow checks and Tariq's integrated run; repeated-session regression remains |

**Current gameplay controls:** WASD move, mouse orbit, Space jump, hold RMB inhale, LMB use held power, E interact, Esc pause. RMB inhale and E interact deliberately replace the Phase 1 proposal's E/F mapping. PR #36 updates the HUD/Controls page to match the production player, removing review-only sprint, middle-mouse orbit and zoom instructions.

### Physics, collision and response reliability

- Explicit layers separate Player (8), Enemy (9), Boss (10), PlayerAttack (11), EnemyAttack (12), Environment (13), Collectible (14) and Trigger (15). Hit masks and Health team checks reject unintended targets.
- Sword uses overlap queries with a per-swing victim set; each attack has an ID, Health rejects repeated IDs, and projectiles have a resolved guard. Multiple colliders on one actor must still yield one hit.
- Projectiles sweep between positions with `SphereCastNonAlloc`, resolve the closest valid collision and ignore owner colliders. This addresses tunneling; thin-target and crowded-collider cases remain part of integration regression rather than a claim that every physics case is covered.
- Inhale checks sight from the body centre to prevent a mouth point inside a wall bypassing blockers. Cancelled pulls return agent ownership to the enemy brain, which re-seats the agent on navigation.
- Teleport disables the CharacterController before moving, synchronizes physics, and invokes motion/camera reset callbacks. Reward, capture, defeat and completion paths each have once-guards. These guards define repeatable interaction semantics; Unity physics is not being claimed as cross-machine deterministic simulation.

AI state diagrams, damage guards, tunables and detailed test cases are in [Muhammad's verification report](muhammad_verification.md). `BossBrain` owns boss events; a second Boss-mode health relay must not duplicate them. The player keeps one Player-mode relay, and the integration uses the existing HUD rather than stacking a second interface.

## 4. UI and audio first pass

**Interface:** the integrated scene uses `FlowMenuV117` / `UiHudV115` for player health, current power, relic count, objective bearing, interaction prompts, ranged aim and the Guardian bar. Title, pause, death and completion screens support retry/return flow. Settings include separate Music/SFX levels, interface preferences and display preview/revert. PR #36 fixes missing CanvasRenderers on runtime-created panels/icons and aligns hints with the real controls. Readability and navigation across resolutions still require native acceptance.

**Audio:** `WorldAudioV15` supplies six regional/Guardian music tracks, three ambient loops and 23 world cues. Its programmatic mix uses regional crossfades, pause/death ducking, separate Music/SFX settings and eight reusable spatial SFX voices. Water is spatial; forest/alpine ambience and music provide background layers. World/actor feedback connects pickups, power changes, hits, captures, warnings, death and restoration. The integrated scene also binds accepted Sword/projectile uses to world feedback.

The first pass therefore includes authored content and event wiring. The remaining audio check is an actual listening pass: test all three attacks and enemy/boss cues, near/far attenuation, pause/retry cleanup, zero-volume mute and duplicate sound sources. No completed native mixing evaluation or AudioMixer-asset implementation is asserted. Licenses and provenance are recorded in [Asset credits](../ASSET_CREDITS.md).

## 5. Playtest and debugging report

### Recorded setup and results

| Test / reporter | Setup and scope | Recorded outcome | Evidence boundary |
|---|---|---|---|
| Combat/AI — Muhammad | Unity 6000.3.27f1, `Sandbox_Muhammad`; component self-tests and `CombatPlayTest`, timeScale 2, harness-driven inputs | Health 10/10, Combat 17/17, Spawner 9/9, Boss 20/20, Boss bar 10/10; scripted run **45 passes, 0 failures** | [Verification report](muhammad_verification.md); not a first-time human playtest |
| Player — Tariq | Movement sandbox in PR #34; dummy and real enemy checks in PR #35 | About 6 m/s walk, 1.65 m jump, 1.4 m ledge cleared; camera retracts at a wall; all three real enemy types grant their powers; wall/range rejection, release and death cancellation reported | [PR #34][pr34], [PR #35][pr35]; real-enemy checks in #35 were temporary Play-mode setups |
| Integration — Tariq | `Echoheart.unity`, PR #36 head; Editor plus Windows RC1 | Acceptance harness pass reported; real capture/arena activation; 14 Bow hits defeat Guardian and unlock Heart; full build route includes pause, main/boss retry, completion and Play Again; zero errors reported | [PR #36][pr36]; 136 MB build reported, hardware/resolution, downloadable artifact and recording not supplied |
| World/UI policies — Jehad checkpoint | .NET 8.0.404 pure C# suites plus scene/source/asset checks | **15 suites, 9,436 assertions PASS** in the saved V1.27 report | [Validation JSON](../jehad/versions/Jehad_V1.27.validation.json); excludes Unity compilation, rendering, native input/physics and audible mix |

Muhammad's boss timings—Sword 2.7 s, Bow 6.8 s, Fire 5.2 s—are ideal scripted game-time results with a pinned, kept-alive player, not human completion times. They motivated Bow cooldown **0.6 → 0.5 s**; Fire speed changed **20 → 14** to distinguish the shorter-range attack. Recheck asset tuning after real wins. An earlier scripted run's three wait-timing failures were corrected in the test harness; the report records the subsequent 45/0 run.

### Findings, priorities and concrete fixes

P1 blocks completion; P2 affects clarity, feel or required evidence; P3 is polish. Branch fixes remain subject to clean-clone review and merged-build regression.

| ID / priority | Finding and impact | Fix / next check | Owner and status |
|---|---|---|---|
| C01 · P1 | `main` does not yet contain the complete inhale/integration loop | Review and merge #35 before #36; import a clean clone and replay start → both retries → Heart → Play Again | Tariq + teammate reviewer; open PRs |
| C02 · P1 | Runtime UI panels/icons were invisible and produced raycast errors | Explicit `CanvasRenderer` requirement; verify title, HUD, bars and buttons on a fresh clone/build | Fixed and reported retested in #36; awaiting merge |
| C03 · P1 | Runtime actor feedback creation threw during MaterialPropertyBlock field initialization | Create it lazily; repeat hit/capture/defeat effects without exceptions | Fixed and reported retested in #36; awaiting merge |
| C04 · P1 | Inhale could bypass a wall when its mouth origin entered the wall | Body-centre line-of-sight check; repeat flush-wall, release, death and out-of-range cases | Tariq; fix/checks reported in #35 |
| C05 · P2 | Integrated scene is V1.25-based; later pause/pickup corrections are outside that scene snapshot | Reconcile V1.26 counter refresh and V1.27 deferred pickup feedback with #36's fixes; immediately pause on collection, check count, resume once, confirm one cue | Jehad + Tariq; source fixes in checkpoints, integrated native retest pending |
| C06 · P2 | Production controls differed from review hints | Use RMB/LMB hints and mouse orbit; test the Controls page and first capture without verbal explanation | Updated in #36; outside-usability retest pending |
| C07 · P2 | Representative usability, audible mix and build profiling evidence are incomplete | Run the protocol below, audit SFX ownership/volume and capture the same busy workload before/after one measured optimization | Jehad organizes; Tariq builds; Muhammad profiles; open #21/#22 |

These are documented observations or explicit verification gaps. Route confusion, frame-rate drops and power imbalance are test hypotheses until observed in the representative build.

### Next structured playtest

Issue [#21][issue21] requires **at least three first-time testers**. No completed three-person session is recorded in the inspected evidence. Jehad will organize a Windows-build session after the integration is accepted; all testers use the same commit and settings.

1. Record tester initials, date, build/commit, CPU/GPU/RAM, resolution and input device. Give only the title-screen instructions, then observe a full run without route coaching.
2. Record time to first capture, power replacements, completion time, deaths/locations, stuck points and help requests. Exercise blocked inhale, repeated hits, duplicate pickup contact, main retry, boss retry and a new run.
3. Ask for 1–5 ratings for movement/camera, combat readability and objective clarity, plus whether the current power and retry behavior were understood.
4. Log each reproducible problem with expected/actual behavior, P1–P3, owner and acceptance check. Fix completion blockers first, then the most common control/clarity issue; replay the failing route and attach the retest result.

Muhammad will also record separate Sword-only, Bow-only and Fire-only Guardian wins from full boss HP, showing both telegraphs and usable recovery windows. Existing developer checks establish integration progress but do not replace these usability/balance sessions.

## 6. Production update

### GitHub tracking snapshot — 24 November

The [issue list][issues] contains **23 tasks: 9 closed and 14 open**, all associated with **M2 - Systems Vertical Slice (Nov 19)**. The PR list contains **10 merged and 3 open** PRs. Issue closure is a workflow count, not an implementation-completion percentage.

The public [repository Projects view][projects] currently shows **no linked projects**. A private/unlinked board and its live column positions were not verified. The intended workflow is Backlog → Ready → In Progress → Review/Test → Done, as defined in [Contributing](../../CONTRIBUTING.md). The table below reconciles repository evidence for board updates; it does not claim that cards have already been moved.

| Workstream | Recorded GitHub progress | Handoff status / next acceptance |
|---|---|---|
| Core, combat, AI, graybox actors | Closed #1, #4–#9 and #19; merged #24–#32 | Completed subsystem implementation and recorded sandbox verification; retain integrated regression checks |
| Movement/camera | Closed #2; merged #34 | Completed implementation; movement/camera feel remains in usability pass |
| Inhale/power holder | Open #3; open #35 | Review/Test candidate; merge after teammate acceptance |
| World, rewards, fall recovery, flow and ending | Open #10–#16 despite implementation in Jehad checkpoints | Reconcile issue acceptance with integrated native results; do not equate code delivery with issue closure |
| HUD/audio | Open #17/#18; TMP support PR #33 open | Current Jehad HUD/audio supplied; finish clean-clone resource checks, controls/readability and mix validation before closure |
| Scene/build integration | Open #20; open #36 | Windows RC1 reported; review, merge, attach build and rerun acceptance from the merged revision |
| Test/profiling/submission evidence | Open #21–#23 | Three testers, native profiler comparison, board evidence, video and tagged build remain delivery gates; this file supplies the checkpoint portion of #23 |

Some Jehad checkpoints were delivered directly to `main`; those commits are implementation history rather than teammate PR-review evidence. The integration PR has no submitted review in the inspected snapshot. Teammate review, issue acceptance and board/milestone reconciliation remain explicit production tasks. This documentation update does not merge feature PRs, move cards or close umbrella issue #23.

### Carry-over sequence and Phase 3 readiness

- **Tariq + reviewer:** accept #35/#36 and resolve TMP resources where used; validate a fresh clone, one player/HUD/AudioListener/EventSystem, real input and both retry scopes. Keep new scene work on the integrated `Echoheart.unity` baseline.
- **Jehad + Tariq:** reconcile V1.26/V1.27 changes with the integration rather than replacing it with the review-capsule scene. Retest relic/chest persistence, pause timing, prompts and one-time completion.
- **Jehad + all:** execute #21's three-tester session, triage observations and document retests. Keep the two-area/three-power scope; trim route distractions if the measured run is too long.
- **Muhammad + Tariq:** complete #22 on the actual Development Build. Target **60 fps at 1920×1080** on declared hardware; record CPU/GPU frame times, GC allocation and bottlenecks, then compare one focused fix under identical conditions.
- **All:** review outstanding issue/board statuses, capture the board and gameplay evidence, and publish the matching milestone tag/Windows artifact. No published GitHub Release was present in the inspected release list.
- **Phase 3 entry:** systems and presentation foundations are available, with a developer-reported complete integration. Readiness is **conditional** on merging/retesting the candidate and closing the evidence gaps; finish these before expanding the visual pass. Carry results into the [final report](../phase3/final_report.md).

## 7. Competency application and next verification

| Competency | How it is applied in this slice | Phase 3 verification / improvement |
|---|---|---|
| Core gameplay systems engineering | Shared input, one-slot power holder, explicit game states, scoped retry and boss-gated ending | Repeat a real-input run, both deaths/retries and a second New Game without debug intervention |
| Physics and collision systems | CharacterController, layer/team masks, swept projectiles, hit IDs, capture/pickup/completion guards | Test thin/multi-collider targets, wall inhale, slopes, falls and pause/retry during interactions |
| AI behavior design | Explicit enemy and Guardian FSMs, data-driven variants, telegraph/recovery and spawner ownership | Record readable slam/ranged cycles, cancel/re-seat recovery and a fair win with every power |
| Real-time graphics pipeline | URP lit materials, directional lighting, differentiated world landmarks and violet corruption cues | Check imported materials, exposure, shadows and silhouette/route readability in the build; tune render/quality settings against the declared hardware |
| Technical art and polish | Cinemachine obstruction handling, actor hit/capture effects, boss warnings, chest reveal, relic glints and Heart restoration | Finish essential movement/attack/inhale/hurt/defeat animation; tune camera and restrained post-processing after native visual review |
| Audio systems design | Event-driven cues, regional music, crossfades/ducking, spatial water/world voices and shared volume controls | Audition near/far cues, prevent duplicated attack sounds, balance warnings against music and verify mute/pause/retry behavior |
| UI/UX feedback | Health/power/relic/boss HUD, prompts, objective bearing, input-matched hints and menu navigation | Use tester confusion and resolution checks to revise prompt hierarchy, contrast, aim visibility and focus flow |
| Performance optimization and debugging | Fixed query buffers, enemy cap, shared/batched foliage, tree LODs, bounded audio/VFX and reused counter strings; runtime UI/feedback fixes tracked above | Capture native CPU/GPU/GC evidence and one before/after bottleneck fix. The existing formatter microbenchmark measures formatting only, not Unity frame performance; see [Performance](../jehad/PERFORMANCE.md) |
| Production and collaboration practices | Owned tasks, shared contracts, sandbox/prefab boundaries, linked PRs, versioned checkpoints and evidence records | Complete teammate reviews, reconcile the Project board/issues, keep release/build/recording revisions aligned and document iteration outcomes |

### Evidence still required for submission acceptance

The report has traceable source/test/PR evidence. The remaining acceptance artifacts are the downloadable revision-matched Windows build, gameplay recording, actual HUD/world/boss/completion screenshots, three-tester observations/retests, native profiler before/after captures, visible Project-board snapshot and milestone release/tag verification. Owners and dependencies are tracked in [integration #20][issue20], [playtesting #21][issue21], [profiling #22][issue22], [submission #23][issue23] and [Muhammad's evidence checklist](muhammad_evidence_checklist.md).

[pr34]: https://github.com/Muhammad-Jaddoua/ECHO-HEART/pull/34
[pr35]: https://github.com/Muhammad-Jaddoua/ECHO-HEART/pull/35
[pr36]: https://github.com/Muhammad-Jaddoua/ECHO-HEART/pull/36
[integrated-scene]: https://github.com/Muhammad-Jaddoua/ECHO-HEART/blob/75cd32f5c35ceaa7aaff8b13167708bdf32b2b26/Assets/_Project/Scenes/Echoheart.unity
[main-commit]: https://github.com/Muhammad-Jaddoua/ECHO-HEART/commit/798ec6bbb2b9c0db4f3c3ab6ee4bb97fc852e43c
[issues]: https://github.com/Muhammad-Jaddoua/ECHO-HEART/issues
[projects]: https://github.com/Muhammad-Jaddoua/ECHO-HEART/projects
[issue20]: https://github.com/Muhammad-Jaddoua/ECHO-HEART/issues/20
[issue21]: https://github.com/Muhammad-Jaddoua/ECHO-HEART/issues/21
[issue22]: https://github.com/Muhammad-Jaddoua/ECHO-HEART/issues/22
[issue23]: https://github.com/Muhammad-Jaddoua/ECHO-HEART/issues/23
