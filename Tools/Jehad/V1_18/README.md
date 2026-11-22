# V1.18 post-Guardian music resolution

Requirements: Python 3.10+, PyYAML, tree-sitter, tree-sitter-c-sharp and .NET SDK 8.
From the repository root:

```sh
python Tools/Jehad/V1_18/build_checkpoint.py --manifest /tmp/jehad-v118-manifest.json
python Tools/Jehad/V1_18/validate_checkpoint.py --manifest /tmp/jehad-v118-manifest.json --dotnet /absolute/path/to/dotnet --report /tmp/jehad-v118-validation.json
```

The builder copies V1.17 and adds a scene resolver plus an EditorOnly review
component to existing roots. It preserves all earlier serialized component
fields. Re-running should write zero files. Partial staging may pass
`--base-tree-json` with the recursive remote Git tree to preserve remote-only
folder metadata.

WorldAudioV15 gains one optional method, EndGuardianMusic. The validator removes
that method and checks the result against the verified pre-change source hash;
all existing code and fields must match exactly. Older scenes have no resolver
and keep their previous behavior. The existing audio, particle, chest, Guardian
and UI components retain their types and serialized references.

Static validation covers two new C# syntax trees and the extended audio source,
126 scene YAML documents, references, event cleanup, unchanged audio pools,
scoped source changes and prior-scene preservation. `--baseline` accepts a
relative-path to SHA256 mapping; only WorldAudioV15.cs and the contract log may
change among earlier files.

With `--dotnet`, ten existing pure C# suites run **8,709 assertions**: the
6,335 region/mix/chest assertions plus the previous 2,374 presentation/UI
assertions. These validate reused models, including continuous music weights;
they do not execute the new Unity hook, event delivery or audio playback.

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.18.unity` for native review.
See `docs/jehad/versions/Jehad_V1.18.md` for the real-boss context action and
listening cases. Unity compilation, playback, supplied context actions and
profiling remain separate.
