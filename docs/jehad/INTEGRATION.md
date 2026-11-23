# Integration handoff for Tariq and Mohammed

**Start here:** [Jehad_V1.27.unity](../../Assets/_Project/Scenes/Jehad/Jehad_V1.27.unity) in **Unity 6000.3.27f1, URP**.  
**Source reviewed:** main commit `004fd3fc8cf02f7edb3e18c0dd838ded3e4c776b`, 23 November 2026.  
**Status:** Jehad's world/UI/audio integration candidate. The scene still uses a movement-only review capsule; production-player integration and native Unity acceptance remain pending.

The target is one main semi-open world and one boss area, with **Sword, Bow and Fire**. Jehad's checkpoint supplies the world, 16 relics, two chests, secrets, flow, menus, music, SFX and VFX. This file explains how Tariq's player and Mohammed's combat/AI connect to those systems. It does not mark the integrated game or final build complete.

## Integration order

1. **Tariq:** make a working copy of V1.27 for integration. Start with its existing world and managers, then replace the review capsule with the production player. Keep the checkpoint available for comparison.
2. **Tariq + Mohammed:** connect the ability holder, inhale and attack runner; agree on one owner for each event and effect using the rules below.
3. **Mohammed:** verify the three enemy types, runtime navigation/population and Guardian encounter in the integrated scene.
4. **Both:** run the joint checklist below and the full [native acceptance route](ACCEPTANCE.md). Fix and retest failures before signing off.
5. **Tariq, as scene/build integrator:** transfer the accepted result to `Assets/_Project/Scenes/Echoheart.unity`, check its Build Profile and produce the release candidate. V1.27 has not replaced that release scene or its build settings.

Keep actual gameplay objects outside roots tagged **EditorOnly**. The acceptance harnesses are stripped from builds; never parent the player, managers, enemies or world content under them. Do not copy older version managers into the integrated scene.

## Where to find the bindings in V1.27

| Scene object | Components / purpose |
|---|---|
| `Jehad V1.27 - Session and menus` | `SessionFlowV13`, `PlayerFlowBindingV13`, `FlowMenuV117`: state, retry/player boundary and current interface |
| `Jehad V1.27 UI Explorer` | Movement-only review capsule to replace after rebinding |
| `Jehad V1.5 - Music and world presentation` | `WorldAudioV15`, `WorldParticlesV15`, `WorldFeedbackV15`, `RelicGlintsV113`; the older object name is intentional |
| `Jehad V1.27 - Interface settings` | `TitleVistaV117` title-camera binding |
| `Guardian encounter volume` | `ArenaEncounterV13`: arena trigger, boss instance, gate and arena navigation |
| `Island Heart interaction` | `IslandHeartV13`: ending interaction after boss defeat |

## Tariq — player, camera, input and interface

### 1. Replace the capsule and rebind before entering Play

Use [PlayerFlowBindingV13](../../Assets/_Project/Scripts/World/Jehad/V1_3/PlayerFlowBindingV13.cs) as the connection point. Set these references in Edit mode: the binding caches its `CharacterController` in `Awake`, and presentation components also cache player data during startup.

| Component / Inspector field | Required assignment |
|---|---|
| `PlayerFlowBindingV13._player` | Production player root, with the `CharacterController` on this same object |
| `PlayerFlowBindingV13._health` | Actual player `Health`, configured for `Team.Player` |
| `PlayerFlowBindingV13._gameplayInput` | Behaviours that read movement, camera, attack and inhale input; flow disables them outside Playing |
| `PlayerFlowBindingV13._onRespawned` | Parameterless motor-velocity reset and camera snap/reset callbacks |
| `PlayerFlowBindingV13._clearAbilityRequested` | Production holder's clear method |
| `PlayerFlowBindingV13._equipAbilityRequested` | Production holder's dynamic `AbilityDefinition` equip method |
| `PlayerFlowBindingV13._publishReviewAbilityChanges` | **Off** when the real holder publishes `GameEvents.AbilityChanged` |
| `SessionFlowV13._camera` | Production gameplay camera transform; check `_lockCursorWhilePlaying` against the input setup |
| `WorldAudioV15._player`, `WorldParticlesV15._player` | Production player root |
| `ObjectiveGuideV112._camera`, `RelicGlintsV113._camera` | Production `Camera` component |
| `TitleVistaV117._camera` | Camera transform used by the title presentation; ensure the gameplay camera takes control in Playing |

