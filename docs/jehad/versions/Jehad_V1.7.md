# Jehad V1.7 — menu navigation and display settings fixes

Open **`Assets/_Project/Scenes/Jehad/Jehad_V1.7.unity`** in Unity 6000.3.27f1.
This checkpoint follows the V1.6 interface pass. It fixes navigation and staged
display changes while retaining its forest/cream/mint/gold presentation, HUD,
audio, world, population and session flow. Previous scenes are preserved.

## Changes to review

- **Spatial menu navigation:** arrows and controller navigation now use the
  controls' actual screen positions. Left/right reaches side-by-side Controls
  and Credits buttons and confirmation actions. Horizontal wrapping stays in
  the same row; each modal has its own focus scope. Sliders retain left/right
  for adjusting their values and up/down for leaving the slider.
- **Settings navigation:** Tab order visits the Audio/Display/Interface tabs,
  the active tab's controls, then the footer. The active tab has a gold marker
  and a heading. Controls and Credits focus Back on entry; Cancel returns to
  the previously selected interactable control.
- **Visible unapplied state:** display edits show a gold status message.
  Apply Display is disabled until a value differs from the applied settings.
  Returning every option to its applied value clears the pending state.
- **Discard confirmation:** leaving Settings with staged display changes asks
  whether to keep editing or discard those edits and return to the home menu.
  Cancel preserves the draft. Audio and interface options still take effect
  immediately; discarding display edits does not reset them.
- **Native display preservation:** editing a frame cap or quality tier keeps
  the exact existing fullscreen mode and VSync count. The mode control still
  toggles between windowed and borderless, while untouched exclusive/maximized
  modes remain intact. Apply skips unchanged native setters. Merely opening or
  leaving Settings does not reload the quality tier.
- **Preview lifecycle:** rollback captures the actual native values at Apply
  time. Keep reads the platform's actual result. It becomes available after
  two frames, allowing a deferred resolution change to settle and avoiding an
  immediate repeat submit. Revert, cancel, focus loss or the existing
  15-second unscaled timeout resolves the transaction once. Unchanged Apply
  never starts a preview. Session graphics restore on teardown only if a
  graphics preview was applied.
- Confirmation action labels now name their action: Retry Area, Return to
  Title, Quit Game, Restore Defaults or Discard Changes.

Settings remain **session-only**. There is no save/Continue system. No new
assets, fonts, packages or external dependencies were imported.

## Unity review steps

1. Inspect title, pause, death, completion and all subpages at 16:9, 21:9 and
   4:3. Check actual text wrapping, tab indicators, focus outlines and HUD.
2. Navigate Title Controls/Credits and every confirmation with left/right,
   vertical arrows, Tab/Shift+Tab and a controller. Confirm a dialog starts on
   Cancel, blocks the page behind it, and Cancel restores focus. Check that
   Credits starts on Back rather than its external Asset Credits link.
3. Open Settings from Title and Pause. Change a display option, visit another
   settings tab, then press Back/Esc/controller East. Choose Keep Editing and
   confirm the draft remains. Repeat and Discard Changes. Verify immediate
   audio/interface changes remain, and returning home never resumes Pause.
4. In a standalone development build, test Apply/Keep, Revert, Esc, timeout and
   focus loss. Verify exact original display mode, resolution, quality, VSync
   and frame cap restore. Change only quality or frame cap while starting from
   exclusive fullscreen or a VSync count above one. Confirm unchanged Apply is
   disabled and the countdown runs while `Time.timeScale` is zero. A two-frame
   settling delay needs verification on the target platform; it is not a
   platform completion callback.
5. In Play mode, select **Jehad V1.7 UI acceptance harness**, then run **UI
   review → Run navigation and settings checks (resets run)**. It checks
   subpage focus/bounds, unchanged Apply, dirty/discard behavior, modal arrows,
   Cancel focus, real volume plumbing, input shielding and retry. It does not
   switch native display settings. Run the V1.7 flow harness and the existing
   population/world polish harnesses separately afterward.

The scene still uses the movement-only review player. Production player and
ability integration requires the teammate-owned player work. Do not treat the
review harnesses' direct health/boss actions as production combat acceptance.

## Verification

**Passed:** 1,570 assertions compile the actual V1.7 router, spatial navigation
and display transaction plus the reused V1.6 preference sanitizer/timer and
actual CoreTypes, using C# 9.0/.NET 8 with warnings as errors. Cases include
page/modal/state combinations, horizontal and grid navigation, slider arrows,
canvas scale/translation, dirty comparisons for every native display field,
exact mode/VSync preservation, stale callbacks, rollback/keep/timeout and
external native-state changes before Apply.

Static validation passes for 12 new C# syntax trees, 115 scene YAML documents,
GUID/component/serialized-field references, focus/input guards and reference
canvas geometry at five resolutions. Normalizing the five intended UI script
bindings and version labels reproduces the previous scene exactly. Baseline
checks preserve all 654 staged prior files except the appended contract-log
entry. The publish check also compares the full remote Git tree.

**Not run:** Unity compilation/import, actual rendering, font fit, pointer/
keyboard/controller operation, standalone display behavior, harness execution
or performance. No Unity Editor is available here. Geometry and pure C# tests
are not engine screenshots or playtest evidence.

Evidence: `Jehad_V1.7.validation.json`. Reproduction commands:
`Tools/Jehad/V1_7/README.md`. Next checkpoint should address findings from
running this scene in Unity and a standalone development build.
