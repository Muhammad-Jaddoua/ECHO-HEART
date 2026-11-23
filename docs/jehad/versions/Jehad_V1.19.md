# Jehad V1.19 — immediate ambience silence at zero SFX

Open **`Assets/_Project/Scenes/Jehad/Jehad_V1.19.unity`** in Unity 6000.3.27f1.

## Behavior

The existing water, forest and alpine ambience loops use gradual volume fades.
That also allowed a short fade-out tail when Sound Effects reached zero. This
scene now sets those three source volumes to zero and stops their playback as
part of the settings-change callback.

Nonzero settings keep the original ambient fade behavior. Unmuting during
exploration lets the existing audio update fade the appropriate loops back in.
Music, world one-shots, stingers, Guardian voices, region selection, pause rules
and the V1.18 post-boss music handoff retain their existing owners and paths.
No audio clip, source or voice capacity is added.

## Integration and earlier versions

WorldAudioV15 gains an optional SilenceMutedAmbience method and a private
SilenceAmbient helper. The public hook returns for positive SFX volume and
otherwise touches only water, forest and alpine sources. It does not write
music levels, mix weights, world-voice levels, stingers or gameplay state.
All existing code, including EndGuardianMusic, is preserved exactly.

AmbientMuteV119 subscribes/unsubscribes to UiSettingsV17.Changed and applies
the hook once on enable. The settings and world audio components share the
same original volume provider. The bridge has no Update/LateUpdate or runtime
allocation loop. Earlier scenes lack the bridge and retain their behavior.

AmbientMuteReviewV119 is added to the existing EditorOnly acceptance root.
Removing the two new components, their list entries and version labels
reproduces V1.18 exactly. No existing serialized field or scene root is added
or changed. Earlier scenes, release scene, build settings, assets and original
gameplay/presentation owners remain preserved.

Production player/ability integration is still outstanding in this
movement-only review-player scene. This fix does not establish final-submission
readiness.

## Unity review

1. Explore near water, in woodland and on the mountain. Open Settings promptly
   and set Sound Effects to zero while ambience is still fading. Listen for
   the absence of the old ambience tail. Inspect the three source volumes and
   stopped playback in the Editor; audible behavior also depends on the native
   audio device/buffer and requires listening.
2. Restore a positive sound level and resume. Confirm the original smooth
   ambient fade-in. Check several positive volume levels, zero/nonzero saved
   preferences after relaunch, Restore Defaults and repeated mute/unmute.
3. Check Music separately. Muting ambience should leave music gain, regional
   crossfades and the post-Guardian transition unchanged. Test pause/resume,
   retries, title and completion with both audio sliders at zero/nonzero values.
4. In Play mode select **Jehad V1.19 UI acceptance harness** and run **Audio
   review → Run ambience mute checks (restores settings, resets run)**. It
   starts main-area ambience, changes SFX through the real settings owner,
   checks zero/stopped sources in the same call, music independence, sustained
   mute, another interface change while muted, unmute and pause/resume. It
   temporarily changes SFX and hints, restores/persists the original settings
   in its finally block, resets the run and returns to title.
5. Run the inherited Guardian music, world polish, UI, title and flow actions
   separately. Audition/profile the standalone development build. The context
   action checks source state, not sound quality, device latency or every
   production gameplay cue.

## Verification

**Passed:** the ten existing pure C# suites pass **8,709 assertions** under
C# 9.0/.NET 8 with warnings as errors. These exercise reused models and policies;
they do not execute Unity settings callbacks or AudioSource operations.

Static checks cover two new C# syntax trees, the extended audio source,
128 scene YAML documents, audio/settings references, event cleanup and the
three-source mute scope. Removing the two optional methods reproduces the
verified V1.18 audio source exactly. Earlier scenes contain no mute bridge.
The builder is idempotent.

The 927-file staged baseline changes only in WorldAudioV15.cs and the contract
log. Publication verifies every prior remote blob outside those files; all
earlier scenes are preserved.

**Not run:** Unity compilation/import, actual settings callbacks, audible
mute/unmute, music independence, context actions, device behavior or profiling.
No Unity Editor is connected. Native review and listening remain required.

Evidence: `Jehad_V1.19.validation.json`. Commands: `Tools/Jehad/V1_19/README.md`.
Next: audition mute/unmute in the three environments and verify the saved
volume options after relaunch.
