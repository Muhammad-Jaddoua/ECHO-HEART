# V1.26 current relic summary on pause

Requirements: Python 3.10+, PyYAML, tree-sitter, tree-sitter-c-sharp and .NET SDK 8.
Run from the repository root:

```sh
python Tools/Jehad/V1_26/build_checkpoint.py --manifest /tmp/jehad-v126-manifest.json
dotnet run --project Tools/Jehad/V1_3/Tests/FlowPolicyTests.csproj --configuration Release | tee /tmp/jehad-v126-tests.txt
dotnet run --project Tools/Jehad/V1_22/NamespaceAudit/NamespaceAudit.csproj --configuration Release -- "$PWD" /tmp/jehad-v126-namespaces.json
python Tools/Jehad/V1_26/validate_checkpoint.py --manifest /tmp/jehad-v126-manifest.json --namespace-report /tmp/jehad-v126-namespaces.json --compiled-test-output /tmp/jehad-v126-tests.txt --report /tmp/jehad-v126-validation.json
```

On Windows use your absolute repository directory in place of `"$PWD"` and save
test output with PowerShell `Tee-Object` or normal redirection. Check the test
process exit code in addition to its PASS message.

Expected: **445 existing assertions** against actual SessionPolicyV13/CoreTypes,
C# 9.0/.NET 8, warnings as errors and no Unity stubs. These cover the pure flow
guards/transitions. They do not run Unity's SessionFlowV13.Pause or establish
counter timing. The existing V1.22 namespace-audit tool checks the current source
digest and known project type-name imports; its symbol-binding limitations are
documented in that tool's README.

The builder copies V1.25, adds one EditorOnly PauseRelicReviewV126 component and
updates version labels. No runtime component or earlier serialized field is
changed. Its second run should write zero files. Partial staging may pass
`--base-tree-json` with the remote recursive tree to preserve folder metadata.

The validator checks 136 scene YAML documents, references, the exact Pause
branch edit and the refresh-before-state-publication order. Reversing that edit
must reproduce the original flow source. Reversing the new review component and
version labels must reproduce V1.25. Original counter polling/no-duplicate guard,
pickup/menu ownership and all other flow methods remain unchanged.

Optional `--baseline` accepts path-to-SHA256 JSON and permits only SessionFlowV13
and the contract log to change among earlier files. Optional `--base-tree-json`
compares every earlier project source blob after reversing the Pause edit.

The new context action temporarily holds counter polling, collects through the
real trigger and verifies Pause corrects the stale count before publishing its
state. It checks the visible summary and restores polling/event subscriptions
in finally. Run it separately from other actions; it resets the run. Unity
compilation, trigger behavior, native event timing and UI playback still need
verification. Steps: `docs/jehad/versions/Jehad_V1.26.md`.
