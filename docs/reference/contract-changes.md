# Contract changes log

Every change or addition to the shared contracts (`Assets/_Project/Scripts/Core`, GameEvents usage, layers, retry
rules) is recorded here so teammates and their Claude sessions see it without a chat message. Newest first.
If you change a contract, add an entry in the same PR. `CLAUDE.md` rule 4 still applies: tell the team too.

| Date | By | Change | Who must act |
|---|---|---|---|
| 2026-11-13 | Muhammad | **New `Core/IAbilityAttack`** (`bool Handles(AbilityType)`, `bool TryUse(AbilityDefinition, Transform origin, Vector3 aimDirection, GameObject owner)`). Combat will provide `AbilityAttackRunner` (one `TryUse(def, origin, aim)` entry point) plus `SwordAttack` and `ProjectileAttack`. Attacks own their cooldown. | **Tariq:** `PlayerAbilityHolder` calls the runner on left click; pass camera aim for Bow/Fire, facing for Sword; origin = hand/muzzle transform. |
| 2026-11-13 | Muhammad | **`Health` never raises `GameEvents`.** `Combat/HealthEventRelay` will bridge it (mode Player or Boss). | **Tariq:** add `Health` (Team.Player, ~0.6 s invulnerability) + `HealthEventRelay` (Player) to the Player prefab. |
| 2026-11-13 | Muhammad | **Boss encounter trigger:** `BossBrain.BeginEncounter()` (idempotent, reset by `ResetFor`). Boss raises `BossEncounterStarted` once, `BossHealthChanged` on change/reset, `BossDefeated` exactly once. Boss is not `IInhalable`. | **Jehad:** arena entrance calls `BeginEncounter()`; Heart listens to `BossDefeated`. **Tariq:** inhale must ignore colliders without `IInhalable`. |
| 2026-11-13 | Muhammad | **Retry/reset:** spawners implement `IResettable` for the enemies they own. Enemy `ResetFor(MainArea/NewGame)` = spawn point, full HP; boss `ResetFor(BossArena/NewGame)` = full HP, idle, encounter guard cleared. | **Jehad:** `RetryService` collects `IResettable`s with `FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include)` and calls `ResetFor(scope)`. |
| 2026-11-13 | Muhammad | **Layers for attacks:** player attacks/projectiles on `PlayerAttack` (11), enemy/boss attacks on `EnemyAttack` (12). Attacks use explicit layer masks (queries ignore the collision matrix). | All: put new damaging prefabs on the right layer. |
| 2026-11-13 | Muhammad | **`UnityEvent` audio hooks** on boss (`OnTelegraph`, `OnAttack`, `OnDefeated`) and enemies (hit, capture) so the AudioManager can hook SFX without new GameEvents. | **Jehad:** wire SFX in prefabs/AudioManager. |
| 2026-11-13 | Muhammad | `architecture.md` fixed: inhale is hold **right click** (was stale "Hold-E"). | n/a |

Status key: entries above are **planned contracts**; the matching code lands in the Combat/AI PRs (issues #4-#9).
