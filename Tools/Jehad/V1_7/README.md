# V1.7 UI navigation and display checkpoint

From repository root, with Python 3.10+, PyYAML, tree-sitter,
tree-sitter-c-sharp and .NET SDK 8 available:

```sh
python Tools/Jehad/V1_7/build_checkpoint.py --manifest /tmp/jehad-v17-manifest.json
python Tools/Jehad/V1_7/validate_checkpoint.py --manifest /tmp/jehad-v17-manifest.json --dotnet /absolute/path/to/dotnet --report /tmp/jehad-v17-validation.json
```

The compiled pure C# test can also run independently:

```sh
dotnet run --project Tools/Jehad/V1_7/Tests/UiPolicyTests.csproj --configuration Release
```

Expected: **1,570 assertions** against actual UiPolicyV17, UiNavigationV17,
UiDisplayEditV17, reused UiValuesV16/UiDisplayTimerV16 and actual CoreTypes.
C# 9.0, warnings as errors, no NuGet packages. These tests exclude Unity APIs.

The builder clones V1.6 and replaces only five owned UI/typed-harness script
references. It reuses unchanged V1.6 graphic primitives and creates deterministic
metadata through the existing V1.2 helper. No build settings or earlier scene
is changed. For a partial staging directory, `--base-tree-json` accepts the
remote recursive Git tree so existing remote folder metadata is preserved.

The validator checks scene references, serialized fields, C# syntax, native
display/focus guards, reference geometry and exact scene preservation outside
the five UI replacements/version labels. `--baseline` optionally accepts a
JSON map of pre-checkpoint relative paths to SHA256 values; only the contract
log may change. It does not establish Unity API compatibility or runtime UI
behavior. New pure-logic tests cover routing, geometric navigation and the
display edit/preview lifecycle, not rendered appearance.

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.7.unity` for actual testing. See
`docs/jehad/versions/Jehad_V1.7.md` for keyboard/controller, dirty settings and
standalone display review. UiReviewHarnessV17/FlowReviewHarnessV17 are supplied
on EditorOnly roots and have not been executed here.
