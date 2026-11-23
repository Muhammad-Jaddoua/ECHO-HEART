# Combined Jehad closeout checks

Requirements: Python 3.10+, PyYAML, tree-sitter, tree-sitter-c-sharp and .NET SDK 8.
From the repository root:

```sh
python Tools/Jehad/V1_27/run_checks.py --dotnet /absolute/path/to/dotnet --report /tmp/jehad-v127-validation.json
```

Use the actual SDK executable path, including `dotnet.exe` on Windows. The
command invokes programs through argument lists without a shell, writes child
evidence beside the chosen report, checks every exit code and returns nonzero
on failure. No NuGet packages or Unity stubs are required.

The command builds scene/metadata twice to check idempotence, runs 15 pure C#
suites (9,436 assertions), executes the existing V1.22 namespace audit, then
checks scene/source/asset integrity and handoff links. Each model result records
hashes of its project and explicit compiled inputs to reject stale evidence.
The counter-text allocation microbenchmark remains .NET formatting evidence only.

It does not launch Unity, build the game, import assets, run native context
actions, listen to audio, profile frame times, create playtest results or publish
a release. Namespace auditing is not full C# symbol/API binding; see V1.22's
tool documentation for its limits.

Optional `--base-tree-json` accepts the inspected remote recursive tree. In a
partial staging directory it preserves remote-only folder metadata and enables
scope/blob checks. Optional `--baseline` accepts a path-to-SHA256 map and permits
only WorldFeedbackV15, WorldNoticesV110, the root README checkpoint section and
the contract log to change among earlier files.

The builder adds one EditorOnly JehadFinalReviewV127 component to the existing
review root. Normal runtime owners/bindings are preserved. The review action
temporarily holds counter/feedback updates, collects through the real trigger,
checks immediate-pause counts and one deferred relic-sound request, then restores
the prior enabled states and observers. Run context actions separately.

The [handoff index](../../../docs/jehad/README.md) provides the native acceptance
route, integration bindings, contribution/performance notes and final report.
