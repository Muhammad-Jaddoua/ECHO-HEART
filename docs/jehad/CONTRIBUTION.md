# Jehad Alharbi — contribution summary

## Delivered scope

| Area | Implementation |
|---|---|
| World | Semi-open island layout and separate Guardian arena; meadow, forest/amber grove, lake/river, mountain/alpine and grotto landmarks, trails and optional discoveries |
| Environment presentation | Credited Kenney/KayKit meshes and materials, textured nature variants, shared foliage meshes, spatial batches, frustum checks and distant tree LODs |
| Collectibles and secrets | 16 authored relics, two hinged secret chests, proximity prompts, once-per-run collection, discovery notices and nearby pickup sparkles |
| Flow | Title, Playing, Paused, Dead and Complete; scoped retries, fall recovery, Guardian entry and boss-gated Heart restoration |
| Menus/UI | Scenic title, pause/death/completion menus, settings/controls/credits, focus/navigation, safe-area layout, display preview/revert, audio/interface preference storage, HUD presentation and objective bearing |
| Audio | Six environment/Guardian tracks, three ambient loops, 23 world cues, spatial water/effects, regional crossfades, pause/death ducking and shared volume options |
| Feedback | Actor hit/capture/defeat effects, Guardian cues, chest/relic/secret particles, jump/landing filtering, recovery hit placement and bounded discovery notices |

The production player/inhale/ability holder and final scene/build integration
remain Tariq's team scope. Combat, health, enemy/boss AI and shared contracts
remain Muhammad's scope. Jehad's adapters use those contracts and do not replace
their damage, capture, attack or reward authorities.

## Finishing changes carried by the combined checkpoint

| Issue | Resolution in the checkpoint history | Native status |
|---|---|---|
| Guardian music override remained after defeat | Release the override into the original regional crossfade | Pending audition |
| Ambient loops faded instead of immediately honoring zero SFX | Stop only muted ambient sources through the existing volume owner | Pending audition |
| Reward available before a chest lid cleared | Gate the original reward until 95% of the scaled lid animation | Pending physics/visual review |
| Ledge departure sounded like a jump | Require upward departure and meaningful landing samples | Pending motor review |
| Movement presenter lacked the audio namespace | Add the missing import and a repeatable namespace audit | Native compile pending |
| Fall hurt feedback stayed at the old below-world point | Anchor distant player impact presentation at the recovered actor | Pending native review |
| Session jingle carried onto title | Stop the dedicated stinger on title entry | Pending audition |
| Unchanged counter labels allocated new strings | Reuse one formatted string per label value | Managed formatter measured; Unity profiling pending |
| Immediate pause could show a stale relic count | Refresh the existing counter before publishing Paused | Pending native timing review |
| Pause before feedback LateUpdate could consume a pickup silently | Hold its presentation snapshot during pause; process it on resume | Pending V1.27 native review |

V1.27 also changes the chest notice to “A hidden relic waits inside.” so it does
not announce an immediate reveal while the lid is still opening.

## Evidence and limitations

The combined command compiles actual pure C# sources across 15 suites and checks
scene YAML/GUIDs, referenced audio headers, license-file presence and source
scope. The generated [validation report](versions/Jehad_V1.27.validation.json)
records exact test inputs, outputs and the current source digest.

These checks do not compile Unity APIs, prove rendering/audio quality or replace
native physics/input tests. The reviewed main branch still lacks production
player sources/prefabs. No Windows release candidate, native profiler capture,
three-person playtest results or matching fallback video was supplied to this
closeout. Their status is recorded in [Acceptance](ACCEPTANCE.md).

## Asset and implementation provenance

Kenney Nature Kit and Kay Lousberg's Forest Nature Pack retain their included
CC0 license files and provenance records. See [Asset credits](../ASSET_CREDITS.md).
The terrain/layout, water/chest presentation, UI graphics and synthesized audio
are project work. Music/ambience/SFX use deterministic note/waveform/noise
generation in `Tools/Jehad/V1_5/build_audio.py`; third-party nature geometry is
credited to its original authors.

## Implementation lessons

Versioned review scenes kept the integrator's release scene isolated. Small
adapters preserved existing health, retry and audio ownership. Pure policies
made state/layout behavior reproducible without the Editor.

The missing namespace exposed the limit of syntax-only checks; the later
namespace audit improves coverage but remains short of native compilation.
Final visual/audio tuning and performance conclusions require the actual Unity
build. Checkpoint delivery used direct main commits as requested; those commits
are not evidence of teammate PR review or the course's graded review workflow.
