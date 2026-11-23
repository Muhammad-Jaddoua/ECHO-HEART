# Jehad V1.25 — reuse unchanged counter text

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.25.unity` in Unity 6000.3.27f1.

The source audit found two numeric labels formatting new strings every visible
frame: the queued-notice count in WorldToastV110 and the display-preview
countdown in FlowMenuV117. Their numbers usually stay unchanged across many
frames, so those temporary strings are avoidable.

CountTextV125 keeps one formatted string per label and rebuilds it when the
number changes. The toast still shows `N more` only for a positive pending
count. The display modal still uses the same rounded-up seconds and the same
two-line wording. Each owner creates one cache when constructed; the cache
retains only its latest number/string. UI update frequency, queue reading time,
preview timeout, confirmation delay, focus/input and settings behavior are
preserved. No gameplay state, audio, reward, camera or display-setting owner is
changed.

This scene differs from V1.24 only in version labels and its own scene GUID.
No component or serialized binding is added. Earlier scene files are preserved;
their shared toast renderer also gets the same cache optimization, and scenes
using FlowMenuV117 get the countdown cache. The visible wording is preserved.

## Managed formatting measurement

The actual cache source compiles under C# 9.0/.NET 8 with warnings as errors and
no Unity stubs. **123 assertions** cover queue counts 0–31, countdown values
0–15, decreasing countdowns, clearing the queue label, integer edges, stable
string-reference reuse and allocation behavior after warmup. Output is compared
against the exact pre-change formatting expressions.

This run measured 10,000 calls with an unchanged positive number, excluding
construction and warmup:

| Formatting path | Previous expression | Cached formatter |
|---|---:|---:|
| Display-preview countdown | 1,680,000 bytes | 0 bytes |
| Pending-toast count | 400,000 bytes | 0 bytes |

These are managed allocations from the isolated formatting functions on .NET 8.
They are not Unity UI, total game allocation, FPS or frame-time measurements.
Changed numbers still build a new string. Exact baseline byte counts can vary
with the runtime and formatted number; the checks require a positive baseline
and zero allocation for warmed unchanged cache calls.

## Unity review and profiling

1. Open V1.25 and check Console compilation. Inspect normal toast text, multiple
   queued notices, objective interruptions, pause/resume and queue clearing on
   retry/title/New Game. Confirm the pending label updates as notices advance.
2. From Settings, stage a display change and choose Apply. Inspect each countdown
   second, automatic revert, manual Revert, Keep and reopening another preview.
   Check keyboard/controller focus and the existing confirmation delay.
3. Use separate checkouts of the V1.24 commit
   `478d9c5d1a386b8d977e0124d85e127ff379831e` and this checkpoint with the same
   Unity version/build configuration and hardware. Earlier scene files in the
   current checkout already use the updated shared renderers. Record the
   Profiler's GC allocation call stacks while a
   pending toast count is stable and while the display-preview modal is open.
   Inspect the formatter call sites rather than attributing every UI allocation
   to them. Other UI/Unity work can still allocate.
4. Record actual build frame times and any observed defects separately. This
   formatter measurement does not replace the required Unity profiling evidence
   or establish a whole-game performance improvement.

## Verification

The namespace audit indexes 158 project scripts and checks 122 Jehad scripts
under Editor and Player symbols, with no missing-import candidates or syntax
errors. It remains a focused source audit, not Unity compilation or full C#
symbol binding; see Tools/Jehad/V1_22/README.md for its limits.

Static checks cover the new helper and two edited UI syntax trees, 135 scene
YAML documents, references and one listener. Reversing the two cache fields and
format calls reproduces the verified original UI sources exactly. Reversing
version labels reproduces V1.24 exactly. All 157 earlier project source blobs
match the inspected remote tree after that source reversal. The 1,052-file
staged baseline changes only in the two UI sources and contract log. The scene
builder is idempotent.

Not run: Unity import/compilation, actual UI rendering/interaction, native
allocation/frame-time profiling, standalone playthrough or visual review. No
Unity Editor is connected. The passing formatter/source checks do not establish
final-submission readiness.

Evidence: `Jehad_V1.25.validation.json`. Commands: `Tools/Jehad/V1_25/README.md`.
Next: inspect the labels and capture Unity allocation call stacks during the
world/menu playthrough.
