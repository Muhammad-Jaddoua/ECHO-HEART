# Jehad V1.27 — combined closeout checkpoint

This single checkpoint combines the remaining authoring fixes, source/model
verification, cleanup and contribution/report handoff. Use
[the handoff index](../README.md) instead of scheduling separate V1.28/V1.29
authoring batches. Native acceptance and production integration remain gates.

## Changes

- WorldFeedbackV15 preserves a pending collectible presentation snapshot while
  paused, allowing its existing sound and particle burst to run once on resume.
  Playing and terminal states still update the snapshot normally; a retry
  refresh can still capture it silently. This fixes a pickup consumed by paused
  LateUpdate before feedback had been presented.
- The chest notice says “A hidden relic waits inside.”, compatible with delayed
  lid reveal and legacy immediate-reveal scenes.
- JehadFinalReviewV127 combines a real pickup, current pause summary and deferred
  relic-sound request check. It temporarily holds counter/presentation updates,
  uses the original trigger and restores prior enabled states/listeners in
  finally. It is EditorOnly and does not award a substitute reward.
- One command runs all 15 pure C# suites, namespace audit and scene/asset checks.
  Contribution, integration, performance, acceptance and Phase 3 report documents
  are linked from the root README and the handoff index.

No new runtime component, gameplay feature, clip, pool, art asset or display
setting is added. V1.27 copies V1.26, changes version labels and adds one
EditorOnly review component. Existing serialized bindings are preserved.
Shared world-source fixes apply to earlier scenes; their scene files are intact.

## Verification

The combined report records 15 existing pure suites totaling 9,436 assertions,
their exact input hashes, the current namespace/source digest, scene structure,
32 referenced WAV/OGG headers and nature-pack license/credit presence. Model tests
exercise actual pure sources under C# 9.0/.NET 8; none compile Unity APIs or
demonstrate native rendering/audio/physics. The managed formatter benchmark is
included with its restricted scope.

Static reversal checks isolate the two world-source changes, README section and
new EditorOnly component. The release scene, build settings and all other
existing remote files are preserved by the publication tree comparison.

The current review pawn is still the movement-only capsule. Native compilation,
acceptance actions, visual/audio tuning, CPU/GPU/GC profiling, production-player
integration, structured playtests and release/media evidence are not completed
by the CLI checks. Their exact status is in the acceptance matrix and report.

Evidence: [Jehad_V1.27.validation.json](Jehad_V1.27.validation.json).  
Reproduction: [tool README](../../../Tools/Jehad/V1_27/README.md).  
Native review: [acceptance route](../ACCEPTANCE.md).