- Remove/disable the old capsule and its collider, Health and relay after rebinding. Disabling only its movement leaves another potential enemy target or collectible collector.
- Keep one active render-camera `AudioListener` and one `EventSystem` with the intended UI input module.
- Keep health, ability storage and event subscriptions active while input is gated. If a combined script also owns that data, split its input handling or provide a gate instead of disabling the entire system. UI navigation must remain usable during pause/death/title.
- Preserve the menu's one-frame input-release shield on start/resume so a menu click does not immediately fire or inhale.
- `_onRespawned` runs after **every binding teleport, including fall recovery**. Reset motion/camera only; do not restore health, clear powers, reset collectibles or start another retry there.
- Recheck `WorldFeedbackV15` and airborne feedback against the production controller's grounded/velocity behavior. Check the hurt-effect anchor against the model; the current fallback uses the Health transform plus one metre.

The holder must publish `GameEvents.AbilityChanged` whenever its power changes, including clearing it. The flow observes this event to remember the current power. Wire flow's equip request into the holder; do not wire the holder's equip callback back into `PlayerFlowBindingV13.ApplyAbility`, which would recurse.

### 2. Use the existing attacks and successful-use hooks

The existing [AbilityAttackRunner](../../Assets/_Project/Scripts/Combat/AbilityAttackRunner.cs) exposes:

```csharp
bool accepted = runner.TryUse(currentAbility, attackOrigin, aimDirection, playerOwner);
```

Here `currentAbility` is an `AbilityDefinition`, `attackOrigin` a `Transform`, `aimDirection` a world-space `Vector3`, and `playerOwner` the owning `GameObject`. The holder supplies the selected definition; attack handlers own cooldown and damage. Populate `_handlerComponents` with the intended `IAbilityAttack` components, or use its child-component discovery when the list is empty. Configure handlers before `Awake`.

| Existing successful-use event | Jehad presentation callback |
|---|---|
| `SwordAttack._onSwing` | `WorldFeedbackV15.SwordUsed()` |
| `ProjectileAttack._onFired` | `WorldFeedbackV15.RangedUsed()` for Bow and Fire |

Both events are **parameterless UnityEvents** and run after an accepted use. Do not play success feedback on every raw click: cooldown-rejected input must not produce another swing/shot effect. `RangedUsed()` reads the current ability from the flow binding, so verify the holder's event wiring first.

**Agree on one primary owner per sound/effect.** The attack components can also play `AbilityDefinition.UseSfx` through their own AudioSources and instantiate `UseVfx`. Audit those settings with Mohammed before adding the world-feedback hooks, so one accepted attack does not play duplicate effects. Route any retained local AudioSources through the same volume/pause behavior; confirm that the menu SFX slider controls them.

### 3. Follow the enemy inhale lifecycle

Use the existing [IInhalable](../../Assets/_Project/Scripts/Core/IInhalable.cs) / [EnemyBrain](../../Assets/_Project/Scripts/AI/EnemyBrain.cs) contract:

1. Check `CanBeInhaled` before starting a new target.
2. Call `OnInhaleStart(inhaler)` and refresh it while pulling; repeated calls refresh the brain's inhale timeout. **Do not use `CanBeInhaled` as permission to continue the current pull:** it becomes false while the brain is already being inhaled.
3. On success, read `GrantedAbility`, call `OnCaptured()` once and equip the definition through the real holder.
4. On release, failure, pause, death, disable or retry, cancel the active pull with `OnInhaleCancel()` and clear the local target.

The enemy brain owns its NavMeshAgent suspension/reseating and capture removal. Do not independently toggle its agent or destroy the enemy in a second capture path. The Guardian is not inhalable.

### 4. Keep one health bridge and one HUD

