# Jehad V1.13 — nearby collectible sparkles

Open **`Assets/_Project/Scenes/Jehad/Jehad_V1.13.unity`** in Unity 6000.3.27f1.

## Changes to review

- Nearby available relics gain small golden sparkles. A relic enters selection
  within 10m and may remain selected out to 12m, reducing boundary flicker.
  Emission opacity fades across the outer four metres.
- The system selects at most six nearest eligible relics. Retained relics keep
  their emitter slot when distance ordering changes. Six reusable systems,
  eight particles each, cap the added budget at **48 particles**.
- Eligibility checks the original pickup's collected/active flags and collider
  gate, camera viewport and an Environment-layer linecast from player eye
  height. View/cover selection refreshes every 0.12 unscaled seconds; slot
  position and pickup availability update each LateUpdate.
- Closed-chest rewards stay excluded because their original reward colliders
  are disabled. Collected/inactive pickups stop emitting and clear their slot.
  The existing depth-tested V1.5 particle material is reused.
- Pause, death, title, completion, retries and component disabling clear the
  emitters. Resume builds fresh local effects. World sparkles are independent
  of the existing UI hints setting.

This is local visual polish. It adds no collectible count, map marker, light,
sound source, collider, exploration objective or reward. It does not direct
players to distant secrets. Existing chest and pickup controllers remain the
only owners of opening, collecting and resetting rewards.

## Integration

RelicGlintsV113 is added to the existing world presentation root. Its authored
references point to SessionFlowV13, RelicCounterV13, the existing Camera and
WorldParticlesV15's material. It caches the existing pickup list and creates
its bounded effect pool once. Local particle simulation follows each bobbing
pickup without scattering particles along its motion.

RelicGlintReviewV113 is added to the existing EditorOnly acceptance root.
Removing these two components, their component-list entries and version labels
reproduces V1.12 exactly. All previous scenes/scripts, the release scene, build
settings, packages, AI/player/combat, world layout, music/SFX and rewards remain
unchanged. The staged baseline changes only in the contract log. Publication
verifies preservation of all earlier remote blobs outside that log.

The scene still uses the movement-only review capsule. Production player and
ability integration remain outstanding; this checkpoint does not establish
final-submission readiness or validate player combat.

## Unity review

1. Start a run and approach ordinary relics. Inspect sparkle size, brightness,
   local bobbing and visibility against grass, rock, water and dark backgrounds.
   Walk across 10m and 12m repeatedly and rotate the camera across the viewport
   edge. Check that fading and the short refresh interval look natural.
2. Put terrain/solid Environment cover between player and relic. Check that
   occluded relics lose their sparkles, then regain them when exposed. Review
   camera obstruction and low-angle views too: the linecast is player based,
   and actual depth rendering remains part of this check.
3. Approach both closed chests. Confirm their concealed rewards have no
   sparkles. Open each through the existing E interaction; an uncollected
   revealed reward may sparkle when visible and nearby. Collect it once and
   verify its sparkle and original collection count update correctly.
4. Pause/resume, die, retry the main area and boss arena, return to title,
   complete a run and start New Game. Check effect clearing/rebuilding while
   respecting the original retry policy: area retries retain collected relics
   and opened chests; New Game resets them. Confirm original audio cues still
   play once and the discovery queue/guide behave as before.
5. In Play mode select **Jehad V1.13 UI acceptance harness** and run **Relic
   review → Run sparkle checks (resets run)**. It checks the pool/relic count,
   both closed reward gates, one nearby ordinary relic, pause/resume, real
   trigger collection, area retry, title and New Game. It teleports the review
   player and resets the run. If the nearby-relic assertion fails, inspect
   actual camera framing and cover rather than bypassing those guards.
6. Run other acceptance actions separately. Inspect the standalone development
   build with Unity's profiler, especially emitter reuse, particle rendering,
   linecast cost and frame allocations. The fixed particle budget is a source
   constraint, not a measured performance result.

## Verification

**Passed:** 61 assertions compile the actual selection source under C# 9.0/
.NET 8 with warnings as errors; 2,163 earlier assertions pass, **2,224 total**.
Static checks cover three new C# syntax trees, 121 scene YAML documents,
serialized camera/counter/chest/material references, prior-scene preservation,
eligibility/lifecycle guards and absence of progression/reward calls. The
builder is idempotent. The 803-file staged baseline permits only the contract
log to change.

**Not run:** Unity compilation/import, actual particle rendering, camera/cover
queries, pickup trigger execution, audio, supplied harnesses and profiling.
No Unity Editor is connected. The pure C# tests validate selection logic;
they do not validate native Unity behavior.

Evidence: `Jehad_V1.13.validation.json`. Commands: `Tools/Jehad/V1_13/README.md`.
Next: inspect this checkpoint in Unity and use the findings for the next polish
step, particularly sparkle visibility, clutter and frame cost.
