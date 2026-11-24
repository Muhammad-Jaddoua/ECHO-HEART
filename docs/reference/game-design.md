# Echoheart: Game Design Reference

Distilled from the approved Phase 1 proposal (`docs/phase1/echoheart_proposal.pdf`). This is the source of truth
for gameplay. If something here conflicts with a request, flag it.

## At a glance

| | |
|---|---|
| Genre | 3D action adventure, third-person, single-player, PC/Windows |
| Layout | **One Unity scene, two areas**: semi-open *main clearing* + compact *boss arena* |
| Length | ~8–10 minutes |
| Powers | Sword, Bow, Fire. **One held at a time**, one attack each |
| Finale | One boss (Corrupted Guardian), two attacks, no phases |
| Tone | Bright, whimsical. Palette: green, turquoise, cream; **violet = corruption** |
| Benchmark | *Kirby and the Forgotten Land*: Overview Trailer + Demo (Nintendo of America, 8 Mar 2022), overgrown urban stage |

## Story / goal
The Island Heart is corrupted and its guardian turned hostile. The player is an original round magical spirit who
inhales creatures to borrow their powers. Explore the clearing, learn the three powers, defeat the guardian, restore the Heart.

## Areas

| Area | Contents |
|---|---|
| Main area | Entry corner that teaches controls (safe first enemy teaches inhale); 3 enemy types; **8–12 collectibles** (one type); **2 small secrets** (placement / short detour / basic jump; *not* required); short side paths that reconnect to a clear route to the boss. Max ~6 active normal enemies. |
| Boss area | Compact arena; **retry point at entrance**; nearby sources of **all three powers**; the Guardian; the Island Heart. |

## Start / success / failure
- **Start:** New Game → main-area entry, full health, no ability.
- **Success:** defeat Guardian → interact with Heart (E) → restoration effect → completion screen.
- **Failure:** 0 health → Retry. **Falling** damages the player and returns them to safe ground.

## Core loop
Explore → meet enemy → inhale or fight → use its power → collect rewards / find secret → continue to boss.

## Inhale (hold right click)
- Captures **one nearby normal enemy in front** of the spirit after a **short pull**.
- Checks **range + line of sight**; target does **not** need to be weakened.
- Capture removes the enemy and **replaces** the held power. Works while holding any power.
- **Defeating** an enemy by attacking does **not** grant its power.
- Enemy "sources" near the main entry and near the arena entrance **replenish** after capture/defeat.
- The boss **cannot** be inhaled.
- Validate the target *before* capture so removal + ability replacement happen exactly once.

## Powers

| Power | Source enemy (provisional) | Attack |
|---|---|---|
| Sword | Blade Beetle | One short melee slash with a clear hit window (attack volume) |
| Bow | Thorn Archer | One straight arrow, short cooldown |
| Fire | Emberling | One fire projectile, distinct effect, shorter range |

Bow + Fire share one projectile component (different values/effects). No combos, charged shots, ammo, or movement powers.
A small accessory + HUD icon identifies the held power. Bow/Fire use a center-screen aim guide.

## Enemies
Three ground variants sharing movement, health, and FSM: **Idle → Chase → Attack** (+ captured / defeated handling).
Sword variant = close attack; Bow & Fire variants reuse ranged behaviour. Short attack cooldown = recovery window.

## Boss: the Corrupted Guardian
- FSM: **Telegraph → Attack → Recover**, choosing between **close-range slam** and **one simple ranged shot**.
- Both attacks have a visible warning and a recovery period. Sword gets safe approach/attack windows.
- No ability immunities; **each power alone can win**. Player picks a power at the arena entrance and needn't switch.
- One health bar (shown only in arena), no extra phases. Boss defeat enables Heart activation.

## Controls

> Changed from the Phase 1 proposal (which had hold E = inhale, F = interact): **inhale is now hold right click and
> interact is E**. Mention this in the Phase 2 checkpoint doc.

| Input | Action |
|---|---|
| WASD | Move (camera-relative) |
| Hold Left Shift | Run (1.6x move speed; added 24 Nov after playtest feedback) |
| Mouse | Orbit camera |
| Space | Jump |
| Left click | Attack with current power |
| Hold right click | Inhale |
| E | Interact (Heart) |
| Esc | Pause |

## Game flow & retry
- States: **Title → Playing ↔ Paused**; 0 HP → **Retry**; boss defeat + Heart → **Complete**.
- Main-area retry: back to entry, full HP, **no power**, enemies reset.
- Collectibles stay collected for the session (simple item IDs); reset **only on New Game**; never duplicate.
- Entering the arena sets the single retry point: boss retry restores player + boss HP and the **selected power**.
- No disk saves, no general checkpoint/snapshot system.

## Presentation targets
- HUD: health, current power (icon + text), collectible count; boss bar only in arena. Icon + text control prompts.
- Pause menu: resume, retry, volume (separate Music/SFX).
- Polish priority: movement feel, capture, hit response, camera (obstruction correction) **before** scenery.
- Essential animation: move, attack, inhale, hurt, defeat. Brief capture / hit / restoration VFX.
- Audio: one music loop; capture, attack, hit, pickup, boss-warning SFX; spatial audio on nearby enemies.
- Graphics: URP, simple lit materials, one main light, restrained shadows, small static environment.
- Performance target: steady **60 fps @ 1080p** on a team PC (record hardware + build settings).

## Scope

**Must-have:** one scene, two areas, third-person move + jump, direct inhale, Sword/Bow/Fire, three enemy types,
one collectible type, two secrets, one boss with two attacks, basic retry, win/fail flow, HUD, essential
audio/animation/effects, Windows build.

**Nice-to-have (only after core checks pass):** one breakable prop, a simple ability switch/barrier for an optional
secret, extra ambience, one extra secret.

**NOT doing:** extra areas/abilities, open-world streaming, required fragment hunts / ability-gate chains, advanced
puzzles, dash/flight, inventory/skill trees, ability combinations, complex combos, boss phases / extra bosses, shops,
dialogue/cutscenes, multiplayer, online, persistent saves, mobile/console.

## Acceptance checks (from proposal §5)

| ID | Owner (proposal) | Check |
|---|---|---|
| P01 | Tariq | Walk, jump, land, turn beside a wall without clipping or losing the player view |
| P02 | Tariq | Inhale captures a valid enemy once, rejects blocked/out-of-range, replaces power |
| P03 | Muhammad | All three attacks hit correctly; enemies chase/attack; capture & defeat can't occur twice |
| P04 | Jehad | Two areas connect; collectibles work once; both secrets reachable; boss route clear |
| P05 | Muhammad | Both boss attacks telegraph; each power can win; death/retry + Heart completion work |
| P06 | Shared | Polish after a complete run: feedback, camera, collisions, sounds, measured perf |

**Core verification:** fresh run; capture each enemy type; replace each power; blocked inhale; repeated hits; touch a
collectible twice; die in main area; retry boss; new session; collectibles never duplicate and reset only on New Game;
both secrets + main route checked for getting stuck.
**Boss verification:** three recorded wins from full HP: Sword-only, Bow-only, Fire-only; boss can't be inhaled; melee
recovery windows safe; retry restores encounter; completion happens once.
**Usability:** ≥3 testers with minimal guidance; fix most common control/clarity problems first.
