# Echoheart: Technical Architecture

Goal: three people build in parallel for 6 days without blocking each other. The trick is **agreeing on small
contracts first** (Day 0), working in **prefabs + personal sandbox scenes**, and integrating into one main scene.

## Unity setup

| Item | Choice |
|---|---|
| Editor | **6000.3.27f1** (Unity 6.3 LTS). Everyone installs exactly this via Unity Hub. |
| Template | Universal 3D (URP) |
| Packages | URP 17.3, Input System 1.20, Cinemachine 3.1.7, ProBuilder 6.1.4, AI Navigation 2.0, Recorder 5.1.8 (gameplay video), uGUI/TextMeshPro, Test Framework, Unity AI Assistant (hosts the Unity MCP for Claude). Removed from template: Plastic SCM (collab-proxy), Visual Scripting, Multiplayer Center |
| Project settings | Asset Serialization = **Force Text**; Version Control = **Visible Meta Files**; Active Input Handling = Input System; URP assets (`Assets/Settings/PC_RPAsset`, `Mobile_RPAsset`): **Depth Texture ON** (needed by Cartoon FX soft particles). Product name "Echoheart" |
| Input | `Assets/_Project/Input/EchoheartControls.inputactions` (the template's default action map, moved). Tariq adapts it to our controls: Move, Look, Jump, Attack, **Inhale (hold right click)**, **Interact (E)**, **Pause (Esc)** |
| Repo layout | Unity project at **repo root** (`Assets/`, `Packages/`, `ProjectSettings/` next to `docs/`). The Unity `.gitignore` is root-anchored. |

## Folder layout

```
Assets/
  _Project/
    Scenes/
      Echoheart.unity            # THE game scene (integrator-owned)
      Sandbox/Sandbox_Muhammad.unity, Sandbox_Tariq.unity, Sandbox_Jehad.unity
    Scripts/
      Core/      # contracts, GameEvents, GameManager, enums, DamageInfo   (shared, change by agreement)
      Player/    # motor, camera, input, inhale, ability holder, aim       (Tariq)
      Combat/    # Health, melee hitbox, projectile, ability attacks      (Muhammad)
      AI/        # enemy FSM, enemy variants, spawner, boss FSM           (Muhammad)
      World/     # collectibles, secrets, fall zones, retry points, Heart (Jehad)
      UI/        # HUD, menus, prompts                                    (see split)
      Audio/     # AudioManager, mixer hooks                              (Jehad)
      Testing/   # TrainingDummy, DummyInhalable, TestPlayerMover: sandbox stand-ins, never in the shipped scene
    Prefabs/  Player/ Enemies/ Boss/ Projectiles/ World/ UI/
    Data/     Abilities/*.asset  Enemies/*.asset   (ScriptableObject tunables)
    Art/      Materials/ Graybox/ Models/ VFX/
    Audio/    Music/ SFX/  Mixer.mixer
    Input/    EchoheartControls.inputactions
  Settings/                       # URP pipeline/renderer assets + volume profile (from the template; leave here)
  ThirdParty/<PackName>/          # imported free assets, only the files we use (log in ASSET_CREDITS.md)
```

## Scene structure (`Echoheart.unity`)

```
--- Managers ---      GameManager, AudioManager, RetryService, EventSystem
--- Player ---        Player (prefab), CameraRig (Cinemachine)
--- World ---         MainArea (prefab, Jehad), BossArena (prefab, Jehad), Lighting, Sky
--- Spawns ---        EnemySpawnPoints (in-area prefabs), PowerSources near entry + arena entrance
--- UI ---            HUD canvas, Menus canvas (Title/Pause/Death/Complete)
```
The `--- Name ---` objects are **hierarchy dividers** only (tagged `EditorOnly`, stripped from builds). Place objects
*below* them, never *inside* them. EditorOnly would strip the children too.

Level geometry, collectibles, secrets, and spawn points live **inside the area prefabs**, so Jehad edits prefabs, not
the scene. The integrator is the only one who saves `Echoheart.unity` in a PR.

## Code contracts (Day 0, in `Scripts/Core`): **implemented**

Files: `CoreTypes.cs` (enums), `AbilityDefinition.cs`, `DamageInfo.cs` (+ `AttackIds.Next()`), `IDamageable.cs`,
`IInhalable.cs`, `IResettable.cs`, `GameEvents.cs`, `Layers.cs`. Read the XML comments in those files for the exact
rules. The sketch below is a summary.

```csharp
namespace Echoheart.Core {
  public enum AbilityType { None, Sword, Bow, Fire }
  public enum Team { Player, Enemy }
  public enum GameState { Title, Playing, Paused, Dead, Complete }
  public enum RetryScope { MainArea, BossArena, NewGame }

  // ScriptableObject: one asset per power. Tunables live here.
  public class AbilityDefinition : ScriptableObject {
      AbilityType Type; string DisplayName; Sprite Icon; GameObject AccessoryPrefab;
      float Cooldown; int Damage; float Range;            // sword: hit-box size/window; bow/fire: projectile values
      GameObject ProjectilePrefab; AudioClip UseSfx; GameObject UseVfx;
  }

  public struct DamageInfo { public int Amount; public Vector3 Point; public Vector3 Direction;
                             public Team SourceTeam; public int AttackId; public GameObject Source; }

  public interface IDamageable { Team Team { get; } bool TryApplyDamage(in DamageInfo info); }
  // AttackId: each swing/projectile gets a unique id; a target ignores an id it already took -> "hit at most once".

  public interface IInhalable {
      bool CanBeInhaled { get; }                // false for boss, already-captured, defeated
      AbilityDefinition GrantedAbility { get; }
      Transform InhaleTarget { get; }           // point the inhale aims at / pulls in
      void OnInhaleStart(Transform inhaler);    // enemy stops AI, plays struggle
      void OnInhaleCancel();                    // resume AI
      void OnCaptured();                        // exactly once: disable/despawn, notify spawner
  }

  public interface IResettable { void ResetFor(RetryScope scope); }   // enemies, boss, collectibles, player

  public static class GameEvents {             // decoupled UI/audio/flow
      event Action<GameState> StateChanged;
      event Action<int,int> PlayerHealthChanged;        // current, max
      event Action<AbilityDefinition> AbilityChanged;   // null = None
      event Action<int,int> CollectiblesChanged;        // collected, total
      event Action<int,int> BossHealthChanged; event Action BossEncounterStarted; event Action BossDefeated;
      event Action PlayerDied; event Action<RetryScope> RetryRequested; event Action GameCompleted;
      event Action<string> PromptChanged;               // "Hold right click to inhale", "" = hide
  }
}
```

**Shared `Health` component** (`Scripts/Combat/Health.cs`, **implemented**) implements `IDamageable`: team, max/current
HP, optional invulnerability window, remembers the last 32 attack ids (hit-once rule), C# events `Damaged`, `Died`,
`Changed(current,max)`, inspector UnityEvents for feedback, `Heal()`, `ResetHealth()`. Used by player, enemies, boss.
AttackId `0` = untracked damage (fall damage).

**Added contracts (Muhammad, 13 Nov; full log in `docs/reference/contract-changes.md`):**
- `Core/IAbilityAttack` (`Handles(AbilityType)`, `TryUse(def, origin, aimDirection, owner)`): how `PlayerAbilityHolder`
  fires the held power. Combat implements it (Sword, Bow/Fire); implementations own the cooldown. Tariq's holder calls
  `AbilityAttackRunner.TryUse(...)` and never touches the attack classes.
- **Health never raises `GameEvents` itself.** `Combat/HealthEventRelay` (flag Player or Boss) bridges a `Health` to
  `PlayerHealthChanged`/`PlayerDied` or `BossHealthChanged`. Put one on the player prefab (Tariq).
- Boss: `BossBrain.BeginEncounter()` starts the fight (Jehad's arena entrance calls it); the boss raises
  `BossEncounterStarted` once and `BossDefeated` exactly once, and is **not** `IInhalable` (inhale must ignore
  colliders without `IInhalable`).
- Retry: spawners are the `IResettable` for the enemies they own (captured enemies are disabled, so they can't reset
  themselves). `RetryService` should call `ResetFor` on every `IResettable` found with `FindObjectsByType<MonoBehaviour>(Include)`.
- Attack layers: player attacks/projectiles on `PlayerAttack`, enemy/boss on `EnemyAttack`. Physics queries ignore the
  collision matrix, so attacks pass explicit layer masks.

**Sandbox stand-ins** (`Scripts/Testing`): `TrainingDummy` (Health + hit flash + auto-revive), `DummyInhalable`
(implements `IInhalable`, grants an ability, respawns, errors if captured twice), `TestPlayerMover` (WASD/Space capsule).
Ability assets: `Data/Abilities/Ability_Sword|Bow|Fire.asset` (placeholder values, tune later).

## System boundaries

| System | Owns | Talks to |
|---|---|---|
| PlayerMotor / Camera | CharacterController movement, gravity, ground check, jump, camera-relative input; Cinemachine orbit + deoccluder | Input actions; RetryService (teleport) |
| PlayerInhale | Hold-right-click cone/sphere query → range + LoS raycast → pull timer → `IInhalable.OnCaptured()` | `PlayerAbilityHolder.Equip()` |
| PlayerAbilityHolder | Current `AbilityDefinition`, accessory swap, raises `AbilityChanged` | Combat attacks via `IAbilityAttack` |
| Combat | `Health`, `MeleeHitbox` (Sword), `Projectile` (sweep with SphereCast between frames, Bow/Fire), attack ids, team filtering | `IDamageable` |
| Enemy AI | `EnemyBrain` FSM: Idle → Chase → Attack (cooldown) + BeingInhaled + Defeated; NavMeshAgent; `EnemyDefinition` SO; implements `IInhalable`, `IResettable` | Combat, `EnemySpawner` (replenish) |
| Boss AI | `BossBrain` FSM: Idle → Telegraph(choose Slam/Ranged) → Attack → Recover; `CanBeInhaled=false` | Combat, GameEvents (boss bar), Heart |
| World | Collectible (stable string id, once per session, reset on NewGame), SecretArea, FallZone (damage + safe-ground respawn), ArenaEntrance (sets retry point, starts boss), IslandHeart (F after boss) | GameEvents, RetryService |
| Flow | GameManager state machine (Title/Playing/Paused/Dead/Complete), RetryService (current retry point, calls `IResettable`s), timeScale on pause | GameEvents |
| UI | HUD (health, power icon + name, collectibles, boss bar, prompt, crosshair for Bow/Fire), Title/Pause(resume/retry/volume)/Death/Complete | listens to GameEvents only |
| Audio | AudioMixer (Music/SFX groups + exposed volumes), one music loop, SFX on events; 3D AudioSources on enemies | GameEvents |

## Physics layers (define Day 0)
`Player`, `Enemy`, `Boss`, `PlayerAttack`, `EnemyAttack`, `Environment`, `Collectible`, `Trigger`. Configure the collision
matrix so attacks only hit the other team and triggers only see the player. Inhale LoS raycasts use `Environment` as blocker.

## Ownership & merge-conflict rules
- Each person owns their folders + prefabs (see milestone plan). Touching someone else's prefab = ask first / pair.
- **Never** commit someone else's sandbox scene. Only the integrator commits `Echoheart.unity`.
- Configure Unity Smart Merge (UnityYAMLMerge) locally as a fallback (see CONTRIBUTING.md).
- `ProjectSettings/` changes (layers, tags, input, quality) go in their own small PR so they're easy to review.
