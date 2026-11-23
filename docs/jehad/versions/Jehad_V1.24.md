# Jehad V1.24 — clear the session jingle when returning to title

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.24.unity` in Unity 6000.3.27f1.

The source audit found that WorldAudioV15 stopped world effects on title entry
but left its dedicated session-stinger source playing. Returning quickly from
Death or Completion could therefore carry the previous jingle onto the title
screen. Retry and audio-component disable already stopped that source.

TitleAudioCleanupV124 listens to the existing StateChanged event and requests
StopSessionStinger only on Title, including when enabled while already at title.
The new hook stops only the dedicated session-stinger source. It leaves its clip
binding intact and does not stop regional music, ambience, world SFX or UI audio,
change volume options, select music, restart playback or publish gameplay events.
Original music routing/crossfades and normal death/completion cue playback retain
their existing owners. The original retry cleanup is unchanged.

WorldAudioV15 gains one optional parameterless method; every existing statement
is preserved. Only the new scene contains the bridge that calls it. Earlier
scene files are unchanged and retain their original routing. The new scene adds
one runtime adapter and one EditorOnly review component to existing roots;
no previous component field, clip, source, pool or gameplay system is replaced.

## Unity review

1. Open V1.24 and check Console compilation before testing audio.
2. Die, then quickly return to title while the death jingle is still playing.
   Confirm the old jingle stops and the normal title music transition continues.
   Repeat with SFX muted and at a low nonzero level.
3. Complete the Guardian/Heart sequence. Let the restoration jingle play on the
   completion screen; return to title before it ends and inspect the cutoff.
   Check New Game from completion and Retry from death retain their existing
   cleanup behavior. Also check ordinary pause/resume and regional ambience.
4. In Play mode select **Jehad V1.24 UI acceptance harness**, then run
   **Audio review → Run title cue cleanup checks (resets run)** separately from
   the other actions. It uses actual Health defeat to enter Dead, checks the
   death cue before returning to title, and exercises the existing retry path.
5. The action then enters the actual arena, defeats the Guardian through Health,
   teleports near the Heart and calls its real proximity-checked interaction.
   It checks the restoration cue in Complete before returning to title. For
   both title transitions it checks the session source stopped, retained its
   clip, kept volume options/eight world voices and did not synchronously stop
   or start any of the six music tracks. The normal crossfade advances later.
6. The action resets the run, temporarily disables review controls for the Heart
   interaction and returns to title in finally. It never calls the cleanup hook,
   plays a cue or publishes progression events directly. Its source-state checks
   still need manual audition; they do not establish production player/input
   integration, combat balance or final sound quality.

## Verification

Passed: the existing Roslyn namespace audit indexes 157 project scripts and
checks 121 Jehad scripts under Editor and Player symbols, with no missing-import
candidates or syntax errors. The source digest matches the current project.
This tool is a focused syntax/import audit, not Unity compilation or full C#
symbol binding; its limitations are documented in Tools/Jehad/V1_22/README.md.

Static checks cover two new and one extended C# syntax trees, 135 scene YAML
documents, typed runtime/review references and one listener. Removing the two
new components/version labels reproduces V1.23 exactly. Removing the one new
hook reproduces the verified original audio source exactly. All 155 earlier
project source blobs match the inspected remote tree after that reversal.
The 1,039-file staged baseline changes only in WorldAudioV15.cs and the contract
log. The builder is idempotent.

No new pure policy is introduced, so earlier gameplay-model suites were not
rerun. No claim of native audio behavior follows from those older results.

Not run: Unity import/compilation, native review action, actual cue/source timing,
audible transitions, music/ambience mix, standalone gameplay or profiling. No
Unity Editor is connected. Source/static checks do not establish submission
readiness.

Evidence: `Jehad_V1.24.validation.json`. Commands: `Tools/Jehad/V1_24/README.md`.
Next: audition the death/completion/title transitions in Unity, then continue
the world/menu/audio playthrough and record observed defects.
