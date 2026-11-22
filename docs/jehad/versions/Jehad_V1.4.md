# Jehad V1.4 — main-world enemy integration

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.4.unity` in Unity **6000.3.27f1**.
This checkpoint connects Muhammad's existing enemies and replenishing spawners
to the expanded valley. It preserves V1.0–V1.3 scenes/assets, the release scene,
build settings, Core APIs, and teammate-owned player/combat/AI implementations.
The V1.3 menus, retry service, health relay, Guardian, Heart and 16 relics are reused.

## What to review

- Six enemy sources: Sword/Bow/Fire near the entry, then the same three choices
  along the final approach before the arena. Color markers identify each source.
- The original Blade Beetle, Thorn Archer and Emberling prefabs provide their
  real health, senses, melee/ranged attacks and inhale contracts. No copied AI
  implementation or direct power pickup has been introduced.
- The existing EnemySpawner recycles each enemy after capture/defeat, with a
  six-second delay and a 12 m player-distance requirement. All six sources use
  its shared cap of six. Defeating an enemy never grants its power.
- Main-world navigation builds once on scene startup from Environment-layer
  physics colliders, using the largest radius/height among the three prefabs.
  Decorative grass/foliage rendering is not collected as navigation geometry.
- The main build excludes a 34 × 34 m box around the arena. V1.3 still builds
  the Guardian's separate larger-radius arena surface; the terrain beneath it
  cannot add a competing main-world surface inside the arena.
- Source instances are created only after navigation and all six spawn samples
  succeed. A setup error logs clearly and leaves population absent rather than
  starting enemies off the NavMesh. Spawn anchors may snap at most 1.5 m.

| Source | Authored position before navigation sampling |
|---|---|
| Entry Sword | (10, 2.006, -88) |
| Entry Bow | (-8, 4.016, -76) |
| Entry Fire | (10, 2.850, -68) |
| Approach Sword | (30, 1.810, 70) |
| Approach Bow | (40, 1.727, 76) |
| Approach Fire | (50, 1.636, 76) |

## State and reset ownership

`EnemyPopulationV14` creates source instances, then listens to the existing
StateChanged, RetryRequested and BossEncounterStarted events. Spawner GameObjects
remain active/discoverable; disabling their timer component also releases its
shared active count until play resumes. Normal brains are disabled and agents
stopped during menus, death and completion. Ongoing inhale/attack work is
cancelled when suspending. During ordinary play the brain retains ownership of
agent stopping, pathing, attack windows and capture behavior.

Entering the arena freezes normal enemies and respawn timers for the rest of
that run, and clears old projectiles so they cannot join the Guardian fight.
Boss retry preserves this isolation. MainArea/NewGame resets re-arm population;
the existing RetryService resets each owned enemy through its spawner once.
Pause/resume does not reset enemy health or restart respawn timers. No new
GameEvents are raised and no extra collectible-reset behavior is added.

## Player integration remains pending

At the inspected main commit, Tariq's production player is absent. The
`feature/2-player-movement` branch contains motor/input/camera work, but no
inhale or ability holder. That branch was inspected and left untouched.

V1.4 therefore still uses the clearly named **Testing movement capsule**. You
can walk/sprint/jump, encounter real attacks, die and retry, but cannot naturally
inhale enemies or attack the boss yet. This checkpoint is not a complete combat
build or a claim of final-submission readiness. Enemy visuals remain the team's
existing first-pass prefabs.

When the production player is ready, use the binding instructions in
`Jehad_V1.3.md`, remove the review controller, and run the complete inhale/attack
acceptance checks. Check entering the arena without a held power as part of
that integration so a player cannot become trapped in an unwinnable fight.
The existing V1.3 flow harness can exercise boss defeat/Heart completion meanwhile.

## Unity acceptance

In Play mode select **Jehad V1.4 population acceptance harness**, then open the
PopulationReviewV14 component context menu and choose **Population review →
Run acceptance checks (resets run)**. Run one harness at a time. This discards
the current session, takes about nine seconds, and returns to Title afterward.

The supplied harness checks ready navigation, all six agents, complete NavMesh
paths from entry, two sources for each power, the actual EnemyBrain capture
protocol, reuse of the same enemy, blocked respawn near the player, successful
respawn after moving away, real Health defeat, pause immobility, main retry,
arena-trigger isolation, boss retry, and a fresh run. Its capture step calls
the enemy contract directly; it does not verify production-player line of sight,
pull timing or ability replacement. Its EditorOnly root has no gameplay children
and is stripped from normal scene builds. Actions are Editor/development only.

Also manually check the Console, source/terrain placement, enemy chase around
trees and the boss approach, repeated deaths, pause during an attack, all 16
relics, and the V1.3 flow harness. Record the runtime build time exposed by
MainNavigationV14, then profile a standalone 1080p build on the target PC.
After player integration, record Sword-only, Bow-only and Fire-only boss wins.

## Evidence and remaining checks

Static verification passed for the three new C# syntax trees and 122 scene/source
prefab YAML documents, local/cross-asset references, source prefab setup, the
arena exclusion, metadata, one player relay, and preservation of the 460 local
baseline files except the documented contract-log row. A terrain graph reaches
all six sources; native-box checks cover 407 colliders and give about 3 m or more
of horizontal source clearance. This is conservative geometry analysis, not
a Unity navigation bake result.

The inherited plain C# session-policy regression still passes **445 assertions**
under C# 9.0/.NET 8. It tests V1.3 state rules, not the new Unity components.

**Not run here:** Unity compilation/import, runtime NavMesh builds/path queries,
agents, physics, the supplied population/flow harnesses, UI/audio behavior,
production combat, camera review or performance. There is no connected Unity
Editor in this environment. Runtime engine checks are required before release.

No new third-party content was imported. Rebuild/check commands are in
`Tools/Jehad/V1_4/README.md`; the machine-readable evidence is
`Jehad_V1.4.validation.json` beside this note.

Next checkpoint after review: connect the production inhale/ability player when
available, verify all three boss victories, then address audio, hit/capture
feedback, camera and measured performance findings.
