# Milestone 2 Plan: Systems Vertical Slice (due Thu 19 Nov 2026)

**Status (13 Nov, evening):** Day 0 skeleton done: Unity 6.3 URP project, packages, layers, folder layout,
`Scripts/Core` contracts, shared `Health`, sandbox stand-ins, sandbox scenes + empty `Echoheart.unity`. 6 working days left.
**Definition of done:** a Windows build where a first-time tester can New Game → explore → copy all three powers →
beat the Guardian → activate the Heart → see the completion screen, and can die/retry in both areas. Graybox
level, key models for spirit/enemies/boss, first-pass HUD + audio. Plus `docs/phase2/checkpoint.md` with evidence.

Rubric weights to optimize for: **flow 30 · integration 25 · playtesting 20 · workflow 15 · writing 10.**
→ Get the end-to-end loop working by **Tue 17**, leave Wed for playtests/fixes, Thu for docs/release.

> Open questions to confirm: exact submission time on 19 Nov and channel (Blackboard? GitHub tag only?); who will be our 3 playtesters.

## Recommended team split

Matches your initial idea (Jehad = world; Tariq + Muhammad = player, enemies, mechanics). The cut lines follow the
code contracts in `architecture.md`, so nobody waits on anybody after Day 0.

| | **Tariq: Player & Camera** | **Muhammad: Combat & AI** | **Jehad: World & Game Flow** |
|---|---|---|---|
| Systems | PlayerMotor (CharacterController, camera-relative, jump, gravity), Cinemachine camera + deoccluder, input actions, **Inhale** (range + LoS + pull + capture), **PlayerAbilityHolder** (equip/replace, accessory), aim guide/crosshair, player hurt/death hooks, spirit model + procedural anim | **Health/Damage** (shared), **Sword** hitbox, **shared Projectile** (Bow/Fire), **Enemy FSM** + 3 variants (Blade Beetle, Thorn Archer, Emberling), replenishing **spawners**, **Boss FSM** (telegraph → slam/ranged → recover), enemy + boss models/anims | **Graybox** main area + arena (ProBuilder, prefabs), NavMesh bake, **collectibles** (ids, once/session), **2 secrets**, **fall zones** + safe ground, arena entrance retry point, **Island Heart** + completion, **GameManager/RetryService** (Title/Play/Pause/Dead/Complete), **AudioManager** + music/SFX first pass |
| UI | HUD: health, power icon, crosshair, prompts | Boss health bar | Menus: Title, Pause (resume/retry/volume), Death/Retry, Completion |
| Owns | `Scripts/Player`, `Prefabs/Player`, `Input/` | `Scripts/Combat`, `Scripts/AI`, `Prefabs/Enemies|Boss|Projectiles`, `Data/` | `Scripts/World|Audio`, `Prefabs/World`, `Audio/`, area prefabs |
| Also | **Integrator**: only one who commits `Echoheart.unity`, builds the Windows exe | **Day 0 contracts** (`Scripts/Core`) + project skeleton | Playtest organizer + checkpoint doc lead |
| Sandbox stub | Uses `DummyInhalable` + `TrainingDummy` (Health) cubes from Core | Uses a capsule "TestPlayer" with Health + a key to fire each ability | Uses a basic capsule controller until Tariq's player prefab lands (Mon) |

Why this split: Muhammad's combat/AI is the heaviest, so UI menus + game flow move to Jehad (flow is tied to his retry
points, fall zones, collectibles and Heart anyway). Tariq's player work is front-loaded, so he takes integration and the build.
Shared: playtesting, docs, PR reviews (everyone reviews at least one PR per day).

## Kickoff meeting (Sat 14, ~30 min)

1. Everyone: install **Unity 6000.3.27f1** + **Git LFS**, clone, open, check the console has **0 errors**, press Play in your sandbox.
2. Confirm or adjust the split above. Assign issues on the board; set the milestone.
3. Walk through `docs/reference/architecture.md` → *Code contracts*. These are the agreements; changes are announced in the group chat.
4. Agree on a daily 10-min sync time and the review rule (each PR reviewed within the same day).
5. Confirm the submission time/channel for 19 Nov and line up 3 playtesters for Wed 18.

