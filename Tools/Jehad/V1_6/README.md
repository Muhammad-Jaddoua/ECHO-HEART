# V1.6 complete UI checkpoint

Use Python 3.10+, apply_patch, PyYAML, tree-sitter and tree-sitter-c-sharp.
From repository root:

```sh
python Tools/Jehad/V1_6/build_checkpoint.py --manifest /tmp/jehad-v16-manifest.json
python Tools/Jehad/V1_6/validate_checkpoint.py --manifest /tmp/jehad-v16-manifest.json --report /tmp/jehad-v16-static.json
dotnet run --project Tools/Jehad/V1_6/Tests/UiPolicyTests.csproj --configuration Release
```

The policy tests require .NET SDK 8, use C# 9.0/warnings as errors, and need no
NuGet packages. Expected output: **873 assertions** for actual UiPolicyV16,
UiValuesV16, UiDisplayTimerV16 and actual CoreTypes. They do not compile Unity APIs.

The authoring script reuses the V1.5 scene. Only its menu presenter and typed
flow harness references are replaced; new settings, toast skin and a UI harness
are added. It generates deterministic metadata using the V1.2 helper. For a
partial staging directory, --base-tree-json accepts a GitHub recursive tree so
absent local metadata for existing remote folders is not recreated.

Static validation checks syntax, serialization, the presenter/flow-harness
replacement, shared references, modal/input guards, reference Expand-canvas
math and HUD spacing. --baseline optionally accepts relative paths mapped to
pre-checkpoint SHA256 digests; only the existing contract log may change.

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.6.unity` for actual UI review. See
`docs/jehad/versions/Jehad_V1.6.md` for all inputs, session settings behavior,
the supplied (not executed here) UiReviewHarnessV16/FlowReviewHarnessV16 context
menus and standalone display keep/revert tests. No Unity Editor is connected.

Official API references consulted:

- https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Screen.SetResolution.html
- https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Screen.safeArea.html
- https://docs.unity3d.com/6000.3/Documentation/ScriptReference/QualitySettings-vSyncCount.html
- https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Application-targetFrameRate.html
- https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-CanvasScaler.html
