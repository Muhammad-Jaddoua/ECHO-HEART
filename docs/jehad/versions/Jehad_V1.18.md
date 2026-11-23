# Jehad V1.18 — calm music after the Guardian

Open **`Assets/_Project/Scenes/Jehad/Jehad_V1.18.unity`** in Unity 6000.3.27f1.

## Behavior

Previously, the Guardian music override stayed active after the real boss
defeat, continuing until title/completion or a reset. This scene now releases
that override on the existing BossDefeated event. The existing regional selector
chooses the exploration track for the player's current position, and the
existing continuous music weights crossfade toward it.

Exploration ambience and local ambient motes can return through their existing
non-Guardian guards. The resolver does not stop the dedicated defeat sound,
world one-shots or restoration stinger. Music and Sound Effects retain their
existing independent volume controls, ducking and mute paths. No audio clip,
source, particle system or voice capacity is added.

Boss-area retry uses the original reset and BossEncounterStarted path, which
selects Guardian music again. Pause/resume after victory retain the regional
target. Title and New Game keep their existing behavior. Combat, Heart unlock,
completion and retry rules are unchanged.

## Integration and earlier versions

WorldAudioV15 gains a small optional public EndGuardianMusic method. It returns
immediately when the Guardian override is already inactive; otherwise it clears
that override and requests the normal region without changing current mix
weights or stopping sound sources. All pre-existing statements, fields and
subscriptions in that file remain unchanged.

GuardianMusicResolveV118, attached to the existing world-presentation root,
subscribes/unsubscribes to BossDefeated and calls that method on the existing
audio owner. It creates no objects and has no per-frame loop. Older scenes have
no resolver, so the optional API addition does not change their execution.

GuardianMusicReviewV118 is attached to the existing EditorOnly acceptance root.
Removing these two components, their component-list entries and version labels
reproduces V1.17 exactly. The original release scene, build settings, assets,
all typed presentation bindings and gameplay owners are preserved. No new
scene root is added.

The review-player scene still lacks production player/ability integration.
This audio fix does not establish final-submission readiness.

## Unity review

1. Start a run and enter the real arena. Listen to the Guardian transition and
   warnings. Defeat the boss using the integrated gameplay when available, or
   the development acceptance action for presentation review.
2. Listen to the defeat cue and music handoff. The final hit/defeat tail should
   remain audible while the combat loop fades into the local exploration
   arrangement. Inspect returning ambience/motes and confirm the Heart remains
   the existing next objective. Do not treat the target-region property as a
   substitute for listening to the actual mix.
3. Pause/resume after victory, then retry the boss area. Confirm the original
   boss reset restores Guardian music. Defeat again and restore the Heart;
   inspect completion audio, Title and New Game. Check zero and nonzero Music/
   Sound Effects levels and the separate Guardian warning/impact voices.
4. In Play mode select **Jehad V1.18 UI acceptance harness** and run **Audio
   review → Run Guardian resolution checks (resets run)**. It enters the real
   arena, applies accepted test damage to the actual boss Health, checks the
   regional target, repeated resolution, Guardian source-volume decay when
   audible, pause/resume, boss retry, Title and New Game. It resets the run
   and returns to title; it does not validate player attacks or sound quality.
5. Run the inherited Guardian, world polish, flow, UI, title and preference
   actions separately. Audition and profile a standalone development build.

## Verification

**Passed:** ten existing pure C# suites pass **8,709 assertions** under C# 9.0/
.NET 8 with warnings as errors: 6,335 for actual region/mix/chest policies and
2,374 for the reused presentation/UI models. They include continuous/normalized
weights and rapid crossfade reversals. They do not execute Unity callbacks or
the optional audio-owner method.

Static checks cover two new C# syntax trees, the extended audio source,
126 scene YAML documents, audio/arena references, event cleanup and unchanged
audio pool sizes. Removing the optional method reproduces the verified prior
WorldAudioV15 source exactly. The only runtime/development callers are the new
resolver and review action; earlier scenes do not contain the resolver.

The builder is idempotent. The 914-file staged baseline changes only in
WorldAudioV15.cs and the contract log. Publication verifies every prior remote
blob outside those two files; all earlier scenes are preserved.

**Not run:** Unity compilation/import, actual BossDefeated delivery, music/SFX/
ambience playback, real defeat/retry integration, context actions or profiling.
No Unity Editor is connected. Native review and listening remain required.

Evidence: `Jehad_V1.18.validation.json`. Commands: `Tools/Jehad/V1_18/README.md`.
Next: audition the defeat-to-exploration handoff and boss retry in Unity.
