# Jehad V1.15 — immediate health and recent-damage trails

Open **`Assets/_Project/Scenes/Jehad/Jehad_V1.15.unity`** in Unity 6000.3.27f1.

## Behavior

- Player and Guardian health fills update immediately from their existing
  health events. The numeric values and current fill agree after each event.
- A gold strip behind each fill shows recent health loss. It holds for 0.18
  seconds, then shrinks at 1.4 full-bar widths per second, settling within
  about 0.9 seconds even after a full-bar loss. Further hits extend the brief
  hold without discarding the existing visible loss.
- At positive player health at or below one quarter, the heading becomes
  **LOW VITALITY**, and its heading/fill use coral. The warning is static and
  includes words rather than relying solely on color.
- Healing, changed maximum HP, menu/state transitions and retries snap away
  stale loss animation. Guardian visibility changes also clear its old trail.
- **Reduce UI motion** removes the trailing animation immediately. High
  contrast makes the trail opaque. Neither option removes the current value
  or low-health wording.

No sound, full-screen flash, camera effect or input control is added. Existing
health, invulnerability, damage, healing, death, boss events and retry owners
remain unchanged.

## Integration

UiHudV115 replaces only the HUD helper constructed by FlowMenuV115. The menu,
screen helper, cancel handler and two typed review harnesses otherwise retain
V1.14 behavior. The new scene changes three script bindings and version labels;
it adds no serialized field, scene root or gameplay component.

HealthBarV115 is a pure presentation model shared by player and Guardian bars.
It normalizes integer inputs, updates current HP immediately and retains only
the visual loss trail. Duplicate health messages do not restart that trail.
The low-health threshold uses integer arithmetic that avoids overflow. Invalid
elapsed times cannot poison the animation.

Two non-interactive graphics are created behind the existing fills, using the
same UI material and existing track dimensions. Unchanged widths skip the
resize call. Existing HUD clusters, safe-area geometry, power display, relic
display, objective, prompts, aim guide and pause shortcut are preserved.

V1.14 preference storage, menu navigation, display preview, all earlier world/
audio/presentation and original gameplay owners are reused. Earlier scenes,
release scene, build settings, packages and imported assets remain unchanged.
The scene still uses the movement-only review capsule; production player/
ability integration remains outstanding.

## Unity review

1. Start a run and take damage through the actual Health component. Confirm
   the mint/coral fill and numeric HP change together, with a short gold loss
   strip behind the fill. Inspect small hits, consecutive hits, lethal damage,
   healing and retry. No trail should replay when returning from a menu.
2. With the review player's five maximum HP, reach one HP. Check **LOW VITALITY**
   wording, coral contrast and text fit. Heal or retry and confirm the ordinary
   heading returns. Test all HUD scales and supported screen/safe-area shapes.
3. Toggle reduced UI motion and high contrast. With reduced motion enabled,
   damage should update both layers instantly. High contrast should keep a
   distinct loss strip without changing HP or the warning threshold.
4. Enter the Guardian encounter, damage the real boss, then retry. Check
   immediate boss fill, gold strip timing, numeric agreement, visibility on
   boss defeat and a clean full-health bar on retry. Existing attack warnings,
   discovery messages and Heart completion should behave as before.
5. In Play mode select **Jehad V1.15 UI acceptance harness** and run **UI review
   → Run vitality HUD checks (resets run)**. It uses accepted test damage on
   the review player's real Health, inspects immediate fill/trail widths and
   low-health wording, checks pause/resume/retry, and tests reduced motion.
   It temporarily changes the motion option and restores/persists the original
   preferences in its finally block. It returns to title and resets the run;
   it does not validate production player attacks or Guardian rendering.
6. Run inherited preference, UI/navigation, notification, flow, Guardian and
   world checks separately. Profile UI rebuild/frame cost during damage and
   while idle in a standalone development build. Inspect the final visuals
   and input behavior before treating this checkpoint as accepted.

## Verification

**Passed:** 41 assertions compile the actual new health presentation model
under C# 9.0/.NET 8 with warnings as errors, including bounded mixed sequences
with 10,000 observations. The seven earlier suites pass 2,303 assertions,
for **2,344 total**.

Static checks cover seven C# syntax trees, 122 scene YAML documents, typed
menu/harness and preference references, loss-strip layering, reduced-motion/
contrast guards and absence of damage/reward/input authority in the health
presentation. Reversing the three script bindings and version labels reproduces
V1.14 exactly. Inherited menu/harness behavior and non-health HUD methods are
checked against their prior sources. The builder is idempotent. The 847-file
staged baseline permits only the contract log to change; publication verifies
all earlier remote blobs outside that log.

**Not run:** Unity compilation/import, actual Health event delivery, UI mesh
generation/rendering, text fit, input, context actions and profiling. No Unity
Editor is connected. Pure model tests do not establish visual quality, combat
integration or final-submission readiness.

Evidence: `Jehad_V1.15.validation.json`. Commands: `Tools/Jehad/V1_15/README.md`.
Next: inspect both bars in Unity, including low-health text and reduced-motion
behavior, then tune from the playtest findings.