## First steps per person (after kickoff)

**Tariq:** branch `feature/2-player-movement`. In `Sandbox_Tariq`, replace the `TestPlayer` capsule with a real `Player`
prefab (CharacterController + `PlayerMotor` + Cinemachine camera). Adapt `EchoheartControls.inputactions` to our controls.
Then `feature/3-inhale`: hold right click against the `DummyInhalable` cubes (they already grant Sword/Bow/Fire).

**Muhammad:** branch `feature/4-health-attacks`. In `Sandbox_Muhammad`, hit the `TrainingDummy` with a Sword hit box,
then the shared `Projectile` for Bow/Fire. Then `feature/7-enemy-fsm` with an `EnemyDefinition` ScriptableObject.

**Jehad:** branch `feature/10-main-area-graybox`. In `Sandbox_Jehad`, block out the main area with ProBuilder (walk it
with the `TestPlayer` capsule; keep paths ≥ 3 m wide for the camera), save it as `Prefabs/World/MainArea.prefab`.
Then collectibles + GameManager skeleton.

## Using Claude Code on this repo
Anyone can open this folder in Claude Code. It reads `CLAUDE.md` automatically and knows the plan, contracts, and rules.
Have Unity open on this project so the Unity MCP connects to the right Editor. Ask it to work on *your* issue on
*your* branch, and review its diff like any teammate's PR.

## Day-by-day

| Day | Tariq | Muhammad | Jehad | Exit check (end of day) |
|---|---|---|---|---|
| **Fri 13 (tonight)** | n/a | ✅ **Day 0 skeleton**: Unity URP project, packages, settings, folders, layers, `Scripts/Core` contracts + stubs, sandbox scenes. Still to do: board + issues | n/a | Everyone can clone, open, and press Play in their sandbox |
| **Sat 14** | Team meeting (30 min): confirm split, versions, board. Motor + jump + camera | Health/Damage, TrainingDummy, Sword hitbox | Graybox main area blockout (route, entry corner, side paths) | Each sandbox runs own feature |
| **Sun 15** | Inhale + AbilityHolder (vs DummyInhalable) | Shared Projectile → Bow & Fire; EnemyBrain FSM (Idle/Chase/Attack) | Arena blockout, NavMesh, collectibles + counter, fall zones | P01 passes; abilities damage dummies |
| **Mon 16** | **Integration #1**: player + enemies + level in `Echoheart.unity`; HUD | 3 enemy variants as `IInhalable`; spawners/replenish; Boss FSM v1 | GameManager + RetryService + menus; arena entrance retry point; Heart | Move, capture, attack, take damage, retry in one build |
| **Tue 17** | Aim guide, hurt feedback, key models (spirit) | Boss slam + ranged with telegraphs, boss bar, tuning so each power can win | 2 secrets, completion screen, music loop + core SFX, power sources at arena | **Feature complete**: start → boss → Heart → complete |
| **Wed 18** | Windows build v0.2-rc; fix P1 bugs | Fix P1 bugs; record 3 boss wins (Sword/Bow/Fire) | Run playtest (≥3 testers, script below); triage into issues | Playtest done, P1s fixed, profiler capture saved |
| **Thu 19** | Final build + tag `v0.2-milestone2` + GitHub Release with zip | Systems status + profiler sections of checkpoint | Checkpoint doc assembly, board snapshot, gameplay video | **Submitted** |

Buffer: if Tue slips, cut in this order: secrets → second collectible cluster → enemy replenish at main entry → boss
ranged attack polish. **Never cut:** start/end flow, death/retry, all three powers, boss win.

## GitHub board & issues (create on Day 0)

Board columns: Backlog → Ready → In Progress → Review/Test → Done. Milestone: **M2 – Systems Vertical Slice (Nov 19)**.
Labels: `player`, `combat`, `ai`, `world`, `ui`, `audio`, `flow`, `docs`, `bug`, `P1/P2/P3`.

