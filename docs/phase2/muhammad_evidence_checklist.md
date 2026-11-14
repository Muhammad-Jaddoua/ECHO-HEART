# Evidence still to capture by hand (Muhammad, Combat and AI)

Feeds `docs/phase2/checkpoint.md`. Tick a box only when the file or link exists. Do not commit large capture files; link them.

## Recordings

| Done | Item | Owner | Notes |
|---|---|---|---|
| [ ] | Boss win, Sword only, from full HP | Muhammad | Fresh retry. Show the boss bar and one telegraph |
| [ ] | Boss win, Bow only, from full HP | Muhammad | Same |
| [ ] | Boss win, Fire only, from full HP | Muhammad | Same |
| [ ] | Real time to win per power noted | Muhammad | Compare with the ideal 2.7 s, 6.8 s, 5.2 s |

## Screenshots

| Done | Item | Owner |
|---|---|---|
| [ ] | Slam telegraph ring visible | Muhammad |
| [ ] | Ranged telegraph (body colour pulse) | Muhammad |
| [ ] | Boss health bar in the arena | Muhammad |
| [ ] | Sword hit window in use | Muhammad |
| [ ] | Bow arrow in flight | Muhammad |
| [ ] | Fire bolt in flight | Muhammad |
| [ ] | Blade Beetle in action | Muhammad |
| [ ] | Thorn Archer in action | Muhammad |
| [ ] | Emberling in action | Muhammad |
| [ ] | `CombatPlayTest` final log line (45 passes, 0 fails) | Muhammad |
| [ ] | Self test results in the console | Muhammad |

## Profiler (issue #22)

| Done | Item | Owner |
|---|---|---|
| [ ] | Development build from the integration branch | Tariq |
| [ ] | Capture of 6 enemies + boss encounter | Muhammad |
| [ ] | Table in `muhammad_verification.md` section 8 filled in (hardware, fps, CPU ms, GC alloc, top 3) | Muhammad |
| [ ] | One measured improvement with before and after | Muhammad |

## Integration checks that need teammates

| Done | Item | Owner |
|---|---|---|
| [ ] | Real inhale captures each enemy type (P02, P03) | Tariq, Muhammad |
| [ ] | Retry through `RetryService` restores boss and player (P05) | Jehad, Muhammad |
| [ ] | Heart enabled only after `BossDefeated`, completion once (P05) | Jehad |

## Production evidence

| Done | Item | Owner |
|---|---|---|
| [ ] | Board snapshot showing issues #4 to #9, #19, #22 | Muhammad |
| [ ] | PR links #24 to #28 collected | Muhammad |
| [ ] | PRs opened for `feature/8-enemy-spawner`, `feature/9-boss-fsm`, `feature/19-graybox-prefabs` (link each) | Muhammad |
| [ ] | Review comments from Tariq on each PR | Tariq |
| [ ] | Merge order agreed (stack merges bottom-up) | Muhammad, Tariq |
