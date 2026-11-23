# Jehad V1.5 — environment music, feedback and hidden treasure

Open **`Assets/_Project/Scenes/Jehad/Jehad_V1.5.unity`** in Unity 6000.3.27f1.
This checkpoint concentrates on Jehad's world/audio/presentation work. The earlier
scenes and reusable landscape remain preserved. Player, combat and AI implementations,
Core signatures, release scene and build settings are unchanged.

## Environment music and ambience

Six original music arrangements now replace the empty music binding:

| Place | Music character | Where to review |
|---|---|---|
| Meadow / title | Light plucked melody and soft accompaniment | Entry clearing |
| Amber woodland | Warm flute melody | Western autumn forest |
| Lake | Higher bell/chime arrangement | Lake shores and island |
| Pine uplands | Lower sustained melody and plucked accompaniment | Northern woodland |
| Alpine mountain | Sparse, airy bells | Above Y = 25 on the mountain |
| Guardian | Faster bass/percussion pulse | Actual boss encounter |

Exploration loops are 32 seconds; the Guardian loop is about 26.67 seconds.
Region changes require a stable 0.65-second entry and use continuous, normalized
crossfade weights. Fast boundary reversals continue from the current mix. The
boss has immediate region priority. Title/completion return to the meadow theme;
Pause and Death duck music. Music uses unscaled time, so menus remain audible.

Three additional original loops provide spatial lake/river water, low forest
air/bird notes and alpine air. Water follows the nearest point on the watercourse
or lake surface. Ambience fades away during menus and the Guardian encounter.
Music and ambience use streaming import settings; short SFX use PCM/preload.

Esc opens the existing **separate Music and Sound Effects sliders**. The old
MenuAudioV13 component remains referenced for its volume setters and UI clicks,
but its automatic pickup/win subscriptions are disabled only in this scene.
WorldAudioV15 supplies the new pickup/restoration sounds and reads both volumes,
preventing duplicate automatic cues. No disk settings/save system was added.

## Sound effects, particles and readable encounters

There are **23 original one-shot assets**: relic, chest, secret, capture, hit,
defeat, hurt, death, restoration, enemy warning, two Guardian warnings, two
Guardian attacks, power replacement, grass/stone/water steps, jump, landing,
Sword, Bow and Fire. Eight reusable spatial voices bound world SFX; session
stingers and menu clicks have their own 2D sources.

- Collecting a loose or chest relic produces a golden burst, a distinct cue and
  a short discovery toast. Existing HiddenRelicV12 still owns the award once.
- Opening a chest produces its own reveal effect/sound. Entering the mushroom
  circle or hidden spring produces a once-per-run secret discovery cue/toast.
- Nearby actors receive brief property-block hit flashes without duplicating
  their materials; actual Health and enemy removal events drive hit, defeat and
  capture effects. Enemy Attack-state entry supplies a warning sound.
- Player displacement/grounding drives bounded grass/stone/water footsteps,
  jump/land sounds and small dust bursts. Teleports/retries suppress step spam.
- Guardian public Telegraph/Attack state drives distinct warning/strike audio,
  a ground ring at its **actual SlamRadius**, and a ranged direction guide.
  These are visual/audio feedback and do not change attack timing or damage.
- A restrained local field of motes changes tint with the environment. The
  summit uses pale snow-like particles; amber woodland uses warm flecks.
- Heart restoration has its own sound and an unscaled mint burst so the effect
  can finish behind the completion menu.

Particles use an original URP unlit sprite shader and shared materials. Twelve
reused burst systems allow 64 particles each; one local ambient system allows
64 more: **832 configured maximum particles**. No new real-time lights, camera
shake, VFX package or external texture was imported. Actual frame time remains
to be measured in Unity.

## Two optional secret chests, still 16 relics

| Chest | Existing relic used | Location |
|---|---|---|
| The Forgotten Garden | RuinedGarden | (-65, terrain height ≈ 2.04, -7) |
| The Summit Cache | SummitSanctuary | (-56, 52, 58) |

The reusable original chest has a timber body, gold fittings, a curved native
lid mesh and curved gold bands. Its hinge opens over about half a second. The
body has collision and a carving navigation obstacle.

Approach and press **E** to open, then approach the revealed floating relic.
These two rewards come from the existing 16 pickups: **14 loose + 2 chest
rewards**, with the same IDs and HUD total. No extra currency, required hunt or
boss gate was added. The existing mushroom circle and hidden spring are also
recognized as optional discovery spots.

