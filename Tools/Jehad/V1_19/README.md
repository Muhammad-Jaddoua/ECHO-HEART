# V1.19 immediate ambience mute

Requirements: Python 3.10+, PyYAML, tree-sitter, tree-sitter-c-sharp and .NET SDK 8.
From the repository root:

```sh
python Tools/Jehad/V1_19/build_checkpoint.py --manifest /tmp/jehad-v119-manifest.json
python Tools/Jehad/V1_19/validate_checkpoint.py --manifest /tmp/jehad-v119-manifest.json --dotnet /absolute/path/to/dotnet --report /tmp/jehad-v119-validation.json
```

The builder copies V1.18 and adds a settings bridge and EditorOnly review
component to existing roots. Earlier serialized fields and scene roots stay
unchanged. Re-running should write zero files. Partial staging may pass
`--base-tree-json` with the remote recursive tree to preserve remote-only
folder metadata.

WorldAudioV15 gains an optional SilenceMutedAmbience method and its private
source helper. Removing those two methods must reproduce the verified V1.18
source exactly, including the existing Guardian resolution hook. The new
bridge calls the optional API on settings changes and enable, with no per-frame
work. Older scenes do not contain that bridge.

Validation covers two new C# syntax trees plus the extended audio file,
128 scene YAML documents, shared settings/volume references, event cleanup,
the three-source scope and preservation of earlier scene/source behavior.
`--baseline` permits only WorldAudioV15.cs and the contract log to change among
earlier files.

With `--dotnet`, the ten existing pure C# model suites run **8,709 assertions**.
They cover reused music/mix/chest and UI/presentation policies. They do not
execute the new Unity hook or assess audible muting. No new pure test project
is added for the native audio operation.

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.19.unity` for engine review.
See `docs/jehad/versions/Jehad_V1.19.md` for the context action and listening
checks. Unity compilation, playback, context actions and profiling remain
separate.
