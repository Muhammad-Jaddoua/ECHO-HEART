# Jehad V1.9 — responsive menus, HUD and discovery messages

Open **`Assets/_Project/Scenes/Jehad/Jehad_V1.9.unity`** in Unity 6000.3.27f1.

## Changes

- Menu cards and confirmation cards now fit inside the available safe area
  with a gutter. They keep their designed size when space permits and shrink
  uniformly when large insets leave less room. Screen dimensions alone no
  longer determine whether a fixed-size card fits.
- The HUD uses a centered logical surface at least 1600×900 canvas units.
  When the safe area is smaller, the whole surface fits uniformly within it;
  health, powers, relics, objectives, Guardian health and interaction hints
  retain their relative spacing. The user's 85–115% HUD preference still
  controls the individual clusters within that surface.
- Discovery messages use the same clipped safe area and logical-surface
  calculation as the HUD. Their text and backdrop scale together. Their
  position keeps a 22-unit gap above the interaction prompt at every supported
  HUD scale, before the common surface scale is applied.
- Layout refreshes after screen dimensions, safe area or actual canvas
  dimensions change. Keyboard/controller navigation is recalculated after
  menu fitting; the V1.8 focus/history behavior is retained.
- Invalid or empty safe-area data falls back to the full screen. Partially
  out-of-screen rectangles are clipped to the screen bounds.

At normal full-screen reference layouts, menu/HUD sizing stays at its designed
size. Discovery placement now follows the prompt gap rather than a fixed Y
offset. Very small windows still require visual review: fitting prevents
geometric clipping but cannot guarantee comfortable text size.

## Integration scope

Only four scene script bindings change: FlowMenuV19, FlowReviewHarnessV19,
UiReviewHarnessV19 and WorldToastSkinV19. The UI harness also receives its toast
skin reference. No new scene roots or gameplay objects are introduced.

The existing WorldToastV15 remains the discovery text/timer owner, and
WorldFeedbackV15 retains its existing observation and feedback paths. The new
skin reparents the existing label, retaining that reference. No second
discovery/event publisher is added. The V1.7 settings/factory/navigation and
V1.8 focus history are reused. Previous scenes, release scene, build settings,
Core and teammate-owned player/combat/AI implementations remain unchanged.

The movement-only review player remains; production player/ability integration
and end-to-end combat acceptance are outstanding.

## Unity review

1. Inspect Title, Settings, Controls, Credits, Pause, Death, Completion and
   confirmations at 1280×720, 1920×1080, ultrawide and 4:3. Resize the Game view
   and inspect font fit, focus outlines, slider dragging and menu navigation.
2. Inspect a target with safe-area insets, or an Editor/device simulator that
   supplies them. Confirm the complete menu card and its outline fit. Check
   all four edges, including asymmetric insets. The layout model also covers
   portrait and extreme narrow cases; these are not verified target platforms.
3. Start a run, set HUD scale to 85%, 100% and 115%, and trigger a relic/chest/
   region discovery near an interaction prompt. Verify the text/backdrop stay
   together above the prompt and the health/relic/Guardian clusters stay apart.
   Check high contrast and reduced motion, pause/resume and area retries.
4. In Play mode run **Jehad V1.9 UI acceptance harness → UI review → Run
   navigation and settings checks (resets run)**. It refreshes layout before
   checking active button and discovery-backdrop corners against the actual
   safe area. It retains the earlier focus, audio, draft/discard and input
   checks. Run it separately at each aspect ratio/inset configuration.
5. Run the V1.9 flow harness and earlier world/population harnesses separately.
   Verify native display Apply/Keep/Revert/timeout/focus loss in a standalone
   development build. Check layout again after changing resolution or mode.

## Verification

**Passed:** 335 compiled assertions against the actual UiLayoutV19 model test
safe-area clipping/fallback, shrink-only menu fitting, shared HUD/toast surface
bounds, all supported HUD scales, prompt gaps and cluster separation across
11 screen/inset configurations. The existing 1,586 pure menu/focus/settings
regression assertions also pass. Both suites use C# 9.0/.NET 8 with warnings as
errors, without Unity stubs or NuGet dependencies.

Static validation covers eight new C# syntax trees, 115 scene YAML documents,
GUID/component/serialized-field references, title rectangle checks and the
shared-layout integration guards. Normalizing the four intended UI bindings,
the extra harness reference and version labels reproduces the previous scene.
The builder is idempotent. The 715-file staged baseline changes only in the
contract log; publication verifies every earlier remote blob outside that log.

**Not run:** Unity compilation/import, actual rendering/font size, mouse/
keyboard/controller input, native display switching, supplied harnesses and
performance. No Unity Editor is available here. Model/geometry checks do not
replace screenshots or runtime tests.

Evidence: `Jehad_V1.9.validation.json`. Commands: `Tools/Jehad/V1_9/README.md`.
Next: use Unity/standalone findings to tune readability and presentation on
the target display, then verify the complete world-to-boss experience.
