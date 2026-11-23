# Jehad V1.14 — remembered audio and interface options

Open **`Assets/_Project/Scenes/Jehad/Jehad_V1.14.unity`** in Unity 6000.3.27f1.

## Behavior

Music volume, Sound Effects volume, HUD scale, high contrast, reduced UI motion,
objective/control hints and the ranged aim guide are remembered across launches.
The settings footer reports automatic saving, pending changes or a caught
storage failure. Restore Defaults uses the same save path.

Display settings retain their existing session-only behavior, explicit Apply,
15-second preview and Keep/Revert actions. Run progress remains session-only:
New Game, collectibles, discoveries, chests, player powers and the boss keep
their existing reset rules. The preference record contains only the seven
audio/interface values listed above. It adds no Continue option or snapshot
system.

## Integration

PreferenceStoreV114 loads one versioned PlayerPrefs record through the existing
UiSettingsV17.Set method. Execution order is settings (-220), store (-210),
menu (-200), so the menu starts with restored option values. Existing audio
volume setters remain the audio owners. Missing, unsupported or malformed
records leave the current session defaults intact. Finite out-of-range values
use UiValuesV16's existing clamps; nonfinite data is rejected.

The invariant-culture codec stores three round-trip floats and four interface
flags. The key is `Echoheart.Jehad.AudioInterface.v1`. No display mode,
resolution, quality level, frame limit or gameplay state is written to it.

Rapid edits are coalesced for half a second of unscaled time. Regular writes
run outside Playing; state transitions flush a pending edit before the next
gameplay frame. Focus loss, application pause/quit and component disabling also
flush pending edits. Reverting a slider to its last stored value cancels its
write, and display-only notifications do not create writes. A caught write
failure retains the dirty record, attempts to restore the prior PlayerPrefs
memory value, reports session-only operation and avoids a per-frame retry loop.
The next changed preference or lifecycle flush can retry. Native storage and
failure handling still require testing in Unity.

FlowMenuV114, MenuScreensV114 and UiCancelV114 preserve V1.9 menu/HUD/layout,
navigation, confirmation and display-preview behavior. Their scoped additions
bind the store and refresh its status text. The new scene rebinds both typed
review harnesses to the new menu. UiReviewHarnessV114 adds a storage action;
its prior navigation, notification and settings checks are retained.

One component is added to the existing settings root; no root, UI control,
audio source, light or imported asset is added. Earlier scenes/scripts,
V1.13 sparkles, guide, Guardian cues, discovery messages, world/audio, original
gameplay owners, release scene and build settings are preserved.

The scene still uses the movement-only review capsule. Production player/
ability integration remains outstanding, and this checkpoint does not establish
final-submission readiness.

## Unity review

1. Import/open V1.14 and check the Console. Open Settings and change both audio
   volumes, HUD scale and all four interface switches. Confirm the current
   audio/HUD respond immediately and the footer returns from Saving to its
   automatic-save message. Inspect footer fit at all supported aspect ratios.
2. Make several quick slider changes, then immediately resume or start a run.
   Pause again and verify the final values. Check the profiler for disk work
   around idle-menu saves and state transitions. No periodic preference write
   should occur during ordinary Playing frames.
3. In a standalone development build, choose distinctive values, close and
   relaunch. Verify the title/settings/HUD and audio use those values at startup.
   Relaunch after changing HUD scale, hints-off and muted audio specifically.
   Editor and standalone PlayerPrefs stores can differ; test each independently.
4. Exercise Display Apply, Keep, timeout, Revert and focus loss. Confirm display
   changes retain their existing preview policy and do not become audio/interface
   data. Test Back, nested confirmations and keyboard/controller focus.
5. Confirm Restore Defaults, close and relaunch, then check default audio and
   interface values. Try retries, title return, completion and New Game: these
   actions should retain preferences while following the original run resets.
6. In Play mode select **Jehad V1.14 UI acceptance harness** and run **UI review
   → Run preference storage check (restores settings)**. It changes a volume
   and hints, flushes through the actual store, checks the native string value,
   tests the default record and restores/persists the original option values
   in its finally block. This action writes preferences; it does not simulate
   process restart or prove durable storage after relaunch.
7. Run the inherited UI, notification, flow, Guardian and world review actions
   separately. For controlled storage-error testing, check that an unavailable
   preference backend leaves the session usable and the footer reports failure.
   Do not clear unrelated preferences when inspecting the single versioned key.

## Verification

**Passed:** 79 assertions compile the actual new codec/save model with the
existing UiValuesV16 and CoreTypes under C# 9.0/.NET 8, warnings as errors.
The previous six suites pass 2,224 assertions, for **2,303 total**.

Static checks cover seven new C# syntax trees, 122 scene YAML documents,
serialized settings/store/menu/harness bindings, explicit load order and
limited persistence scope. Reversing the three script rebindings, two new
serialized references, added store component/list entry and version labels
reproduces V1.13 exactly. Normalizing the scoped additions also reproduces
the inherited menu/harness code exactly. The builder is idempotent.
The 821-file staged baseline allows only the contract log to change;
publication verifies all earlier remote blobs outside that log.

**Not run:** Unity compilation/import, actual startup callbacks, PlayerPrefs
I/O, standalone relaunch, supplied context actions, visual/input/audio checks,
storage failures or profiling. No Unity Editor is connected. The pure C# tests
exercise record handling and scheduling, not the Unity backend.

Evidence: `Jehad_V1.14.validation.json`. Commands: `Tools/Jehad/V1_14/README.md`.
Next: perform the standalone relaunch and UI review before choosing another
polish change from observed playtest findings.
