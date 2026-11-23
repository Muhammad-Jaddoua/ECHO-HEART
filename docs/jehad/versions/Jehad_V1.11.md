# Jehad V1.11 — clearer Guardian warnings and protected boss audio

Open **`Assets/_Project/Scenes/Jehad/Jehad_V1.11.unity`** in Unity 6000.3.27f1.

## Changes to review

- **Slam warning:** a fixed boundary shows the configured SlamRadius from
  the start of the telegraph. A gold arc fills around it using the real
  BossBrain.TelegraphProgress. The impact gets a short expanding ring accent
  that ends after 0.35 seconds. These visuals never apply damage.
- **Ranged warning:** the guide uses the runtime prefab's actual Muzzle
  transform, current player aim point and configured ProjectileRange, replacing
  the fixed seven-metre line. An environment ray shortens the projected guide
  at cover. A growing inner segment communicates preparation progress.
  This is a ground projection of the centerline; the projectile's radius and
  collision queries can produce different edge collisions.
- **Dedicated audio:** warning and impact/defeat cues have two fixed spatial
  AudioSources outside the eight-voice world SFX pool. Footsteps and pickups
  cannot overwrite these channels through the world's round-robin allocation.
  They still use the existing audio bank and real SFX volume control.
- **State handling:** cues fire on presentation state edges. Pause hides the
  warning, pauses its audio and preserves its state; Resume continues without
  replaying the warning. Retry, death, Title and completion clear effects and
  dedicated voices. Slam impact timing follows scaled gameplay time.
- Ranged impact sound/particles originate at the muzzle. Boss hit flashes
  retain the existing ActorFeedbackV15, and defeat retains its existing
  sound/capture-particle combination through the new presentation owner.

No new attack, boss phase, damage rule, health value, AI timer, power or area
is introduced. The original boss prefab and definition remain unchanged.

## Presentation ownership

GuardianFeedbackV111 is added to the existing scene presentation root. The
scene clears WorldFeedbackV15's optional arena reference, delegating only its
boss binding/death-feedback path to this presenter. Player/enemy/relic/secret/
footstep feedback continues through WorldFeedbackV15. Its optional toast
reference remains cleared as in V1.10.

For the bound runtime Guardian, the new presenter temporarily disables the
prefab's presentation-only BossTelegraphVisual so its growing disc/body pulse
does not duplicate the new warning. It restores that component's original
enabled state if the new presenter is disabled. No boss-simulation component
is disabled by this presenter. BossBrain remains the sole combat/event owner;
no HealthEventRelay is added to the boss.

The authored muzzle child path is serialized as `Muzzle` and resolved once
under the bound boss. Static validation checks that it is the actual direct
child referenced by BossBrain._muzzle. Geometry uses world coordinates, so
the slam boundary uses the configured damage radius directly.

Menus/HUD/settings, V1.10 notification queue, chests, relics, world music,
population, Heart, retry flow, earlier scenes and release/build settings are
preserved. Production player/ability integration is still outstanding; the
scene uses the movement-only review player.

## Unity review

1. Enter the Guardian arena. Stay near it for Slam and farther away for Ranged.
   Inspect the full slam boundary, countdown arc, impact accent, aimed guide
   and charge segment. Check visibility from the normal gameplay camera.
2. Move behind arena cover during a ranged telegraph. Compare the guide with
   actual projectiles, including grazing edges. Inspect the authored muzzle
   position and ground projection. Tune rendering only after seeing it in Unity.
3. While warnings play, trigger other world sounds. Listen for clear warning,
   shot, slam and defeat cues at different camera distances. Test SFX mute and
   both volume sliders. These two sources are protected from the world pool,
   but audibility/mixing and Unity's global voice budget still need auditioning.
4. Pause and resume during each telegraph and slam impact. Retry during the
   warning, die, return to Title and start a new run. Confirm there are no
   repeated cues, stale rings or growing source/component counts.
5. In Play mode select **Jehad V1.11 UI acceptance harness** and run **Guardian
   review → Run presentation checks (resets run)**. It enters the real arena,
   checks presentation owners/muzzle/two voices, reaches a real slam warning,
   measures its radius, checks pause/resume cue count and retries the boss.
   It supplies no player attacks and does not prove combat balance.
6. Run the existing UI/notification, V1.9 flow and V1.5 world-polish harness
   actions separately. Verify the boss can still be defeated with each power
   once the production player is integrated, and that the Heart completes once.

## Verification

**Passed:** 51 compiled assertions against the actual GuardianCuePolicyV111
cover distinct warning/impact edges, repeated frames, paused/resumed state,
retry re-arming, late binding, ring/arc radius and vertex bounds, range/cover
clamping and impact expiry. The existing 111 notification, 335 layout and
1,586 menu/focus/settings regression assertions also pass. Tests use C# 9.0/
.NET 8 with warnings as errors and no Unity API stubs or NuGet packages.

Static checks cover three new C# syntax trees, 117 scene YAML documents,
GUID/serialization/component references, the original boss muzzle and empty
cue hooks, source-level damage/range conventions and passive presentation
guards. Removing two added components, their component-list entries, the
delegated arena reference and version labels reproduces V1.10 exactly. No new
scene root is added. The builder is idempotent; the 767-file staged baseline
changes only in the contract log. Publication also verifies all prior remote
blobs outside that log.

**Not run:** Unity compilation/import, actual line rendering, audio playback/
mixing, physics/cover projection, input, supplied harnesses and profiling.
No Unity Editor is connected. The tests above do not establish engine behavior
or final-submission readiness.

Evidence: `Jehad_V1.11.validation.json`. Commands: `Tools/Jehad/V1_11/README.md`.
Next: use the real Guardian playtest to tune visibility, sound levels and
presentation timing without changing combat balance.
