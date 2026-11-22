# V1.9 responsive interface checkpoint

Requirements: Python 3.10+, PyYAML, tree-sitter, tree-sitter-c-sharp and .NET SDK 8.
Run from repository root:

```sh
python Tools/Jehad/V1_9/build_checkpoint.py --manifest /tmp/jehad-v19-manifest.json
python Tools/Jehad/V1_9/validate_checkpoint.py --manifest /tmp/jehad-v19-manifest.json --dotnet /absolute/path/to/dotnet --report /tmp/jehad-v19-validation.json
```

Independent compiled tests:

```sh
dotnet run --project Tools/Jehad/V1_9/Tests/LayoutTests.csproj --configuration Release
dotnet run --project Tools/Jehad/V1_8/Tests/UiPolicyTests.csproj --configuration Release
```

Expected: **335** assertions for actual UiLayoutV19 and **1,586** existing
menu/focus/settings assertions. The new suite covers 11 full/inset screen
configurations, menu-card fitting, 85–115% HUD scaling, common HUD/toast
surface bounds and prompt/cluster spacing. Tests compile C# 9.0 with warnings
as errors, no Unity API stubs or external NuGet packages. They exclude actual
MonoBehaviour execution, CanvasScaler updates and Unity rendering/input.

The builder clones V1.8, replaces four UI/typed-review script references, and
adds the UI harness's toast-skin reference. Metadata uses the existing
deterministic V1.2 authoring helper. For partial staging, `--base-tree-json`
accepts the remote recursive Git tree to preserve remote-only folder metadata.
No previous scene or build setting changes.

Validation checks C# syntax, serialization/GUID/local references, scene
preservation, shared-layout guards and title rectangles using the V1.8 parser
against actual MenuScreensV19. `--baseline` optionally accepts relative paths
mapped to pre-checkpoint SHA256 digests; only the existing contract log may
change. Re-running the builder should report zero files written.

Review `Assets/_Project/Scenes/Jehad/Jehad_V1.9.unity` in Unity. Manual review and
unexecuted harness instructions: `docs/jehad/versions/Jehad_V1.9.md`.
