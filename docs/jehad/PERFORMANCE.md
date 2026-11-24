# Performance work and evidence boundary

| Implemented strategy | Source-level purpose | Native measurement |
|---|---|---|
| Shared foliage meshes and spatial batches | Avoid duplicated grass geometry; bound draw groups | Pending |
| Cached foliage matrices/frustum planes and per-camera bounds checks | Reuse transforms and skip off-screen batches | Pending |
| Distant tree LOD meshes | Reduce distant geometry | Pending |
| Eight reusable world SFX voices | Bound simultaneous world-source growth | Pending |
| Twelve burst systems and six nearby sparkle slots | Bound particle-system growth | Pending |
| Counter-label string reuse | Avoid formatting identical numbers every visible frame | Isolated managed formatter measured |

The V1.25 formatter test is rerun by the combined command. It measures 10,000
warmed calls with an unchanged positive number using the actual CountTextV125
class and the original formatting expressions. The cached calls allocate zero
new managed bytes in the .NET 8 test process; the prior expressions allocate
new strings. Exact baseline bytes are in the current
[validation report](versions/Jehad_V1.27.validation.json) and depend on the
formatted number/runtime.

That result covers formatting only. It is not Unity UI allocation, total frame
cost, GPU performance or evidence of a stable game framerate. Construction,
changed values and other Unity/UI work can allocate.

## Required native capture

Record the tested commit/build, Unity version, CPU, GPU, RAM, resolution,
quality/VSync/frame-limit settings and a declared target. Capture consistent
workloads: dense foliage traversal, lake/river view, a busy encounter, Guardian
telegraphs/projectiles, queued notices and the display-preview modal. Record
CPU/GPU frame time, GC allocation call stacks and relevant rendering counters.

For the counter-cache comparison, use separate checkouts of the pre-cache
V1.24 commit `478d9c5d1a386b8d977e0124d85e127ff379831e` and this checkpoint.
Older scenes in the current checkout also use the updated shared renderers.
Hold hardware/build/settings and workload constant. Attribute a measured change
to its call site, then save before/after captures with the fix description.

**Current native results:** not provided. No FPS, CPU/GPU time, native GC
improvement or optimization success is asserted for the Unity build. The
isolated formatter result does not replace the course's build profiling evidence.
