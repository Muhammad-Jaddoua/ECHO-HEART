# Jehad V1.26 — show the current relic total when pausing

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.26.unity` in Unity 6000.3.27f1.

RelicCounterV13 refreshes its published total every 0.1 seconds. FlowMenuV117
builds its pause summary when StateChanged(Paused) arrives. Pausing immediately
after a trigger pickup could therefore show the previous count. The later
counter event updates the HUD but does not rebuild the already-open summary.

An accepted SessionFlowV13.Pause now asks the existing counter to Refresh(false)
before publishing the paused state. The summary then reads the current pickup
flags through the original count owner. The false argument preserves its
existing no-duplicate-event rule when the total has not changed. Rejected pause
requests do not refresh or publish anything. Normal counter polling remains at
0.1 seconds; one extra scan occurs only on an accepted pause.

The original readiness/transition/policy guards are preserved. Other flow
methods, time-scale/input ownership, pickup/collection flags, reward events,
retry persistence, menu wording and timer behavior are unchanged. If necessary,
the counter emits its existing CollectiblesChanged update before the existing
StateChanged(Paused) notification. No new gameplay event or publisher is added.

The shared flow fix also applies to earlier scenes that use SessionFlowV13.
Their scene files are preserved. V1.26 adds one EditorOnly review component to
the existing acceptance root and changes no previous serialized field or runtime
component binding.

## Unity review

1. Open V1.26 and check Console compilation.
2. Start New Game, collect a loose relic and pause immediately. Compare the pause
   summary with the actual collected total. Repeat at different frame rates and
   with a chest reward after its lid clears.
3. Resume and pause repeatedly without collecting more. Check the number stays
   correct, controls and time freeze/resume normally, and no duplicate reward or
   discovery is produced. Check main-area retry, boss-area retry and New Game
   retain their established collection/reset rules.
4. In Play mode select **Jehad V1.26 UI acceptance harness** and run
   **Pause review → Run immediate pickup summary checks (resets run)** separately
   from other actions. It resets the run and temporarily disables only the
   counter component's automatic Update polling. Its public Refresh method and
   the original pickups remain in use.
5. The action teleports to an available loose relic and waits for the real
   trigger collection. With polling held, it requires the published count to
   remain stale, then calls the real Pause method. It checks the count/event
   ordering, visible pause summary, time scale, repeated rejected pause and a
   subsequent unchanged-count pause without another collectible event.
6. Finally it removes its event listeners, restores the counter's previous
   enabled state and returns to title. It does not set collection flags, publish
   events directly or award a substitute reward. The polling hold makes the
   otherwise short timing window reproducible; ordinary input/physics timing
   still needs manual playtesting.

## Verification

Passed: **445 existing assertions across six scenarios** compile the actual
SessionPolicyV13 and CoreTypes under C# 9.0/.NET 8, warnings as errors and no
Unity stubs. These are flow-policy regressions; they do not execute the changed
Unity Pause method, trigger physics, counter refresh or visible summary.

The namespace audit indexes 159 project scripts and checks 123 Jehad scripts
under Editor and Player symbols, with no missing-import candidates or syntax
errors. It remains a focused source audit rather than Unity compilation or full
C# symbol binding; see Tools/Jehad/V1_22/README.md for its limits.

Static checks cover the new review script, edited flow source and 136 scene
YAML documents. They inspect the accepted Pause branch and its refresh-before-
publication order. Reversing that branch edit reproduces the verified original
flow source exactly. Removing the EditorOnly component/version labels reproduces
V1.25 exactly. All 158 earlier project source blobs match the inspected remote
tree after that reversal. The 1,065-file staged baseline changes only in the flow
source and contract log. The scene builder is idempotent.

Not run: Unity import/compilation, real pickup-to-pause timing, the new native
review action, visible UI/playthrough or profiling. No Unity Editor is connected.
The policy/source checks do not establish the native snapshot behavior or final
submission readiness.

Evidence: `Jehad_V1.26.validation.json`. Commands: `Tools/Jehad/V1_26/README.md`.
Next: run the deterministic pickup/pause action and inspect ordinary immediate
pausing during the world/menu playthrough.
