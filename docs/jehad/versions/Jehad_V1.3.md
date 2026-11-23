# Jehad V1.3 — menus, checkpoints and restoration flow

Scene: `Assets/_Project/Scenes/Jehad/Jehad_V1.3.unity`  
Unity: **6000.3.27f1**, URP 17.3. Open this scene and press Play.

This checkpoint adds a complete session flow around the V1.2 valley. It reuses
the 240 × 240 m landscape, nature assets, water and 16 real relic pickups, plus
the existing Corrupted Guardian prefab. V1.0–V1.2 snapshots, the release scene,
build settings and teammate-owned player/combat/AI code are preserved.

The production player is not present on main at this checkpoint. The scene
therefore uses an explicitly named Testing capsule with movement and health,
without inhale or attacks. This is a flow integration checkpoint, not evidence
of a naturally playable start-to-boss-victory build. Test boss defeat through
the Editor-only harness until the production combat player is connected.

## What changed

- Scenic title screen, New Game and Quit; pause with Resume, Retry Area,
  audio sliders and Return to Title; death/retry and Heart-restored completion.
- Matching mint/cream/navy uGUI cards, keyboard focus, confirmation dialogs,
  unscaled menu fades, and a scene-local event-driven review HUD.
- Full health on retry, scope-specific ability retention, preserved relics,
  live-projectile cleanup, and inactive-object resets through existing contracts.
- Arena-only runtime NavMesh built from the existing arena colliders. Entering
  the interior activates the real Guardian, closes a physical gate and begins
  the encounter. Boss retry re-arms `BeginEncounter()` even inside the trigger.
- Boss defeat opens the gate and unlocks the Heart. A nearby E interaction
  restores it once, adds a brief light/scale change and shows run time/relics.
- Original synthesized UI/pickup/restoration cues and volume bindings. No new
  external assets or background music track are added. The Music slider controls
  bound sources; its source array is intentionally empty until music integration.

## Controls

| Control | Action |
|---|---|
| WASD / Left Shift / Space | Move / sprint / jump |
| Hold middle mouse / mouse wheel | Orbit / zoom the review camera |
| Esc | Pause/resume, or cancel the open confirmation |
| E near unlocked Heart | Restore the Island Heart |
| Mouse, Tab/arrows and Submit | Operate menus through the Input System |

The old R shortcut and duplicate V1.2 review overlay are removed only from this
new scene. If the player falls below Y = -18, it returns to the active checkpoint
with a one-HP fall penalty, preserving power and relics; lethal damage uses Death.
The review scene keeps a visible cursor for its middle-mouse camera and buttons.

## Reset behavior

| Action | Player | Enemies/boss | Relics | Heart / checkpoint |
|---|---|---|---|---|
| Main-area retry | Full HP, entry clearing, power cleared | Existing MainArea reset contract | Preserve | Locked / main |
| Boss-area retry | Full HP, inside arena, current power retained | Guardian full HP, encounter restarted; main spawners respect their own scope flag | Preserve | Locked / boss |
| New Game / Play Again | Full HP, entry clearing, power cleared | Existing NewGame reset contract | Reset all 16, including inactive pickups | Locked / main; timer zero |
| Pause / Resume | Input disabled/enabled; no reset | Simulation frozen/resumed | Preserve | Preserve |
| Return to Title | Gameplay frozen; scenic camera | Boss disabled, gate opened | Next New Game resets | No Continue/save feature |

The timer counts scaled Playing time across retries. Title, Pause, Death and
Complete freeze gameplay (`Time.timeScale = 0`) and disable the assigned player
input components. Menus and Heart restoration use unscaled time. Standalone
focus loss pauses; the Editor remains usable for Inspector context-menu checks.
Normal teardown restores time scale and the previous cursor settings.

## Event and teammate integration

`SessionPolicyV13` is plain C# for guarded state transitions. `SessionFlowV13`
owns scene state, resets and completion. `RetryServiceV13` resets all scene
`IResettable`s including inactive ones, while each EnemySpawner owns its enemy
instance. `RelicCounterV13` reads unchanged `HiddenRelicV12` components and raises
`CollectiblesChanged` only on change or an explicit reset refresh.

