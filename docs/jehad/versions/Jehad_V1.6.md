# Jehad V1.6 — complete interface pass

Open **`Assets/_Project/Scenes/Jehad/Jehad_V1.6.unity`** in Unity 6000.3.27f1.
The new scene replaces its menu presenter and adds settings/toast styling; it
keeps the V1.5 world/audio/chests, V1.4 population and V1.3 session flow. Earlier
snapshots, the release scene, build settings, shared Core and teammate-owned
player/combat/AI implementations remain unchanged.

## Visual and menu changes

- A scenic title screen with an original heart emblem, Sword/Bow/Fire glyphs,
  clear Begin Adventure action, Settings, Controls, Credits and confirmed Quit.
- One forest-green/cream/mint/gold visual language across rounded panels,
  controls, focus outlines, health/ability/relic HUD, boss bar, prompts and
  discovery notifications. UI geometry/icons are original vector graphics;
  no new images, fonts, packages or third-party assets were imported.
- Dedicated pause, retry/death and completion screens. Pause/completion show
  actual relic totals and exploration time; retry copy explains the current
  checkpoint's health/power/relic policy.
- A readable controls page for the current review controller and in-game
  credits for the team, existing nature packs and project-original sound/UI.
  Asset Credits opens the repository's full credits page when selected.
- Confirmation dialogs isolate focus and input. Cancel is selected first;
  stale/repeated callbacks cannot apply the same action twice.

## Settings are connected

| Tab | Options | Behavior |
|---|---|---|
| Audio | Music, Sound Effects, Preview Sound | Updates the existing volume provider used by V1.5 music, ambience, SFX and UI clicks immediately |
| Display | Resolution, windowed/borderless fullscreen, actual project quality tiers, VSync, 30/60/120/unlimited frame cap | Values are staged, then Apply Display previews them; frame-cap control is disabled when VSync controls pacing |
| Interface | HUD scale 85–115%, high contrast, reduced UI motion, objective/control hints, ranged aim guide | Applies immediately; HUD clusters reflow to keep spacing at larger sizes |

Display changes require **Keep Settings** within **15 seconds of unscaled
time**. Revert, Esc, timeout or focus loss restores the previous display,
quality, VSync and frame cap. Settings remain usable while the game is paused.
Restoring defaults changes audio/interface immediately and stages the initial
display settings until Apply is selected.

These preferences last for the scene/session. No PlayerPrefs, progress save or
new save system is added. Leaving the scene restores the runtime graphics
settings captured at startup. Display mode/resolution/frame pacing must be
verified in a standalone build: the Editor Game view is not equivalent to a
real player window, and a frame cap is a target, not a performance guarantee.

## HUD and interaction polish

The HUD now has a compact vitality bar + number, a power glyph/name, relic
count/progress, a contextual objective, Guardian health + number and a clear
Pause button. Bow/Fire can show a restrained center aim guide when enabled.
HUD scale affects anchored clusters independently, preserving screen margins.
The health/boss bars interpolate with unscaled time; reduced motion makes them
immediate and removes menu fades/relic color transitions.

Chest/Heart prompts have a keycap and readable backing. Existing discovery
toasts receive the same panel/icon/type treatment and move above the prompt
near the bottom center, avoiding the objective and boss bar. Their existing
discovery timing/award owner remains WorldToastV15/WorldFeedbackV15.

All HUD values still come from the existing GameEvents. The new presenter does
not deal damage, award powers/relics or decide gameplay progression. The old
FlowMenuV13 is absent from this scene, so it cannot create a duplicate canvas
or consume Esc alongside the new UI. The flow harness's serialized UI type is
updated to FlowReviewHarnessV16; the original V1.3 harness file is preserved.

## Input and responsive layout

- Mouse hover, keyboard selection and controller navigation have a visible
  outline, not just a color change. Tab/Shift+Tab cycle active controls;
  arrows/vertical stick navigate, Enter/Space or controller South select, and
  Esc/controller East go back. Controller Start pauses/resumes gameplay.
- Esc closes a confirmation before a subpage, and a subpage before resuming.
  A frame guard prevents Input System Cancel plus keyboard Esc from handling
  the same press twice. Opening settings never resumes a paused/dead game.
- The confirmation's underlying page is non-interactable and removed from
  pointer handling; navigation wraps only inside the active focus scope.
- Returning to play clears UI selection and blocks gameplay controls for one
  frame, shielding movement from the submit/click that closed the menu.
  The gameplay HUD Pause button is mouse-only and does not take keyboard focus
  on hover, so Space remains available to jump.
- The 1600×900 reference canvas uses Expand scaling and a Screen.safeArea root.
  Static reference geometry fits 1280×720, 1920×1080, 2560×1080, 1024×768 and
  3840×2160. Actual font rendering, focus and runtime safe areas need inspection.

## Unity review

1. Open V1.6, check the Console, then inspect the title, each subpage and button
   focus at 16:9, 21:9 and 4:3. Check text truncation, rounded/icon geometry,
   clickable slider tracks and Tab/Shift+Tab/arrow/controller navigation.
2. Test Audio from both Title and Pause, including zero volume and Preview
   Sound. Test every Interface option and resume to inspect HUD scale/hints/
   crosshair. Check the larger HUD and discovery toasts together.
3. In a standalone development build, change display mode/resolution/quality,
   Apply then Keep; repeat with Revert, Esc, timeout and focus loss. Confirm
   the 15-second countdown progresses while Time.timeScale is zero. Check
   VSync/frame-cap behavior on the target monitor and restore settings on exit.
4. Select **Jehad V1.6 UI acceptance harness**, then use **UI review → Run
   navigation and settings checks (resets run)** in Play mode. It resets the run,
   checks subpage origin/focus/button bounds, real audio volume plumbing,
   interface options, the one-frame input shield, modal isolation, cancel,
   confirmed retry and duplicate callbacks. It restores original UI preferences
   and returns to Title. It does not automatically change the display.
5. Run **Jehad V1.6 flow acceptance harness** for the existing health/retry/
   Guardian/Heart/completion path. Also run the V1.4 population and V1.5 world
   polish harnesses, one at a time. Manually inspect Death and Completion UI.

The harnesses are on EditorOnly roots with no gameplay children, and their
context actions compile only for Editor/development builds. Do not treat their
direct enemy/health actions as production combat acceptance. The movement-only
review player remains; Controls documents its actual keys, while production
player/ability integration remains a separate task.

## Verification in this environment

**Passed:** 873 assertions compile the actual UI routing, preference sanitizer
and display timer with actual CoreTypes under C# 9.0/.NET 8. These cover all
game-state/page/modal combinations, stale/repeated confirmation callbacks,
back priority, invalid preference inputs and display keep/timeout behavior.

Static checks pass for 13 new C# syntax trees and 115 Unity scene YAML documents,
GUID/component/serialized field references, the single presenter and health
relay, the updated flow harness type, preserved baseline files, reference-canvas
card fit and scaled-HUD cluster spacing. The layout math is not a screenshot
or an engine rendering test.

**Not run:** Unity C# compilation/import, actual font/glyph/layout appearance,
pointer/keyboard/gamepad operation, display switching, settings teardown,
the supplied harnesses, UI/world regression playtests or performance. There
is no Unity Editor available here. Verify those before final submission.

Rebuild/test commands: `Tools/Jehad/V1_6/README.md`. Evidence:
`Jehad_V1.6.validation.json`. Next after review: correct Unity UI findings and
tune readability, transitions and target-PC frame time from actual playtesting.