- Put the player body on **Player layer 8** and ensure body colliders resolve to the intended parent Health.
- Keep exactly one Player-mode `HealthEventRelay` on the **same GameObject as player Health**; its implementation uses `GetComponent<Health>()`. Remove other publishers of the same health/death events.
- For the first merge, keep the checkpoint's `FlowMenuV117` / `UiHudV115` and disable any duplicate incoming HUD. This interface already owns health, power, collectibles, prompts, ranged guidance and the boss bar.
- `FlowMenuV117` constructs its HUD in `Awake`; **it has no `_showReviewHud` toggle**. Replacing the HUD later requires an explicit ownership/refactor change while preserving the menus, rather than stacking another HUD over it.

## Mohammed — combat, enemies, navigation and Guardian

### 1. Validate definitions and population in the integrated scene

- Keep only the intended **Sword, Bow and Fire** ability/enemy types. Verify the player-copy definitions and each enemy's granted ability, attack references, health and `Team.Enemy` configuration.
- Preserve [MainNavigationV14](../../Assets/_Project/Scripts/World/Jehad/V1_4/MainNavigationV14.cs) and [EnemyPopulationV14](../../Assets/_Project/Scripts/World/Jehad/V1_4/EnemyPopulationV14.cs). The checkpoint uses three enemy prefabs and six source entries, with a common agent type. The main runtime navigation excludes the arena; the arena builds its own navigation.
- Keep population source prefab roots **inactive** before instantiation. Population creates them after navigation is Ready; sources must sample within the configured 1.5 m radius. A navigation/source failure should be diagnosed rather than bypassed with partial population.
- Preserve the shared active-enemy cap of six and the `EnemySpawner` ownership of spawned enemies and their reset/respawn. Keep the population/navigation managers active so they can receive state transitions.
- Population gates source, brain and agent simulation outside gameplay and on arena entry, cancelling attacks/inhale when suspended. Preserve the brain's agent ownership during normal play and inhale.
- Do not overlay sandbox baked navigation, duplicate runtime builders, extra spawners or older managers onto the same area.

### 2. Check attacks, projectiles and masks

Verify Sword hitboxes and Bow/Fire projectile definitions with Tariq's actual origin and aim. Keep the intended projectile component on the prefab root for predictable ownership and cleanup; confirm owner/team filtering, collision and destruction on retry.

| Layer | Index |
|---|---|
| Player | 8 |
| Enemy | 9 |
| Boss | 10 |
| PlayerAttack | 11 |
| EnemyAttack | 12 |
| Environment | 13 |
| Collectible | 14 |
| Trigger | 15 |

Preserve these layer names/indices and their collision masks in the integrated project. Enemy/boss player searches use the Player layer and parent Health; test the production collider hierarchy. Review successful-use SFX/VFX ownership with Tariq as described above.

### 3. Keep the existing boss event owner and encounter lifecycle

Use [ArenaEncounterV13](../../Assets/_Project/Scripts/World/Jehad/V1_3/ArenaEncounterV13.cs) to own the arena trigger, boss instance, gate and retry activation. Verify its `_flow`, `_bossPrefab`, `_arenaRoot`, `_bossPoint` and `_gate` references after scene transfer. The boss prefab needs its real `Health`, `NavMeshAgent`, `BossDefinition` and attack/muzzle/projectile references.

- **`BossBrain` already publishes `BossEncounterStarted`, `BossHealthChanged` and `BossDefeated`. Do not add a Boss-mode `HealthEventRelay` or a second event bridge to that prefab.** Duplicate publishing can repeat HUD, music or ending transitions.
- `BeginEncounter()` is idempotent for an active encounter. A reset re-arms the boss, so arena retry must call it again through the encounter controller; merely re-enabling the object is insufficient.
- Arena entry must start the real encounter and gate the area. After retry, the Guardian must resume its actual AI, the boss bar/music must restart correctly and the player must retain the selected power.
- One real boss defeat unlocks the Heart, opens the gate and releases boss music. `IslandHeartV13` then requires the nearby interaction to finish the game once. Do not unlock it from a debug button in the release route.

## Shared flow and retry rules — preserve these behaviors

