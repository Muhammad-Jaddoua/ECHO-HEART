# ECHOHEART — final-build report

**Report state:** integration candidate, awaiting team/native sign-off.  
**Jehad checkpoint:** [Jehad_V1.27.unity](../../Assets/_Project/Scenes/Jehad/Jehad_V1.27.unity).  
**Scope of verified evidence:** source/model/asset checks; no native Unity build validation.

This report consolidates Jehad's world, flow, presentation and UI contribution.
The production player, release build, live-input combat wins, native performance,
playtests and final media remain team integration/evidence gates. The current
report must not be presented as a passed final release.

## Build and playable scope

The intended slice is explore → acquire Sword/Bow/Fire → defeat the Guardian →
restore the Heart, with death/retry in the main world and arena. The review scene
contains the world/presentation and a movement-only capsule. Its actual controls
are WASD, Shift, Space, middle-mouse orbit, wheel zoom, E and Esc.

| Delivery item | Recorded state |
|---|---|
| Windows release-candidate build/run verification | Pending team link and native run |
| Production player, inhale, ability holder and final HUD integration | Pending |
| Sword/Bow/Fire input-driven boss wins | Pending integrated-player evidence |
| Matching fallback gameplay video and live-demo rehearsal | Pending |
| Final tag/release and build-to-commit mapping | Pending team release verification |

The [integration handoff](../jehad/INTEGRATION.md) identifies concrete bindings.
The integrator-owned release scene/build settings are preserved by this batch.

## World and visual production

Jehad's authored world contains the semi-open main island and Guardian arena,
trails, mountain/alpine, forest/amber grove, lake/river and grotto landmarks,
16 relics and two chests. Nature geometry comes from the credited CC0 Kenney
and KayKit packs; shared meshes, spatial foliage batches, frustum checks and
distant LODs support the environment. Terrain/layout, water/chest presentation,
UI graphics and particle configuration are project work.

The scenic title camera, safe-area UI, health trails, objective bearing,
discoveries and collectible sparkles support readability. Actual lighting,
materials/shaders, post-processing, animation, camera clearance and final visual
quality still require Editor/build review and screenshots. See
[Contribution](../jehad/CONTRIBUTION.md) and [Asset credits](../ASSET_CREDITS.md).

## Audio production

The authored bank contains six regional/Guardian tracks, three ambient loops and
23 cues. The code provides regional crossfades, spatial water/world effects,
state ducking, shared volume controls and bounded reusable sources. Original
waveform/noise/note generation is reproducible in the audio authoring script.

Finishing fixes address Guardian-music release, immediate ambient mute, jump/
landing classification, recovered-player hit placement, session-cue cleanup
and paused pickup feedback. Attack-use hooks remain a production-player
integration step. Mix balance, spatial feel and timing are awaiting audition.

## Optimization and stability evidence

The combined command runs 15 actual pure C# suites, namespace/source checks and
scene/asset integrity checks. Its current
[machine-readable report](../jehad/versions/Jehad_V1.27.validation.json) records
test inputs, outputs and scope. It does not compile Unity APIs or execute physics.

The counter-text formatter has an isolated .NET allocation comparison. Foliage,
LOD and pool strategies are implemented but not natively measured here. Actual
hardware, target framerate, CPU/GPU/GC captures, bottleneck analysis and measured
build improvement remain pending; [Performance](../jehad/PERFORMANCE.md) defines
the capture method without substituting model results for game measurements.

| Priority | Item | Implementation/evidence state |
|---|---|---|
| Release gate | Production player and input-driven complete loop | Pending integration/native run |
| Release gate | Unity import/compile and stable Windows build | Not run in this environment |
| P1 | Missing movement-audio namespace | Source corrected; native compile still required |
| P2 | Immediate-pause relic summary and pickup-feedback loss | Source corrected; combined native fixture supplied, not run |
| P2 | Chest reveal timing, fall hit position and title jingle carryover | Source corrected; native fixtures/audition pending |
| P3 | Repeated unchanged counter-string formatting | Cache implemented; managed microbenchmark passed, native impact pending |

Record new observed bugs with steps, severity, fix commit and native retest.
No fabricated crash-free run, tester feedback or profiling result is included.

## Presentation and repository delivery

The handoff entry point is [docs/jehad/README.md](../jehad/README.md). It links the
contribution, acceptance route, integration and performance notes. The root
README links the combined scene. Version snapshots, Unity metadata, licenses
and asset provenance are retained; generated build/test cache files are excluded
from the change package.

The final demo should use the verified release build and matching fallback
recording after the pending gates pass. The walkthrough must demonstrate real
input, a copied power, an optional discovery, retry and the Guardian/Heart
ending. No demo recording or audience playtest results were supplied here.

## Postmortem and remaining team work

Versioned review scenes and explicit owners reduced interference with the team
release scene. Pure policies made deterministic state/layout checks practical.
The missed namespace demonstrated why syntax/model checks alone are insufficient;
the newer namespace audit broadens coverage but native compilation remains
essential. Visual/audio tuning and performance sign-off depend on the integrated
build rather than authoring intent.

Checkpoint commits were delivered directly to main at the user's request. They
do not establish teammate PR review or complete graded issue/milestone records.
The team should verify those records, execute the
[acceptance matrix](../jehad/ACCEPTANCE.md), attach actual profiler/playtest/media
evidence and publish the final build/tag. Team-member retrospectives can be
added after integration; no views are attributed to unconsulted teammates.

Report structure checked against the [current Phase 3 description](https://github.com/gamedevkfupm/swe402/blob/main/project/Phase3_Final_Build_Description.md)
on 23 November 2026. This is a documented candidate, not a final-build certificate.
