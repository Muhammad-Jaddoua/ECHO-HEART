# Jehad V1.12 — objective bearing and distance guide

Open **`Assets/_Project/Scenes/Jehad/Jehad_V1.12.unity`** in Unity 6000.3.27f1.

## Changes to review

- During exploration, a compact Guardian marker shows the direction and
  straight-line distance to the existing arena entry point. After the boss
  is defeated, it switches to the Island Heart.
- The marker uses a Sword/Heart glyph when its projected position fits the
  available HUD region and a rotating chevron when clamped. Targets behind
  the camera get a corrected bearing; a directly behind target uses a stable
  downward pointer. The marker is kept clear of the aiming center.
- The guide hides during the boss fight, menus, death, completion and any
  active interaction prompt. It follows **Settings → Interface → Objective
  and control hints**, and supports the existing HUD scale/high-contrast values.
- It shares the V1.9 safe-area surface and reserves room for the upper HUD
  and lower discovery/prompt cards. It adds no input handler or raycaster.
- The guide updates after the review camera's normal update order and only
  rebuilds its glyph/distance text when those values change.

This is a bearing/distance aid, not a pathfinder: terrain and cover can lie
between the player and the target. Secrets, chests and relics are not targets.
The existing exploration and interaction rules are unchanged.

## Integration

ObjectiveGuideV112 and its EditorOnly review harness are added to existing
scene roots. The authored Camera, arena-entry Transform and Heart Transform
are serialized references. The guide reads SessionFlowV13 and existing prompt
events; it never enters the arena, unlocks the Heart, grants rewards, teleports
the player or raises progression events.

All earlier menu/HUD/settings, notification, Guardian presentation, world
audio/particles, pickup/chest, population and flow implementations are reused.
No previous scene, original AI/player/combat/Heart implementation, release
scene, build setting, package or imported asset changes. Production player/
ability integration remains outstanding; this is still the review-player scene.

## Unity review

1. Begin a run. Walk and rotate/orbit the camera while watching the Guardian
   bearing and rounded-up distance. Check front/side/behind targets, directly
   behind the camera, and transitions across the camera plane. Follow the
   existing paths; the marker is not a route or obstruction indicator.
2. Test 16:9, ultrawide, 4:3 and safe-area insets at HUD scales 85%, 100% and
   115%. Inspect arrow rotation, text fit, high contrast and separation from
   health/objective/pause controls, aim guide and queued discovery cards.
3. Toggle objective/control hints. Pause, resume, approach/open chests and
   leave interaction range. Confirm the guide hides and returns without
   consuming a click, movement key or interaction.
4. Enter the arena and retry the fight: the entrance guide should stay hidden.
   Defeat the Guardian: the Heart guide should appear. Approach the Heart:
   the existing E prompt takes priority. Complete and start a new run: the
   guide should hide on completion and return to the Guardian afterward.
5. In Play mode select **Jehad V1.12 UI acceptance harness** and run **Objective
   review → Run guide checks (resets run)**. It checks hints/prompt/pause
   suppression, real arena entry, boss retry, Heart target switching, marker
   bounds, completion and New Game. It uses direct test damage to reach boss
   defeat and the real Heart interaction; it does not validate player combat.
6. Run the existing Guardian, notification, UI, flow and world-polish actions
   separately. Inspect the full experience in a standalone development build.

## Verification

**Passed:** 80 assertions compile actual ObjectiveGuidePolicyV112 with actual
CoreTypes and the shared layout model (C# 9.0/.NET 8, warnings as errors).
Coverage includes objective-stage journeys, settings/prompts, front/behind
bearings, invalid projection data, bounds, reserved HUD space and center
clearance across multiple surfaces and HUD scales. Existing 51 Guardian,
111 notification, 335 layout and 1,586 menu/focus/settings assertions pass.

Static checks cover three new C# syntax trees, 119 scene YAML documents,
serialized references to the authored camera/entry/Heart, input transparency
and absence of progression/reward calls in runtime guide code. Removing the
two added components, their component-list entries and version labels
reproduces V1.11 exactly. No new scene root is added. The builder is idempotent.
The 785-file staged baseline changes only in the contract log; publication
also verifies every prior remote blob outside that log.

**Not run:** Unity compilation/import, actual camera projection/rendering,
font fit, mouse/keyboard/controller input, supplied harnesses and profiling.
No Unity Editor is connected. Pure placement tests do not establish actual
camera behaviour or final-submission readiness.

Evidence: `Jehad_V1.12.validation.json`. Commands: `Tools/Jehad/V1_12/README.md`.
Next: inspect guide placement and camera bearings in Unity and tune presentation
from those findings while preserving exploration and combat balance.
