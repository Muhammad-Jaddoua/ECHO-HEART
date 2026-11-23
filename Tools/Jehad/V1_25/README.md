# V1.25 counter-text caching

Requirements: Python 3.10+, PyYAML, tree-sitter, tree-sitter-c-sharp and .NET SDK 8.
Run from the repository root:

```sh
python Tools/Jehad/V1_25/build_checkpoint.py --manifest /tmp/jehad-v125-manifest.json
dotnet run --project Tools/Jehad/V1_25/Tests/CountTextTests.csproj --configuration Release | tee /tmp/jehad-v125-tests.txt
dotnet run --project Tools/Jehad/V1_22/NamespaceAudit/NamespaceAudit.csproj --configuration Release -- "$PWD" /tmp/jehad-v125-namespaces.json
python Tools/Jehad/V1_25/validate_checkpoint.py --manifest /tmp/jehad-v125-manifest.json --namespace-report /tmp/jehad-v125-namespaces.json --compiled-test-output /tmp/jehad-v125-tests.txt --report /tmp/jehad-v125-validation.json
```

On Windows use your absolute repository path in place of `"$PWD"` and save the
test output with PowerShell `Tee-Object` or normal shell redirection. Check the
test process exit code in addition to its PASS message.

Expected: **123 assertions** against the actual CountTextV125 source under
C# 9.0/.NET 8, warnings as errors, no Unity stubs or NuGet packages. The test
also emits JSON with allocated-byte measurements for 10,000 unchanged-number
calls after warmup. It compares the cache with the exact old formatting
expressions. Baseline byte counts vary; warmed cache calls must allocate zero.
This is a managed formatter microbenchmark, not Unity profiling.

The builder copies V1.24 with version-label changes and creates the helper's
Unity metadata. No scene component or serialized binding changes. Its second
run should write zero files. Partial staging may pass `--base-tree-json` with
the remote recursive tree to preserve remote-only folder metadata.

The validator checks 135 scene YAML documents, the two exact cache call-site
changes, formatting/allocation evidence and the current namespace source digest.
Reversing the cache fields and calls must reproduce both original UI sources;
reversing version labels must reproduce V1.24.

Optional `--baseline` accepts path-to-SHA256 JSON and permits only WorldToastV110,
FlowMenuV117 and the contract log to change among earlier files. Optional
`--base-tree-json` compares all earlier project source blobs after reversing the
two call-site edits. The namespace audit's alias/nested-type/symbol-binding
limitations remain documented in Tools/Jehad/V1_22/README.md.

The shared UI classes also use the cache in earlier scenes that reference them;
their scene files and visible wording are preserved. Unity compilation, UI
interaction, native GC allocations and frame-time impact still need Editor/build
verification. Follow `docs/jehad/versions/Jehad_V1.25.md` for the review steps.
