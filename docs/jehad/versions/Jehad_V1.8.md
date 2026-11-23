# Jehad V1.8 — title layout and reliable menu focus

Open **`Assets/_Project/Scenes/Jehad/Jehad_V1.8.unity`** in Unity 6000.3.27f1.

## What changed

- **Title layout:** Quit moved beside Controls and Credits in a compact row
  below Settings. Its previous position intersected the right-hand footer
  caption. A source-geometry regression check reproduces that intersection
  in V1.7 and finds none in V1.8's direct title button/text rectangles.
- **Background clicks:** the scene-created Input System UI module keeps its
  selection when clicking empty space. An existing external module retains
  its configuration. If another component clears selection, directional
  keyboard/D-pad/stick input restores focus within the active menu.
- **Back returns to the origin:** visiting a subpage remembers the button
  that opened it. Returning from Settings, Controls or Credits selects that
  button rather than always jumping to Begin Adventure or Resume. Each page
  has its own history for the current visit; re-entering a subpage starts on
  its intended default, and entering a different game state clears all history.
- **Scoped recovery:** inactive, disabled, destroyed or out-of-scope controls
  are rejected. Confirmation dialogs recover to Cancel, and modal selections
  never overwrite page history. Gameplay does not recover menu selection or
  give the HUD keyboard focus.
- **Settings dialogs:** cancelling still returns to the originating control.
  Accepting Restore Defaults also returns to its invoking control when usable.

The new scene replaces only the menu presenter, typed flow-review harness and
UI-review harness. It reuses the V1.7 factory, HUD, settings, navigation, display
transaction and toast skin, plus V1.6 graphics/values/timer. World/audio/chests,
population, session flow and all earlier snapshots are preserved. There are
no Core, player, combat, AI, release-scene, package or build-settings changes.
The existing session-only settings and 15-second display rollback are retained.

## Unity review

1. Inspect the title at 1280×720, 1920×1080, 21:9 and 4:3. Check that Controls,
   Credits and Quit captions fit their smaller buttons and the right footer
   is unobstructed. Rectangle checks do not establish actual font fit.
2. Use mouse, keyboard and a controller to open Settings/Controls/Credits and
   return with Back/Esc/controller East. Confirm focus returns to the button
   that opened the page. Repeat from Pause without resuming the game.
3. Click empty areas, then navigate with arrows, D-pad or stick. In a development
   test, clear EventSystem selection and verify navigation recovers. Test with
   both the scene-created module and an existing module. Recovery occurs on
   directional navigation; an isolated Submit press is not used to recover.
4. Open Quit/Retry/Defaults/Discard/Display dialogs. Confirm focus stays inside
   the modal, defaults to Cancel/Revert, and returns to a usable origin after
   cancellation. Restore Defaults should return to its button. Begin a new
   game, pause, die or complete a run and check that stale focus is not reused.
5. Run **Jehad V1.8 UI acceptance harness → UI review → Run navigation and
   settings checks (resets run)** in Play mode. New checks cover the title's
   Settings origin, cleared-selection recovery and modal Cancel recovery.
   It retains prior bounds, audio, draft/discard, navigation, input-shield and
   retry checks. Then run the V1.8 flow and prior world/population harnesses
   separately. Test native display switching in a standalone development build.

The review player still provides movement only. Teammate-owned production
player/ability integration remains outstanding; direct harness health/boss
actions do not prove production combat works.

## Verification

**Passed:** 1,586 assertions compile actual UiFocusHistoryV18 and the reused
V1.7 router/navigation/display transaction plus V1.6 preferences/timer against
actual CoreTypes (C# 9.0/.NET 8, warnings as errors). Sixteen new assertions
exercise page-origin journeys, empty clicks, repeated state rendering, state
changes, rejected stale controls and reference identity. The remaining 1,570
are the existing logic regression suite.

Static checks cover six new C# syntax trees, 115 scene YAML documents, serialized
references, focus/input guards and reference-canvas geometry. Only three UI
script bindings and version labels differ from the V1.7 scene. The builder is
idempotent. A 690-file staged baseline is preserved except for the new contract
log row; publishing also verifies every prior remote blob outside that log.

**Not run:** Unity compilation/import, actual rendering/font layout, hardware
input, display switching, harness execution and profiling. No Unity Editor is
connected. Source geometry and pure-logic tests are not screenshots or runtime
evidence.

Evidence: `Jehad_V1.8.validation.json`. Commands: `Tools/Jehad/V1_8/README.md`.
Next: use Unity/standalone findings to tune layout, focus and world feedback.
