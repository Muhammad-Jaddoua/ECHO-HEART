# V1.15 health readability

Requirements: Python 3.10+, PyYAML, tree-sitter, tree-sitter-c-sharp and .NET SDK 8.
From the repository root:

```sh
python Tools/Jehad/V1_15/build_checkpoint.py --manifest /tmp/jehad-v115-manifest.json
python Tools/Jehad/V1_15/validate_checkpoint.py --manifest /tmp/jehad-v115-manifest.json --dotnet /absolute/path/to/dotnet --report /tmp/jehad-v115-validation.json
```

The deterministic builder copies V1.14 and rebinds only the menu and its two
typed harnesses. No serialized field, scene root or gameplay component is
added. Re-running should write zero files. Partial staging may pass
`--base-tree-json` with the remote recursive Git tree to retain remote-only
folder metadata.

Independent tests:

```sh
dotnet run --project Tools/Jehad/V1_15/Tests/VitalTests.csproj --configuration Release
```

Expected: **41 assertions** compile the actual HealthBarV115 model under C# 9.0
with warnings as errors. These include immediate damage, repeated reports/hits,
healing, max-health changes, transition snapping, reduced motion, invalid frame
times, frame partitioning, exact low-health thresholds and bounded behavior
across 10,000 mixed health observations. No Unity stubs or NuGet packages.

The validator also runs the existing 79 preference, 61 sparkle, 80 guide,
51 Guardian, 111 notification, 335 layout and 1,586 menu assertions:
**2,344 total**. Static checks cover seven C# syntax trees, 122 scene YAML
documents, references, scoped rebindings, unchanged menu/harness behavior,
unchanged non-health HUD methods and passive presentation ownership.
`--baseline` accepts relative-path to SHA256 values; only the contract log may
change among earlier files.

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.15.unity` for native review.
See `docs/jehad/versions/Jehad_V1.15.md` for the vitality context action and
manual checks. Unity compilation, actual health events, UI mesh/render/input,
supplied context actions and frame cost remain outside these checks.