| # | Title | Owner | Depends on | Acceptance |
|---|---|---|---|---|
| 1 | Unity project skeleton + Core contracts | Muhammad | n/a | Clean clone opens with 0 errors; sandboxes play |
| 2 | Player movement + jump + camera (P01) | Tariq | 1 | Walk/jump/land/turn by wall without clipping or losing view |
| 3 | Inhale + ability holder (P02) | Tariq | 1 | Captures valid target once; rejects blocked/out of range; replaces power |
| 4 | Health/damage + hit-once attack ids | Muhammad | 1 | Same swing/projectile never damages a target twice |
| 5 | Sword attack | Muhammad | 4 | Hit window visible (gizmo), damages enemies/boss |
| 6 | Shared projectile: Bow + Fire | Muhammad | 4 | Sweep hit detection, no tunneling, cooldown, distinct values |
| 7 | Enemy FSM + 3 variants + IInhalable | Muhammad | 4 | Idle/Chase/Attack; capture and defeat can't both happen |
| 8 | Enemy spawners (replenish) | Muhammad | 7 | Entry + arena sources respawn after capture/defeat |
| 9 | Boss FSM: slam + ranged, telegraphs, bar | Muhammad | 5,6 | Both telegraph; recovery windows; can't be inhaled |
| 10 | Main area graybox + NavMesh | Jehad | 1 | Clear route; side paths reconnect; enemies path |
| 11 | Boss arena graybox + entrance retry point | Jehad | 1 | Retry restores player/boss HP + selected power |
| 12 | Collectibles (8–12) + counter | Jehad | 1 | Touch twice = counted once; reset only on New Game |
| 13 | Two secrets | Jehad | 10 | Both reachable, optional |
| 14 | Fall zones + safe-ground respawn | Jehad | 10 | Falling damages + returns to safe ground |
| 15 | GameManager, RetryService, menus | Jehad | 1 | Title/Pause/Death/Complete flow; New Game resets session |
| 16 | Island Heart + completion | Jehad | 9,15 | Only after boss defeat; completes once |
| 17 | HUD (health, power, collectibles, prompts, crosshair) | Tariq | 1 | Updates from GameEvents only |
| 18 | Audio first pass (mixer, music, 6 core SFX, 3D enemy audio) | Jehad | 1 | Music/SFX sliders work |
| 19 | Key models: spirit, 3 enemies, boss | Tariq / Muhammad | 2,7 | No pink materials; credited |
| 20 | Integration into Echoheart.unity + Windows build | Tariq | all | Clean build runs start→end |
| 21 | Playtest (≥3 testers) + triage | Jehad (all) | 20 | Findings logged as issues with P1–P3 |
| 22 | Profiler capture + one measured improvement | Muhammad | 20 | Screenshot + numbers in checkpoint |
| 23 | `docs/phase2/checkpoint.md` + video + board snapshot + tag/release | All | 21,22 | Matches Phase 2 required sections |

## Playtest script (Wed 18)
Setup: Windows build, team PC (note CPU/GPU/RAM, resolution), no instructions beyond the title screen. Tester plays
once fully; observer notes time-to-first-capture, deaths and where, confusion points, bugs. Then ask: did you know your
current power? Where to go? How to retry? Ratings 1–5 for controls/camera/combat clarity. Log every finding as an issue
(severity P1 blocks completion / P2 hurts clarity / P3 polish) and reference issue numbers in the report.

## Checkpoint doc evidence checklist
- [ ] Screenshots: overview of both areas, each power in use, HUD, boss telegraph, completion screen
- [ ] 2–4 min gameplay recording (Unity Recorder or OBS) + link
- [ ] Board snapshot (screenshot) + list of closed issues/PRs
- [ ] Profiler capture (busy encounter + boss) with fps/ms and one bottleneck note
- [ ] Known issues + carry-over to Phase 3 + Phase 3 readiness bullets
- [ ] Tag `v0.2-milestone2` + GitHub Release with zipped Windows build
