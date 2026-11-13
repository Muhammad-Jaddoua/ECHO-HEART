# ECHOHEART

*Borrow their powers. Restore your world.*

A short third-person 3D ability-copying adventure made in Unity for **SWE 402 Game Programming** (KFUPM, Term 261).
A small round spirit explores an island clearing, inhales enemies to copy **Sword**, **Bow**, or **Fire**, finds hidden
rewards, and defeats the Corrupted Guardian to restore the Island Heart.

> Status: **Milestone 2 (Systems Vertical Slice) in progress, due 19 Nov 2026.**

## Team

| Member | Role |
|---|---|
| Muhammad Jaddoua | Combat, enemy & boss AI, health/damage, core contracts |
| Tariq Alothman | Player movement, camera, inhale, HUD, scene integration & builds |
| Jehad Alharbi | Level layout, collectibles & secrets, game flow & menus, audio |

*(Roles per the Milestone 2 plan; final split confirmed at the 14 Nov team meeting.)*

## Controls (planned)

| Input | Action |
|---|---|
| WASD | Move |
| Mouse | Camera |
| Space | Jump |
| Left click | Attack with current power |
| Hold right click | Inhale (copy an enemy's power) |
| E | Interact |
| Esc | Pause |

## Setup

1. Install **Unity 6000.3.27f1** via Unity Hub (with Windows Build Support) and **Git LFS** (`git lfs install`).
2. `git clone https://github.com/Muhammad-Jaddoua/ECHO-HEART`
3. Open the folder in Unity Hub → open `Assets/_Project/Scenes/Echoheart.unity` → Play.

## Build

`File → Build Profiles → Windows` → scene list contains only `Echoheart.unity` → Build into `Builds/` (git-ignored).
Builds for each milestone are attached to the GitHub Release for that tag.

## Docs

- Proposal: [`docs/phase1/echoheart_proposal.pdf`](docs/phase1/echoheart_proposal.pdf)
- Design reference: [`docs/reference/game-design.md`](docs/reference/game-design.md)
- Architecture: [`docs/reference/architecture.md`](docs/reference/architecture.md)
- Milestone 2 plan: [`docs/plan/milestone2-plan.md`](docs/plan/milestone2-plan.md)
- Contributing / workflow: [`CONTRIBUTING.md`](CONTRIBUTING.md)
- Asset credits: [`docs/ASSET_CREDITS.md`](docs/ASSET_CREDITS.md)
