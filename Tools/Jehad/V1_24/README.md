# V1.24 title-entry session-cue cleanup

Requirements: Python 3.10+, PyYAML, tree-sitter, tree-sitter-c-sharp and .NET SDK 8.
Run from the repository root:

```sh
python Tools/Jehad/V1_24/build_checkpoint.py --manifest /tmp/jehad-v124-manifest.json
dotnet run --project Tools/Jehad/V1_22/NamespaceAudit/NamespaceAudit.csproj --configuration Release -- "$PWD" /tmp/jehad-v124-namespaces.json
python Tools/Jehad/V1_24/validate_checkpoint.py --manifest /tmp/jehad-v124-manifest.json --namespace-report /tmp/jehad-v124-namespaces.json --report /tmp/jehad-v124-validation.json
```

On Windows replace `"$PWD"` with the absolute repository directory. The audit
uses Roslyn assemblies bundled with the .NET SDK. No NuGet packages or Unity
stubs are used, and the Unity game is not compiled. See the V1.22 tool README
for alias, nested-type and symbol-binding limitations.

The builder copies V1.23, adding the title audio bridge and EditorOnly review
component to existing roots. It changes no previous serialized field. Its
second run should write zero files. Partial staging may pass `--base-tree-json`
with the remote recursive tree to preserve remote-only folder metadata.

The validator checks 135 scene YAML documents, references and the exact source
extension. Removing the new hook must reproduce the original WorldAudioV15;
removing the two new components/version labels must reproduce V1.23. Only the
new scene's bridge may call the hook. Existing music, world-voice, ambient and
volume logic must remain unchanged.

Optional `--baseline` accepts path-to-SHA256 JSON and permits only the audio
source and contract log to change among earlier files. Optional
`--base-tree-json` verifies every earlier project source blob after reversing
the optional method extension. The namespace audit report must match the
current source digest.

No pure policy changed, so no new policy suite was added and older gameplay
suites were not rerun. Open `Assets/_Project/Scenes/Jehad/Jehad_V1.24.unity`
for native review. Run the new audio context action separately: it resets the
run and exercises real death, retry, Guardian defeat and Heart completion.
Unity compilation, actual source playback and sound quality remain pending.
Manual instructions are in `docs/jehad/versions/Jehad_V1.24.md`.
