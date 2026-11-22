# V1.12 objective guide checkpoint

Requirements: Python 3.10+, PyYAML, tree-sitter, tree-sitter-c-sharp, .NET SDK 8.
From repository root:

```sh
python Tools/Jehad/V1_12/build_checkpoint.py --manifest /tmp/jehad-v112-manifest.json
python Tools/Jehad/V1_12/validate_checkpoint.py --manifest /tmp/jehad-v112-manifest.json --dotnet /absolute/path/to/dotnet --report /tmp/jehad-v112-validation.json
```

Independent new test:

```sh
dotnet run --project Tools/Jehad/V1_12/Tests/GuideTests.csproj --configuration Release
```

Expected: **80 guide assertions**, plus the existing **51** Guardian, **111**
notification, **335** layout and **1,586** menu/focus/settings assertions when
running the validator with `--dotnet`. Tests compile actual pure C# sources,
CoreTypes and shared layout under C# 9.0 with warnings as errors. No Unity
stubs or external NuGet dependencies. Camera matrices and native UI rendering
remain outside these tests.

The deterministic builder clones V1.11, adding the guide and EditorOnly review
components to existing roots. No existing component fields, gameplay objects,
build settings or earlier versions change. Re-running should write zero files.
For partial staging, `--base-tree-json` accepts the recursive remote Git tree
to preserve remote-only folder metadata.

Validation checks serialization/GUIDs, authored target/camera references,
scene preservation, hints/prompt/projection guards and passive runtime code.
`--baseline` optionally accepts relative-path→pre-checkpoint SHA256 values;
only the contract log may change among those files.

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.12.unity` for actual engine review.
See `docs/jehad/versions/Jehad_V1.12.md` for the Objective harness and manual
camera/visibility/interaction cases.

Official projection API references consulted:

- https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Camera.WorldToScreenPoint.html
- https://docs.unity3d.com/6000.3/Documentation/ScriptReference/RectTransformUtility.ScreenPointToLocalPointInRectangle.html
