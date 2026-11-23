# V1.11 Guardian presentation checkpoint

Requirements: Python 3.10+, PyYAML, tree-sitter, tree-sitter-c-sharp and .NET SDK 8.
From a full repository checkout:

```sh
python Tools/Jehad/V1_11/build_checkpoint.py --manifest /tmp/jehad-v111-manifest.json
python Tools/Jehad/V1_11/validate_checkpoint.py --manifest /tmp/jehad-v111-manifest.json --dotnet /absolute/path/to/dotnet --report /tmp/jehad-v111-validation.json
```

Independent new test:

```sh
dotnet run --project Tools/Jehad/V1_11/Tests/GuardianTests.csproj --configuration Release
```

Expected: **51 new cue/geometry assertions**. The validator also runs the
existing **111** notification, **335** layout and **1,586** menu/focus/settings
assertions. Tests compile actual pure C# sources with C# 9.0/warnings as errors,
no Unity stubs or NuGet dependencies. They do not execute BossBrain, native
audio, LineRenderer, physics or MonoBehaviour lifecycle code.

The deterministic builder clones V1.10, adds the Guardian presentation and
EditorOnly review components to existing roots, and clears the old feedback
component's arena reference. It does not change the boss prefab/definition/AI
or earlier versions. Re-running should write zero files. For partial staging,
`--base-tree-json` accepts the recursive remote Git tree. The validator's
`--reference-json` can supply missing reference files as a map from repository
path to `{content, sha}`; it verifies their exact Git blob hashes. In a full
checkout the original local files are used directly.

Validation checks scene preservation, serialized references, passive
presentation, dedicated channel/pause guards, and the original prefab's
Muzzle binding and empty cue hooks. `--baseline` accepts pre-checkpoint
relative-path→SHA256 values; only the contract log may change among them.

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.11.unity` in Unity. See
`docs/jehad/versions/Jehad_V1.11.md` for the Guardian harness, actual ranged/
slam/cover cases, presentation-owner delegation and outstanding engine tests.
