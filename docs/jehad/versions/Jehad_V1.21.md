# Jehad V1.21 — jump and landing feedback

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.21.unity` in Unity 6000.3.27f1.

Walking off a ledge previously played the same jump cue as an upward jump.
The new scene requests Jump only when a grounded-to-airborne transition has
sampled upward velocity above 0.5 m/s. Landing sound/dust needs at least 0.12
seconds of airborne samples and either a detected jump or downward speed of
at least 3 m/s. Brief ground-contact changes therefore stay quiet on landing.
A very short blocked jump can still have a departure cue without a landing cue.

The presenter reads the existing CharacterController after movement and uses
scaled delta time. It does not read jump input or move the player. This is a
motion-based presentation heuristic, not an authoritative jump event: an
upward launch from the ground can qualify, and a non-CharacterController motor
needs an explicit integration review. Production player/ability integration
remains outstanding in this movement-only review-player scene.

Pause holds the airborne phase without adding elapsed time or replaying cues.
Retry, title, death, completion, unavailable/disabled controller, invalid speed
and sampled 3D displacement of at least 8 m clear old motion. A reset waits for
ground contact before arming another departure. Retry also retains the existing
0.3-second feedback grace period. Spawn/teleport settling cannot play a landing.
Small teleports below the distance threshold are not distinguishable from
ordinary motion unless they go through the existing retry transaction.

The existing audio and particle owners receive the same Jump, Land and Dust
requests. No clip, source, particle pool, light or normal-gameplay collider is
added. Footsteps, health, damage, input, abilities, pickups, enemy/boss behavior,
menus and previous presentation components retain their existing owners.

WorldFeedbackV15 gains a default-false, nonserialized ExternalAirborneCues
property guarding only its two original Jump/Land branches. AirborneFeedbackV121
opts this scene in and restores the previous flag on disable. Earlier scenes
have no adapter and keep their legacy behavior. The new scene adds this runtime
component and one EditorOnly review component to existing roots; no old scene
or existing serialized component field is changed.

## Unity review

1. Jump repeatedly from flat ground at walk/sprint speed. Listen for one
   departure and one landing; inspect dust placement and sound timing.
2. Walk off a ledge without jumping. Expect no Jump sound, then Land/dust after
   the meaningful fall. Traverse small steps, uneven ground and slopes; brief
   grounding jitter should not cause repeated landing bursts. Check short hops
   and jumping beneath a low ceiling as well.
3. Pause during ascent and descent, wait, then resume. Check there is no replay
   on resume, and the eventual real landing still responds. Retry in midair,
   return to title, die, start New Game and use fall recovery; check settling is
   quiet and later movement still works.
4. In Play mode select **Jehad V1.21 UI acceptance harness**, then run
   **Movement audio → Run grounded cue checks (resets run)** separately from
   other review actions. The action temporarily creates two Environment-layer
   platforms above the world, disables review controls and moves the actual
   CharacterController through a jump and ledge fall. It checks cue-request
   counts, pause/resume and retry, destroys its fixture in finally and returns
   to title. This resets the run. Counters establish requests, not audible
   playback or production input/physics quality.
5. Run inherited chest, sparkle, audio, menu, Guardian and flow actions
   separately. Audition a standalone development build at different frame
   rates and inspect allocations/frame cost. Review final sound mix by ear.

## Verification

Passed: **92 new assertions** compile the actual MovementCuePolicyV121 under
C# 9.0/.NET 8 with warnings as errors and no Unity stubs. Scenarios cover jump
versus ledge departure, landing thresholds, brief contact loss, paused phase,
reset/teleport settling, invalid samples, bounded duration and 30/60/144 Hz
motion sequences. The eleven existing suites pass 8,737 assertions, for
**8,829 total** across twelve suites.

Static checks cover three new C# syntax trees, the extended feedback source,
131 scene YAML documents, component/GUID/local references, the single listener
and the EditorOnly review root. Removing the two new components and version
labels reproduces V1.20 exactly. Removing the optional property and two-branch
guard reproduces the verified original feedback source byte-for-byte.

The builder is idempotent. The 958-file staged baseline changes only in
WorldFeedbackV15.cs and the contract log. Publication compares the remote tree
against the original main tree so unrelated remote files are preserved.

Not run: Unity compilation/import, actual motor/CharacterController behavior,
audible playback, dust synchronization, pause/retry integration, native review
action, standalone behavior or profiling. No Unity Editor is connected.
Pure/static checks do not establish native behavior or submission readiness.

Evidence: `Jehad_V1.21.validation.json`. Commands: `Tools/Jehad/V1_21/README.md`.
Next: perform the Unity review above, especially slopes, ledge falls and pausing
in midair, then tune the thresholds against the production motor when available.
