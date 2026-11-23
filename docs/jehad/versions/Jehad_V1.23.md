# Jehad V1.23 — keep fall hurt feedback with the recovered player

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.23.unity` in Unity 6000.3.27f1.

SessionFlowV13 recovers a player below y=-18 by teleporting to the current safe
point and then applying one fall-damage hit. DamageInfo intentionally retains
the old fall position. ActorFeedbackV15 previously used that old position for
both hurt audio and hit particles, leaving the feedback below the world while
the player had already returned to safety.

In this scene, player impact feedback more than 8 m from the current Health
actor uses an anchor 1 m above that actor instead. Nearby contacts, including
exactly 8 m, keep their original impact position. Invalid selected distances
also use the anchor for player feedback. Enemy/boss hit placement is preserved.
This is a presentation heuristic for a stale contact point; it does not identify
fall damage or modify the DamageInfo passed to other listeners.

The existing PlayerHurt sound and Hit particle burst share the selected point.
No extra sound, particle system, light, collider, health event or damage hit is
created. Original fall recovery, damage amount, invulnerability, death, retries,
combat/AI, menus and collectible ownership are unchanged. Fatal falls still use
the existing death flow, which can stop world cues and clear hit particles.

## Integration

WorldFeedbackV15 gains a default-false, nonserialized LocalPlayerHitFeedback
option. ActorFeedbackV15 adds only a guarded point override before its existing
audio/particle calls. PlayerHitAnchorV123 opts this scene in and restores the
prior flag on disable. Earlier scenes have no adapter and keep their legacy
placement. No existing scene file or serialized component field is changed.

The new scene adds the runtime adapter and an EditorOnly review component to
existing roots. The review uses the existing flow, feedback and audio bank.
The production player is still an integration dependency; review the Health
transform and body anchor again when that prefab is connected.

## Unity review

1. Open the scene and check Console compilation before testing behavior.
2. Take an ordinary nearby hit. Its sound/particles should stay at the contact
   point. Fall below the recovery threshold with more than one HP remaining;
   check that one damage is applied after recovery and its sound/particles
   appear at the recovered body. Compare what is heard with the new position.
3. Repeat after main-area and boss-area retries. Check nearby enemy/boss hit
   placement, death/completion, music, volume/mute, pause and repeated New Game.
4. In Play mode select **Jehad V1.23 UI acceptance harness** and run
   **Player hurt → Run recovery feedback checks (resets run)** separately from
   other actions. It resets the run, temporarily disables review controls and
   resets player HP between cases. It checks an ordinary contact, a distant
   contact, then teleports the actual player below the world so the real flow
   must recover it and apply one fall hit. It verifies the existing hurt voice
   is positioned correctly and the eight-voice pool is preserved.
5. The review action clears existing world-voice clips between cases to avoid
   mistaking an older sound for a new request, then returns to title in finally.
   It does not create sources or alter normal audio behavior. Voice positioning
   does not prove audible playback, particle appearance or production input.

## Verification

Passed: **39 assertions** compile the actual HitFeedbackPointV123 policy under
C# 9.0/.NET 8, warnings as errors and no Unity stubs. They cover nearby/far
thresholds, invalid values, player-only eligibility, vertical/horizontal/combined
displacement and a representative below-world recovery distance.

The namespace audit indexes 155 project scripts and checks 119 Jehad scripts
under Editor and Player symbols, with no missing-import candidates or syntax
errors. This is a focused source audit, not Unity compilation or full C# symbol
binding; its limitations remain documented in Tools/Jehad/V1_22/README.md.

Static checks cover three new and two extended C# syntax trees, 133 scene YAML
documents, typed component/bank references and one listener. Removing the two
new components/version labels reproduces V1.22 exactly. Removing the optional
guard/property reproduces the verified actor/world feedback sources exactly;
all 152 pre-existing project source blobs are checked against the inspected
remote tree after that reversal. The 1,021-file staged baseline changes only
in those two sources and the contract log. The builder is idempotent.

Not run: Unity import/compilation, real fall recovery, native review action,
audible sound, particle placement, integration playthrough or profiling. No
Unity Editor is connected. These checks do not establish submission readiness.

Evidence: `Jehad_V1.23.validation.json`. Commands: `Tools/Jehad/V1_23/README.md`.
Next: inspect recovery sound/particles in Unity and record any remaining world,
menu or audio defects from an actual playthrough.