Closed chests disable their reward's collider and renderers. Main-area and
boss-area retries preserve open chests and collected relics; New Game closes
chests, clears discovery flags and resets all relics. The gate does not depend
on reset ordering: HiddenRelicV12 resets its GameObject but never re-enables the
collider that the chest owns. Repeated opening is guarded before feedback fires.

## Production-player hooks

The production player is still absent on the inspected main branch. The Testing
movement capsule remains for world review; this checkpoint does not implement
Tariq's inhale/ability holder. World/health/enemy/boss presentation is connected
now, while successful player attacks need the real attack components connected.

After the production player is merged, bind **SwordAttack._onSwing** to
`WorldFeedbackV15.SwordUsed()` and **ProjectileAttack._onFired** to
`WorldFeedbackV15.RangedUsed()`. These existing UnityEvents fire only on an
accepted attack, avoiding sounds on failed clicks/cooldowns. Alternatively use
the provided attack SFX through AbilityDefinition.UseSfx + the attack AudioSource;
choose one sound path per attack to prevent doubles. Refresh the coordinator's
player/health/controller references through the existing V1.3 binding. No new
shared event or grant-on-defeat rule is introduced.

## Unity review and acceptance

1. Import/open V1.5 and check the Console for script, shader, media and reference
   errors. Verify that the title music plays with a single AudioListener.
2. Start a new game and walk to each music region. Listen through a full loop,
   cross boundaries repeatedly and check both volume sliders, including zero.
   Pause/resume, die/retry, return to title, and trigger the Guardian encounter.
3. Collect a loose relic twice; confirm one count, sound and burst. Visit both
   chests: the reward must be hidden/locked before E, the lid must open smoothly,
   and approaching the revealed relic must count once. Check area retries,
   New Game and the two optional discovery zones.
4. Select **Jehad V1.5 world polish acceptance harness** in Play mode and use
   **World polish → Run chest and asset checks (resets run)**. It discards the
   current session, checks bank/pool setup, tests locked rewards against actual
   physics, pause/repeated-open guards, both reset orders, and the unchanged
   16-relic total. It returns to Title. Run one harness at a time.
5. Inspect hit/capture bursts, footstep surfaces, Guardian ground warnings and
   Heart restoration. Re-run the V1.3 flow and V1.4 population harnesses. Record
   target-PC 1080p frame time/audio voice load before calling the scene release-ready.

The new harness is on an EditorOnly root with no gameplay children. Its context
actions compile only for Editor/development builds; the root is stripped from
normal scene builds. It tests the enemy/world contracts, not production inhale
line of sight, player attacks or combat balance.

## Evidence and limits

**Passed outside Unity:** 6,335 assertions compiling the actual region/mix and
chest policy sources with actual CoreTypes under C# 9.0/.NET 8. The checks include
region priority, boundary jitter, rapid crossfade reversal, normalized finite
weights, pause/distance/open guards and chest persistence across retry scopes.

Static checks cover all 12 new C# syntax trees, 178 scene/prefab/bank/material/mesh
YAML documents, references, the two original meshes' buffers/normals/winding/bounds,
the original 16 IDs, reward/lid bindings, and previous-file preservation.
All 32 audio files decode correctly, have finite/non-silent/non-clipped samples,
match declared durations/checksums, and pass a loop-endpoint outlier check. All
six music tracks have distinct decoded audio. The total authored audio is about
**1.29 MB**. This is signal analysis, not a listening or Unity playback review.

**Not run here:** Unity compilation, shader compilation, asset import/playback,
particle appearance, chest physics/animation, NavMesh carving, harness execution,
volume/UI playtesting, production combat or performance. No Unity Editor is
connected. The new feedback must be observed/heard in the Editor before release.

No third-party content was added. Original audio provenance is recorded in
ASSET_CREDITS.md and `Jehad_V1.5.audio.json`; the generator contains all original
arrangements/waveforms. These small version-folder audio files have scoped Git
attribute exceptions for direct storage; root LFS rules for other assets are
unchanged, and a fresh clone needs no unavailable LFS objects for this pack.

Reproduction: `Tools/Jehad/V1_5/README.md`. Machine-readable validation:
`Jehad_V1.5.validation.json`. Next after review: correct Unity findings, tune
levels/particle visibility, then profile and optimize Jehad's world presentation.
