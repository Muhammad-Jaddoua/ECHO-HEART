# SWE 402 Course Requirements (project)

Source: https://github.com/gamedevkfupm/swe402 (`project/`, `syllabus.md`, Lecture 6 DevOps). Snapshot taken 13 Nov 2026.
Re-check the course repo before each phase submission, because it changes during the term.

## Project purpose
One **production-quality playable scene (vertical slice)**, benchmarked against a scene from a target game, with
comparable complexity in gameplay systems, visual fidelity, audio, technical quality, and production workflow.
Project = **20%** of the course grade.

## Required competencies (every phase doc must show these)
1. **Core gameplay systems:** controls, interaction model, fail/win, game-state flow
2. **Physics & collision:** deterministic interactions, hit detection, reliable response
3. **AI behaviour:** state-driven (FSM / behaviour tree) enemy or boss
4. **Real-time graphics pipeline:** materials, lighting, render settings, readability
5. **Technical art & polish:** animation, VFX, camera design, post-processing
6. **Audio:** SFX, music, mixing, spatial/3D audio
7. **UI/UX feedback:** HUD, prompts, combat/system feedback, usability flow
8. **Performance & debugging:** profiling evidence, bottleneck analysis, stability fixes
9. **Production & collaboration:** GitHub workflow, docs, issues, milestones, iterative delivery

## Schedule (Term 261, 2026)

| Week | Dates | Milestone |
|---|---|---|
| 4 | Sep 6–10 | Phase 1 Proposal ✅ (done) |
| **14** | **Nov 15–19** | **Phase 2 "Advanced Mechanics" (Systems Vertical Slice). Team target: Thu 19 Nov** |
| 15 | Nov 22–26 (break 22–23) | Quiz 3 |
| 17 | Dec 6–10 | Phase 3/4 "Advanced Graphics" + Final Presentation (live demo) |

Classes end Dec 10. Finals Dec 13–24.

## Repository & workflow requirements (all phases)
- One GitHub repo per team; `README.md` with **setup, controls, build steps, team roles**.
- **Feature branches; never commit directly to `main`.**
- **PRs with at least one teammate review.**
- GitHub Issues + Project board with status updates.
- Meaningful commit messages; link commits/PRs to issues.
- Per-phase package: **GitHub URL + milestone tag**, **playable build artifact**, required docs, demo media.
- Phase 1 also expected: branching policy, PR template, contribution rules (now in `CONTRIBUTING.md`, `.github/`).
- Lecture 6 (DevOps): `main` stays releasable; correct `.meta` handling; ignore generated folders; **Git LFS** for large
  binaries (.psd, .fbx, .wav, .mp4, big textures); no secrets in repo; CI optional but valued ("can we trust this
  change?"); every build maps to a branch + commit; releases = version + notes + builds.

---

## Phase 2: Systems Vertical Slice (CURRENT)

**Deliver:** one end-to-end playable **graybox** vertical slice where core systems are implemented, connected, testable.
**Doc:** `docs/phase2/checkpoint.md` must include:
- **Playable slice summary:** start/end boundaries implemented, core loop status, win/fail behaviour, known limitations
- **Systems implementation status:** controls/interactions, physics/collision, AI/combat/challenge logic, game-state flow
- **UI + audio first pass:** base HUD/prompts/feedback, initial SFX/music
- **Playtest report:** test setup, findings, priority issues, concrete planned fixes
- **Production update:** board progress, completed tasks, carry-over tasks, Phase 3 readiness bullets

Our proposal (§4.4) additionally promised for this checkpoint: one scene with exactly main area + arena; 3 enemy
variants + 3 copied attacks (one held); collectibles, 2 secrets, health/retry, readable HUD, first complete
boss→ending flow; **board snapshot, short gameplay recording, known issues, initial profiler capture from the build.**

### Phase 2 rubric (100)

| Criterion | Pts | "Strong" means |
|---|---|---|
| Playable End-to-End Flow & Mechanics Readiness | **30** | Fully playable from clear start trigger to end condition; loop consistent; win/fail implemented and **repeatable**; progression coherent, testable **without instructor intervention** |
| Systems Integration & Technical Coverage | **25** | Gameplay, physics/collision, AI/challenge, base UI feedback, first-pass audio implemented with clear integration; system boundaries & dependencies understood; technically defensible |
| Playtesting, Debugging & Iteration | **20** | Structured playtest evidence (what, by whom, outcomes); issues triaged by severity/priority; fixes applied or scheduled with rationale; debugging grounded in observed behaviour |
| Production Discipline & Repo Workflow | **15** | Issues, PRs, milestone/task status, ownership clearly maintained; progress aligns with Phase 2 targets; blockers & carry-over explicitly managed |
| Technical Communication & Professionalism | **10** | Clear, concise, well-structured; correct terminology; screenshots/clips/logs support claims |

---

## Phase 3: Final Build & Presentation (Dec 6–10)

**Doc:** `docs/phase3/final_report.md`: final build summary (RC build link, controls, how to run); visual production
pass (lighting, materials/shaders, VFX, animation, camera, post-processing); audio pass (balanced mix, event-driven SFX,
music, spatial); performance & optimization report (profiler screenshots/metrics, bottlenecks, fixes); bug triage &
stability log (severity, resolutions); presentation package (demo plan, **fallback video**, repo cleanup); team postmortem.

| Criterion | Pts |
|---|---|
| Final Build Quality & Playability (runs end-to-end, clear reproducible instructions, fallback video matches build) | 25 |
| Visual & Audio Production Pass (intentional, technically grounded) | 25 |
| Performance Optimization & Stability (profiling evidence, documented fixes, severity-structured bug log, stable target fps on declared hardware) | 20 |
| Production Discipline & Repo Cleanup (README/controls/build/roles, issues/PRs/milestones, **final tag/release published**) | 15 |
| Presentation, Postmortem & Communication | 15 |

Our proposal also promised at final delivery: Windows build, controls/run instructions, **asset credits**, brief
playtest log, profiling evidence, one documented measured performance improvement, ≥3 testers.

## Phase 1 (done, for reference)
Required `docs/phase1/proposal.md` in the repo. **Currently only the PDF is in this repo.** Add the Markdown
proposal if it was submitted elsewhere, since the repo should hold the full trail.