No Core APIs were added. The existing Guardian owns its boss events and has no
extra HealthEventRelay. The player has exactly one Player-mode relay. Menus read
existing GameEvents rather than polling combat internals. The new usage is also
recorded in `docs/reference/contract-changes.md`.

When Tariq's player is merged:

1. Replace the Testing capsule and assign the production Transform, Health and
   gameplay input behaviours to `PlayerFlowBindingV13`. Keep UI input separate.
2. Connect `_clearAbilityRequested` to the holder's clear method and the dynamic
   `_equipAbilityRequested(AbilityDefinition)` to its equip method. The holder
   must raise the existing AbilityChanged event. Turn off
   `_publishReviewAbilityChanges` so events have one real owner.
3. Connect `_onRespawned` to reset motor velocity and snap the production camera.
   The review scene currently binds `JehadFlowExplorerV13.ResetMotion`.
4. Assign the production camera to flow, opt into `_lockCursorWhilePlaying` if
   needed, and retain one Player-mode HealthEventRelay. Never add one to boss.
5. Turn off `FlowMenuV13._showReviewHud` when the production HUD is installed.
   Retain the menu canvas and one InputSystemUIInputModule/EventSystem.
6. Add the three enemy spawner types and ability interactions on the main-world
   NavMesh in a later checkpoint, then playtest victory with Sword, Bow and Fire.
   This scene builds navigation only for the Guardian arena.

## Review in Unity

First check the Console for compilation/import errors, then test menus at
1920×1080 and another aspect ratio. Confirm mouse and keyboard focus, Esc,
confirmation cancel, SFX volume and Quit in a standalone development build.
There is no progress persistence or music track to test yet.

For a repeatable flow smoke test in Play mode, select the scene root named
`Jehad flow acceptance harness` and use its `FlowReviewHarnessV13`
component context menu **Flow review → Run acceptance checks (resets run)**.
This starts a new run and discards current progress. It checks a real pickup,
real player death and Health reset, main/boss power policies, inactive relic
reset, runtime navigation, real arena entry/BossBrain restart, real boss Health
defeat, guarded Heart interaction, completion UI and Play Again. It returns to
Title when finished and reports success/failure in the Console.

Individual context actions can defeat the player, move into the arena, damage
the Guardian to death, or move near the Heart. They use actual Health/trigger
paths, not fabricated BossDefeated events. The harness is on an `EditorOnly`
root with no gameplay children and its actions compile only for the Editor or
development builds; that root is stripped from normal scene builds. These
actions are test aids and do not establish production combat balance.

Then walk the full arena, inspect the gate/respawn clearance, pause during
attacks, retry repeatedly, collect more than one relic and complete manually.
Run the repository's existing combat/AI self-tests after player integration.
Profile the enlarged landscape and review lighting/camera before final release.

## Verification in this environment

**Passed:** the actual `SessionPolicyV13` and shared `CoreTypes.cs` compile under
C# 9.0 in a .NET 8 test executable with warnings as errors: **445 assertions
across six scenarios**, including exhaustive reachable-state invariant checks.
Static checks parse all 11 new C# files and 99 Unity YAML documents, validate
GUID/local/prefab references, relay ownership, trigger/respawn/gate wiring and
the preservation of 417 pre-existing local files.

**Not run:** Unity Editor compilation/import, runtime uGUI/Input System, physics
triggers, arena NavMesh build/agent movement, audio playback, lighting, camera
clearance, combat victory or performance. The supplied Unity acceptance harness
has not been executed here. No Unity Editor is available in this environment.
The policy test and syntax/reference checks do not substitute for those checks.

Reproduction: `Tools/Jehad/V1_3/README.md`. Machine-readable evidence:
`Jehad_V1.3.validation.json` beside this note. Existing art licenses/credits are
unchanged; no third-party content was introduced by V1.3.

Next after this checkpoint is reviewed: run Unity acceptance, connect the
production player and the three enemy types, then test each ability against
the boss and profile the full world before final-submission polish.
