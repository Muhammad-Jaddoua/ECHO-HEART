# ECHOHEART — Claude working guide

Echoheart is the SWE 402 (Game Programming, KFUPM, Term 261) team vertical slice: a short
third-person 3D ability-copying adventure in Unity. A round spirit inhales enemies to copy
**Sword, Bow, or Fire** (one held at a time), explores one island clearing, and beats the
Corrupted Guardian boss to restore the Island Heart. Kirby-*style* mechanics, **original** art,
names, and characters. Nothing Nintendo/HAL may be shipped or imitated.

Team (3): **Muhammad Jaddoua** (repo owner, the person usually driving Claude), **Tariq Alothman**, **Jehad Alharbi**.
Instructor: Dr. Omar Hammad. Repo: https://github.com/Muhammad-Jaddoua/ECHO-HEART

## Read these before doing related work

| File | Use it for |
|---|---|
| `docs/reference/game-design.md` | What the game *is*: rules, controls, enemies, boss, flow, scope (Must / Nice / NOT doing). Source of truth for gameplay. |
| `docs/reference/course-requirements.md` | Phase deliverables, rubrics, deadlines, required competencies. Check before writing any phase doc. |
| `docs/reference/architecture.md` | Unity setup, folder layout, scene structure, code contracts (interfaces/events), ownership. |
| `docs/reference/assets.md` | Approved free asset sources, license rules, import rules. |
| `docs/plan/milestone2-plan.md` | Current plan, team split, day-by-day schedule, issue list for Milestone 2. |
| `CONTRIBUTING.md` | Git/branch/PR/issue workflow (graded!). |
| `docs/ASSET_CREDITS.md` | Every third-party asset in the build, logged on import. |
| `docs/phase1/echoheart_proposal.pdf` | The approved Phase 1 proposal (reading version). |

## Current milestone

**Milestone 2 — Phase 2 "Systems Vertical Slice / Advanced Mechanics", due Thu 19 Nov 2026** (week 14).
Playable **graybox** from New Game → boss → Heart → completion screen, with fail/retry, all three
powers, three enemy types, HUD, first-pass audio, plus `docs/phase2/checkpoint.md` (playtest report,
board snapshot, recording, known issues, profiler capture). Graybox + key models only. The art pass is Phase 3.
Final build + presentation: week 17 (Dec 6–10).

## Hard rules

1. **Scope discipline.** Build only what `game-design.md` lists as Must-have. Nice-to-haves only after the
   full loop passes its checks. Never add anything on the NOT-doing list (extra abilities/areas, combos,
   boss phases, saves, dialogue, etc.). If a request would grow scope, say so first.
2. **Never commit to `main`.** Branch per issue (`feature/<issue#>-short-name`, `fix/...`, `docs/...`, `chore/...`),
   PR into `main`, at least one teammate review, link the issue (`Closes #12`). Ask before committing or pushing.
   The course grades this workflow (Phase 2: 15 pts).
3. **Don't edit the main scene casually.** `Assets/_Project/Scenes/Echoheart.unity` belongs to the integrator.
   Do feature work in prefabs and in your own sandbox scene (`Scenes/Sandbox/Sandbox_<Name>.unity`).
   Unity scene merges are painful. See `architecture.md` → Ownership.
4. **Respect the contracts.** Systems talk through the interfaces/events in `Assets/_Project/Scripts/Core`
   (`IDamageable`, `IInhalable`, `IResettable`, `GameEvents`, `AbilityDefinition`). Changing a contract means
   telling the team. It's the thing that lets three people work in parallel.
5. **Original IP + licensed assets only.** Use the sources in `assets.md`; log every import in
   `docs/ASSET_CREDITS.md` in the same PR. No ripped/Nintendo assets, no unlicensed music.
6. **Keep the build playable.** `main` must open, compile with zero errors, and run start→end. Check the Unity
   console (MCP `Unity_GetConsoleLogs`) after every script change.

## Working with Unity through the MCP

- Unity **6000.3.27f1 (Unity 6.3 LTS)**, **URP**, new **Input System**, **Cinemachine 3**, **ProBuilder** (graybox),
  **AI Navigation** (NavMesh). Windows target. Built-in pipeline is wrong; this project is URP.
- The Unity MCP (`mcp__unity-mcp__*`) talks to whatever project is **open in the Editor**. Before acting, confirm
  `Application.dataPath` points to this repo, not another project (e.g. the Assignment 3 JuiceLab project).
- Prefer writing real `.cs` files under `Assets/_Project/Scripts/...` (reviewable, versioned) over one-off
  `Unity_RunCommand` snippets. Use `Unity_RunCommand` for editor automation: creating prefabs, wiring references,
  ProBuilder blockouts, baking NavMesh, checking scene state.
- After changes: read console logs, then verify visually with `Unity_SceneView_Capture2DScene` /
  `Unity_Camera_Capture` / multi-angle capture. Play-mode behaviour must be tested by a human or via a test.
- Serialize references in the Inspector rather than `GameObject.Find`. Use ScriptableObjects for tunables
  (ability stats, enemy stats) so balancing doesn't need code changes.

## Code style

- C#, namespaces `Echoheart.<Area>` (Core, Player, Combat, AI, World, UI, Audio). One MonoBehaviour per file, file = class name.
- `[SerializeField] private` fields, not public fields. `PascalCase` methods/properties, `_camelCase` private fields.
- State machines as explicit `enum` + `switch` or small state classes. Keep them readable, because the AI rubric wants
  "state-driven logic" that is visible and explainable.
- Guard "happens once" logic explicitly (capture, defeat, completion, collectible pickup, per-attack hit IDs). The
  proposal promises these never double-fire.
- Comment *why*, not *what*. No dead code or debug spam left in PRs.

## Commits

Meaningful messages (`feat(combat): shared projectile with sweep hit detection (#14)`). Claude attribution is
disabled in `.claude/settings.json`. Don't add `Co-Authored-By` trailers.
