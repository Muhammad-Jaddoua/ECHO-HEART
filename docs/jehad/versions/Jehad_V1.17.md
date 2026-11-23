# Jehad V1.17 — an open title screen for the island vista

Open **`Assets/_Project/Scenes/Jehad/Jehad_V1.17.unity`** in Unity 6000.3.27f1.

## Changes

The previous full-width title card obscured much of the scenic island view.
The title now uses an unpainted 1320×760 layout frame, a 600px left menu panel
and a compact 510×140 power legend at the lower right. The remaining right
side exposes the existing 3D island and its V1.16 camera motion.

The logo and introduction are sized for the left panel. Begin adventure,
Settings, Controls, Credits and Quit keep their existing positions, sizes,
actions and navigation order. The three power icons remain a non-interactive
legend. No title control or ability-selection feature is added.

Title Home uses a lighter 16% dark backdrop shade. Subpages, confirmations and
other menu states retain the normal 48% shade; the existing modal overlay still
isolates confirmation input. The opaque-panel contrast option remains active,
and the global backdrop continues to block world clicks while menus are open.

## Integration

MenuScreensV117 changes only BuildTitle. FlowMenuV117 caches the existing
backdrop graphic and selects its shade in Render. The cancel handler and typed
UI/flow harnesses are copied for the new menu type. TitleVistaV117 and its
typed review component preserve V1.16 camera behavior while binding to the new
menu type; TitleVistaMotionV116 is reused unchanged.

The scene changes five script bindings and version labels. It adds no scene
root, serialized component field, gameplay component, asset, light or sound.
V1.15 health feedback, V1.14 preferences, existing settings/display preview,
navigation, world/audio/effects and original gameplay owners are preserved.
Earlier scenes, release scene and build settings remain unchanged.

The scene still uses the movement-only review capsule. Production player and
ability integration remain outstanding; this title polish does not establish
final-submission readiness.

## Unity review

1. Inspect Title Home for a full scenic loop. Confirm the island has space on
   the right and the camera's focal area reads clearly alongside the menu.
   Check terrain/foliage clearance and the power legend against the actual
   scene, including at 16:9, ultrawide, 4:3 and safe-area insets.
2. Inspect logo/description wrapping, button labels and spacing, and the three
   compact power labels/icons. Test high contrast and both motion settings.
   The logical rectangle checks do not prove native font fit or composition.
3. Navigate with mouse, keyboard and controller. Check that all five title
   actions retain their behavior, Back restores focus, and clicking the open
   scenery does not activate gameplay. The power legend is decorative.
4. Open Settings, Controls, Credits and confirmations. Check the normal darker
   shade and held vista; return Home and check the lighter shade and resumed
   motion. Verify reduced motion still restores the fixed view. Inspect all
   other menu states and the transition into gameplay.
5. In Play mode select **Jehad V1.17 UI acceptance harness** and run **UI review
   → Run open title layout checks (resets run)**. It checks the unpainted root,
   left panel, compact legend, five title controls and their containment,
   current safe-area button bounds, and Home/Settings/confirmation shade
   changes. It resets the run and returns to title. It opens and cancels the
   Quit confirmation without accepting it.
6. Run the inherited Title review action and other UI, vitality, preference,
   notification, flow and world actions separately. Inspect standalone focus,
   rendering and frame cost in a development build before accepting the change.

## Verification

**Passed:** the nine existing pure C# suites pass **2,374 assertions** under
C# 9.0/.NET 8 with warnings as errors. They exercise reused models and policies;
they do not validate the new visual composition.

Static checks cover seven C# syntax trees, 124 scene YAML documents, typed
menu/vista/harness references and scoped source changes. The title's button
and text rectangles fit the left panel. About 47.4% of the logical title frame
has no large panel over it; the central logical focal region is clear of both
panels. These are authored layout measures, not screenshot or visibility tests.

Normalizing the five script bindings and version labels reproduces V1.16
exactly. Normalizing BuildTitle, backdrop shading and the new review action
reproduces the inherited sources. The builder is idempotent. The 891-file
staged baseline permits only the contract log to change; publication verifies
all earlier remote blobs outside that log.

**Not run:** Unity compilation/import, actual rendering, font fit, input,
camera framing/clearance, context actions and profiling. No Unity Editor is
connected. Native review is required before treating the design as accepted.

Evidence: `Jehad_V1.17.validation.json`. Commands: `Tools/Jehad/V1_17/README.md`.
Next: review the open title composition and text in Unity, then tune from the
actual screen and playtest findings.
