# V1.8 title layout and menu focus

Requirements: Python 3.10+, PyYAML, tree-sitter, tree-sitter-c-sharp, .NET SDK 8.
From repository root:

```sh
python Tools/Jehad/V1_8/build_checkpoint.py --manifest /tmp/jehad-v18-manifest.json
python Tools/Jehad/V1_8/validate_checkpoint.py --manifest /tmp/jehad-v18-manifest.json --dotnet /absolute/path/to/dotnet --report /tmp/jehad-v18-validation.json
```

Independent checks:

```sh
python Tools/Jehad/V1_8/check_title_layout.py
dotnet run --project Tools/Jehad/V1_8/Tests/UiPolicyTests.csproj --configuration Release
```

The pure C# test expects **1,586 assertions**: 16 for actual V1.8 focus history,
plus the existing 1,570 V1.7 navigation/routing/display and V1.6 value/timer
checks against actual CoreTypes. No NuGet packages or Unity API stubs are used.
It does not compile Unity APIs or execute the MonoBehaviour input code.

The title check parses the actual C# argument syntax for direct title labels
and buttons. It reproduces the old Quit/footer rectangle overlap, then checks
new rectangles for card containment and button/text/button intersections. It
does not simulate Unity transforms, fonts or rendering.

The deterministic builder clones V1.7, replacing only FlowMenu, FlowReviewHarness
and UiReviewHarness script references. Settings/HUD/factory/toast remain reused.
For partial staging, `--base-tree-json` accepts the remote recursive Git tree to
preserve absent local folder metadata. Validation includes exact scene
preservation after those three replacements and version-label normalization.
`--baseline` accepts a pre-checkpoint path→SHA256 map; only the contract log
may change among those files.

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.8.unity` for engine review. Instructions
and unexecuted UI/flow harnesses: `docs/jehad/versions/Jehad_V1.8.md`.

Input System API reference used for the owned module's background-click option:
https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html
