# V1.14 audio/interface preferences

Requirements: Python 3.10+, PyYAML, tree-sitter, tree-sitter-c-sharp and .NET SDK 8.
From the repository root:

```sh
python Tools/Jehad/V1_14/build_checkpoint.py --manifest /tmp/jehad-v114-manifest.json
python Tools/Jehad/V1_14/validate_checkpoint.py --manifest /tmp/jehad-v114-manifest.json --dotnet /absolute/path/to/dotnet --report /tmp/jehad-v114-validation.json
```

The deterministic builder copies V1.13, rebinds the menu and its two typed
review harnesses, and adds one preference store to the existing settings root.
No scene roots are added. Re-running should write zero files. For partial
staging, `--base-tree-json` takes the recursive remote Git tree to preserve
metadata for folders absent locally.

Independent tests:

```sh
dotnet run --project Tools/Jehad/V1_14/Tests/PreferenceTests.csproj --configuration Release
```

Expected: **79 assertions**, compiling the actual preference codec/save model,
UiValuesV16 and CoreTypes under C# 9.0 with warnings as errors. Coverage includes
all interface flag combinations, exact float round trips, four locales,
malformed/unsupported records, clamping, debounce, cancelled writes, stale
acknowledgement and Restore Defaults. No Unity stubs or NuGet packages.

The validator also runs the previous 61 sparkle, 80 guide, 51 Guardian,
111 notification, 335 layout and 1,586 menu assertions: **2,303 total**.
Static checks cover seven C# syntax trees, scene references, load ordering,
limited storage scope and preservation of the inherited menu/harness code.
`--baseline` accepts a relative-path to SHA256 mapping and permits only the
contract log to change among earlier files.

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.14.unity` for native review.
`docs/jehad/versions/Jehad_V1.14.md` describes the storage context action and
standalone relaunch test. Pure C# tests do not execute PlayerPrefs or validate
native startup order, UI rendering, input, storage errors or disk performance.

Unity 6.3 API references consulted:

- https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerPrefs.Save.html
- https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerPrefs.SetString.html
