# Jehad V1.16 — scenic island title view

Open **`Assets/_Project/Scenes/Jehad/Jehad_V1.16.unity`** in Unity 6000.3.27f1.

## Behavior

The title home screen gains a gentle scenic view of the existing 3D island.
The camera orbits up to four degrees either side of the original title bearing
and rises up to 2.4m while keeping the original island focal point in view.
The nominal loop takes 90 seconds of active title animation. It starts at the
existing fixed title pose and adds no roll or shake.

Settings, Controls, Credits and confirmation dialogs hold the background still
without consuming the scenic phase. Returning home resumes from that phase.
In a standalone build, focus loss also holds the view. Editor playback remains
available while using the Inspector, so standalone focus behavior needs its
own check. A frame consumes at most 0.1 seconds of scenic time to limit jumps
after a stall or focus return; sustained low frame rates lengthen the loop.

**Reduce UI motion** immediately restores the fixed title pose on every title
page. Disabling the vista while in Title also restores that pose. Each title
entry captures the original flow's newly applied title pose and starts fresh.

The presenter exits before any camera write outside Title. Playing, pause,
death, completion and player-camera reset behavior remain with the existing
owners. It adds no input handler, UI control, audio source, light or asset.

## Integration

TitleVistaV116 is attached to the existing interface-settings root. It reads
SessionFlowV13, FlowMenuV115, UiSettingsV17 and the authored camera Transform.
The serialized focus matches SessionFlowV13's original title look-at point.
The fixed position/rotation are captured after the flow publishes Title;
LateUpdate applies only the bounded title offsets.

TitleVistaMotionV116 is a pure motion model. It retains phase across menu/focus
holds, resets for reduced motion/title reentry, rejects invalid elapsed time
and wraps phase each cycle. The runtime limits camera-writing code to its
guarded title refresh and title-only disabling cleanup.

TitleVistaReviewV116 is added to the existing EditorOnly acceptance root.
Removing these two components, their component-list entries and version labels
reproduces V1.15 exactly. Existing menus/HUD/preferences, world/audio/effects,
player/AI/combat, release scene, build settings and earlier snapshots are
preserved. No new scene root is added. The scene still uses the movement-only
review capsule; production player/ability integration remains outstanding.

## Unity review

1. Inspect the title for a full loop. Check the visible island composition,
   focal point, terrain/foliage clearance, camera limits and motion comfort.
   Inspect 16:9, ultrawide and 4:3 views with the title card visible. The pure
   orbit bounds do not prove actual scene clearance or pleasing framing.
2. Open Settings, Controls, Credits and a confirmation. Confirm the view holds
   while navigating, then resumes smoothly on Home. Check the existing focus,
   mouse/keyboard/controller navigation and confirmation actions.
3. Turn Reduce UI motion on, including while on the Settings page. Confirm the
   original fixed view returns immediately and stays fixed. Turn it off and
   return Home: motion should restart gently from the beginning. Check that
   V1.14 preference restoration applies this choice on relaunch.
4. Start a run, orbit/zoom the review camera, pause/resume, die/retry, complete
   and return to title. Confirm the vista never overrides a non-Title camera
   pose and title reentry always starts from the original authored view.
5. In a standalone development build, switch focus away and return after a
   long interval. Confirm a held phase and limited first resumed step. Profile
   frame cost while the camera reveals the actual island geometry.
6. In Play mode select **Jehad V1.16 UI acceptance harness** and run **Title
   review → Run vista checks (resets run)**. It checks home motion, Settings/
   confirmation holds, fixed reduced-motion view, disabling cleanup and no
   camera write during Playing or Pause. It resets the run and temporarily
   changes the motion option, then restores/persists the original preferences
   and returns to title. It does not execute the Quit confirmation or validate
   actual framing, clearance, death/completion views or standalone focus.
7. Run inherited vitality, preference, UI, flow, Guardian and world actions
   separately. Review the full experience in a standalone development build.

## Verification

**Passed:** 30 assertions compile the actual motion model under C# 9.0/.NET 8
with warnings as errors. Tests cover bounded and finite offsets, holds, reduced
motion, frame-step limits and continuity across 100,000 steps. Eight earlier
suites pass 2,344 assertions, for **2,374 total**.

Static checks cover three C# syntax trees, 124 scene YAML documents, authored
camera/focus references, menu/settings wiring, prior-scene preservation and
camera-write locations/guards. The builder is idempotent. The 873-file staged
baseline permits only the contract log to change; publication verifies all
earlier remote blobs outside that log.

**Not run:** Unity compilation/import, actual callback ordering and camera
execution, framing/geometry clearance, UI rendering/input, standalone focus,
context actions or profiling. No Unity Editor is connected. Pure offset tests
do not establish native behavior, visual quality or final-submission readiness.

Evidence: `Jehad_V1.16.validation.json`. Commands: `Tools/Jehad/V1_16/README.md`.
Next: review the title framing and motion in Unity and tune from those findings.
