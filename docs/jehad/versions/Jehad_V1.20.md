# Jehad V1.20 — reveal treasure after the lid clears

Open **`Assets/_Project/Scenes/Jehad/Jehad_V1.20.unity`** in Unity 6000.3.27f1.

## Behavior

The two secret-chest rewards previously became visible/collectible immediately
when opening began. This scene keeps each reward's renderer and pickup trigger
disabled until the existing lid animation reaches 95% progress, roughly half
a second after opening at normal speed. The original controller updates the
lid pose before releasing its reward gate.

The original E interaction still opens a chest once and plays its existing
opening sound/particles. The existing HiddenRelicV12 remains the only pickup
owner. No additional collectible, currency, reward event or objective is added;
the total remains 16 relics, including the two chest rewards.

Lid movement uses the existing scaled delta time, so a paused opening does not
advance to the reveal threshold. Main-area/boss-area retries preserve the
existing open/collected state and lid progress. New Game closes chests, resets
their animation and hides their rewards. Collected rewards cannot reappear
through the presentation gate. Sparkles continue to follow the original
pickup-collider availability guard.

## Integration and earlier versions

SecretChestV15 gains an optional, nonserialized reveal-after-lid flag, a read-only
property and a setter. It delegates the visibility condition to the pure
ChestRevealPolicyV120 and releases a waiting trigger when the actual animation
clears the threshold. The flag defaults false, and the opt-out policy is
equivalent to the original open-and-uncollected rule for all progress values.

ChestRevealGateV120 opts only the new scene's two authored chests into the gate
and restores their prior option values on disable. It does not write renderers,
colliders, collection flags or opening state itself. It has no per-frame loop;
the original chest controller remains the renderer/trigger owner.

WorldPolishReviewV120 retains the original asset/pool/chest acceptance checks,
adding locked-opening, paused-opening and revealed-before-collection checks.
It teleports directly onto the revealed reward for its existing physics pickup
test. The new scene rebinds that EditorOnly component and adds one runtime
adapter to an existing root. No scene root or earlier component field changes.

Earlier scenes lack the adapter and keep immediate legacy reveal. Release
scene, build settings, existing assets, audio, UI and original gameplay owners
are preserved. Production player/ability integration is still outstanding in
this movement-only review-player scene.

## Unity review

1. Approach both chests and press E. Inspect the lid, opening cue/burst and
   reward visibility. The reward should remain hidden/locked while the lid
   starts moving, then appear with its existing pickup/sparkle behavior once
   the lid is nearly fully open.
2. Open while standing close to the reward location. Confirm there is no
   collection before the lid clears and exactly one collection afterward.
   Verify the HUD total and discovery/sound feedback remain correct.
3. Pause immediately after opening, wait, then resume. Confirm the lid/reward
   do not advance through the pause. Retry during opening and after collection;
   inspect the original persistence rules. Start New Game and check both
   closed chests and reset order again.
4. In Play mode select **Jehad V1.20 world polish acceptance harness** and run
   **World polish → Run chest and asset checks (resets run)**. It retains the
   six-track/SFX/pool/16-relic checks and tests both real chest triggers, opening
   guards, paused reveal, an open uncollected reward, collection and both reset
   orders. It resets the run and returns to title. It does not prove production
   player movement, visual clearance or final gameplay balance.
5. Run the inherited sparkle, audio, UI, Guardian and flow actions separately.
   Inspect a standalone development build for animation/trigger timing, sound
   synchronization and frame cost. Test disabling the gate adapter only as an
   intentional opt-out: it restores the original immediate reveal option.

## Verification

**Passed:** 28 new assertions compile the actual reveal policy alongside the
original chest/reset policy and CoreTypes under C# 9.0/.NET 8, warnings as
errors. The ten existing suites pass 8,709 assertions, for **8,737 total**.

Static checks cover three new C# syntax trees, the extended chest source,
129 scene YAML documents, the same two chest targets, scaled animation usage
and original reward ownership. Reversing the adapter, review binding and
version labels reproduces V1.19 exactly. Reversing the scoped optional-gate
edits reproduces the verified original chest source. The inherited world/asset
acceptance code is preserved outside the reveal checks/pickup teleport.

The builder is idempotent. The 940-file staged baseline changes only in
SecretChestV15.cs and the contract log. Publication verifies all earlier remote
blobs outside those files; earlier scenes are preserved.

**Not run:** Unity compilation/import, actual lid animation, trigger activation,
pause/retry integration, physics collection, sparkle/audio synchronization,
acceptance actions or profiling. No Unity Editor is connected. Model/static
checks do not establish native behavior or final-submission readiness.

Evidence: `Jehad_V1.20.validation.json`. Commands: `Tools/Jehad/V1_20/README.md`.
Next: review both chests in Unity, especially pausing mid-opening and collecting
while standing close to the lid.
