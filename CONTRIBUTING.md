# Contributing to Echoheart

## One-time setup
1. Install **Unity 6000.3.27f1** (Unity Hub) with Windows Build Support (Mono/IL2CPP).
2. Install Git + **Git LFS**, then run `git lfs install` once.
3. Clone: `git clone https://github.com/Muhammad-Jaddoua/ECHO-HEART` and open the folder in Unity Hub.
4. (Recommended) Unity Smart Merge. `.gitattributes` routes scenes/prefabs to a merge driver named
   `unityyamlmerge`; without this config Git just uses its normal text merge. Add to your global `~/.gitconfig`
   (adjust the editor path):
   ```
   [merge "unityyamlmerge"]
       name = Unity SmartMerge
       driver = 'C:/Program Files/Unity/Hub/Editor/6000.3.27f1/Editor/Data/Tools/UnityYAMLMerge.exe' merge -h -p --force %O %B %A %A
       recursive = binary
   ```

## Branching
- `main` is always playable and protected. **No direct commits.**
- One branch per issue: `feature/<issue#>-short-name`, `fix/<issue#>-...`, `docs/...`, `chore/...`.
- Keep branches short-lived (≤ 1–2 days). Rebase on / merge `main` before opening the PR.

## Commits
Format: `type(area): summary (#issue)`. Types: `feat`, `fix`, `refactor`, `docs`, `chore`, `art`, `audio`.
Example: `feat(ai): enemy FSM idle/chase/attack states (#7)`.

## Pull requests
- Use the PR template. Link the issue (`Closes #7`).
- **At least one teammate review** before merge. Reviewer actually opens the project and plays the change.
- PR must: compile with 0 console errors, not break the sandbox/main scene, include asset credits for new assets.
- Squash-merge; delete the branch.

## Unity rules (avoid merge hell)
- Work in your own sandbox scene and in prefabs you own (see `docs/reference/architecture.md`).
- Only the integrator commits `Assets/_Project/Scenes/Echoheart.unity`.
- `ProjectSettings/` changes go in a separate small PR.
- Never commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, builds.
- Third-party assets go in `Assets/ThirdParty/<Pack>/` (only used files) and are logged in `docs/ASSET_CREDITS.md`.

## Issues & board
- Every task is an issue with: owner, milestone, labels, dependencies, acceptance check.
- Board: Backlog → Ready → In Progress → Review/Test → Done. Move your cards daily.
- Bugs: steps to reproduce, expected vs actual, severity P1 (blocks completion) / P2 (hurts clarity/feel) / P3 (polish).

## Releases
Tag each milestone (`v0.2-milestone2`, `v1.0-final`) and attach the zipped Windows build to a GitHub Release.