| Action | Expected behavior |
|---|---|
| Pause / resume | Preserve HP, power, relics and discoveries; suspend gameplay, retain UI navigation and avoid an input firing on resume |
| Main-area retry | Full player HP, no power, return to main entry; preserve collected relics, opened chests and discoveries |
| Boss-area retry | Full player and boss HP, preserve player power, re-arm the encounter; preserve collected relics/chests/discoveries |
| New Game | Reset power, relics, chests and discoveries for a fresh run |
| Fall recovery | Use the existing safe-position teleport/damage pipeline; motor/camera callback must not start a second reset |
| Guardian defeat → Heart | Defeat unlocks the Heart; a valid nearby interaction completes the run once |

Keep [RetryServiceV13](../../Assets/_Project/Scripts/World/Jehad/V1_3/RetryServiceV13.cs) as one reset transaction. It clears active projectiles, includes inactive resettable objects and skips directly resetting spawner-owned enemies because their spawner resets them. Do not add a parallel player/menu reset loop that repeats those operations.

## Joint verification before handing back the integrated scene

Use actual player input for this checklist. EditorOnly actions that apply direct Health damage check event paths, not playable attacks or balance.

1. **Import and startup:** clean Unity Console; Title → New Game; exactly one player, HUD, AudioListener and EventSystem; player cannot move/attack behind menus.
2. **Main world:** walk the routes, slopes, water edges and secrets; collect all 16 relics and use both chests; check collisions, camera clearance, objective prompts, regional music and volume settings.
3. **Each power:** inhale Sword, Bow and Fire enemies; equip/switch/clear correctly; hit through the intended attack handler; check damage, masks, aim, cooldown and one set of success effects. Confirm enemies path to the real player.
4. **Interruptions and retries:** cancel inhale on release/pause/death; pause during attacks/pickups; test fall recovery, main death/retry and New Game against the table above. No leaked projectiles, duplicate enemies or repeated rewards.
5. **Guardian:** enter with each power across separate runs and defeat it using that power's real attacks. Test boss death/retry repeatedly, including an active projectile. Confirm retained power, restored HP, active AI, correct boss bar/music and no extra encounter publishers.
6. **Ending and repeat run:** the Heart is blocked before boss defeat, usable afterward, and completion occurs once. Start another run and verify reset rules again.
7. **Release evidence:** run the full [acceptance route](ACCEPTANCE.md), capture the [performance evidence](PERFORMANCE.md), record at least three structured playtests, then verify the Windows build and matching gameplay recording from the chosen release commit. Fill in the [Phase 3 report](../phase3/final_report.md) with actual results.

Record each failure with commit, scene/build, reproduction steps, expected/actual result, fix and retest. Keep native checks pending until somebody actually runs them.

## Quick diagnosis

| Symptom | Check first |
|---|---|
| Enemies chase the old position / pickups trigger twice | Old capsule collider/Health still active; missed player-reference rebinding |
| Power works but HUD or ranged feedback is wrong | Holder did not publish `AbilityChanged`, or flow still publishes a duplicate review event |
| Controls work behind menus / UI stops in pause | Incomplete `_gameplayInput` list, or UI/data owner disabled along with gameplay input |
| Falling unexpectedly restores HP or clears power | `_onRespawned` wired to a full reset instead of motion/camera reset |
| No main-world enemies | Navigation readiness, common agent type, six source samples and inactive source prefab roots |
| Guardian idle after retry | Missing encounter re-arm / `BeginEncounter()` through the arena controller |
| Duplicate bars, hitsounds or ending transitions | Second HUD, extra health/boss relay, or both local attack and world feedback playing the same effect |
| Attack SFX ignore settings or pause | Local attack AudioSource bypassing shared volume/state handling |

## Handoff sign-off

| Owner | Record when finished | Current status |
|---|---|---|
| Tariq | Player/camera/holder/input bindings, HUD owner, integrated scene path and tested commit | Pending production integration |
| Mohammed | Enemy/navigation/attack/boss checks and tested commit | Pending integrated native verification |
| Both | Checklist results, remaining issues and retests | Pending native playthrough |
| Tariq / team release | Release scene, Windows build, profiler captures, playtests and gameplay media | Pending release evidence |

For delivered ownership and source/model evidence, see [Contribution](CONTRIBUTION.md) and the [Jehad handoff index](README.md).
