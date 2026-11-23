# V1.23 local player hurt feedback

Requirements: Python 3.10+, PyYAML, tree-sitter, tree-sitter-c-sharp and .NET SDK 8.
From the repository root:

```sh
python Tools/Jehad/V1_23/build_checkpoint.py --manifest /tmp/jehad-v123-manifest.json
dotnet run --project Tools/Jehad/V1_23/Tests/HitPointTests.csproj --configuration Release | tee /tmp/jehad-v123-tests.txt
dotnet run --project Tools/Jehad/V1_22/NamespaceAudit/NamespaceAudit.csproj --configuration Release -- "$PWD" /tmp/jehad-v123-namespaces.json
python Tools/Jehad/V1_23/validate_checkpoint.py --manifest /tmp/jehad-v123-manifest.json --namespace-report /tmp/jehad-v123-namespaces.json --compiled-test-output /tmp/jehad-v123-tests.txt --report /tmp/jehad-v123-validation.json
```

On Windows, use your absolute repository path instead of `"$PWD"` and save the
test output with PowerShell `Tee-Object` or the shell's normal redirection. Check
the test process exit code as well as its PASS message.

Expected: **39 assertions**, actual HitFeedbackPointV123 source, C# 9.0, warnings
as errors, no Unity stubs or NuGet packages. The namespace audit is the existing
V1.22 Roslyn source tool; it does not compile the Unity game. It indexes all
project scripts, audits known project type-name references in Jehad scripts
under Editor/Player symbols, and ties the report to the current source digest.
See its README for aliases, nested types and symbol-binding limitations.

The builder copies V1.22 and adds one player-feedback adapter plus one EditorOnly
review action to existing roots. Its second run should write zero files.
Partial staging may pass `--base-tree-json` with a remote recursive tree to
preserve metadata for remote-only folders.

The validator checks scene structure/references, the exact scoped source edits,
default-off compatibility for earlier scenes, shared audio/particle point and
the new review bindings. Optional `--baseline` accepts path-to-SHA256 JSON and
allows only ActorFeedbackV15.cs, WorldFeedbackV15.cs and the contract log to
change among earlier files. Optional `--base-tree-json` compares all earlier
project script blobs after reversing the two scoped edits.

The native context action resets the run and clears existing world-voice clips
between cases. Run it separately from other acceptance actions. Unity compile,
audio, particles, actual damage/recovery and profiling remain separate from the
39 pure-policy assertions and static/source checks. Manual steps are in
`docs/jehad/versions/Jehad_V1.23.md`.
