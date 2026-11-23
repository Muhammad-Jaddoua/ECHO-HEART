# V1.21 movement feedback

Requirements: Python 3.10+, PyYAML, tree-sitter, tree-sitter-c-sharp and .NET SDK 8.
From the repository root:

```sh
python Tools/Jehad/V1_21/build_checkpoint.py --manifest /tmp/jehad-v121-manifest.json
python Tools/Jehad/V1_21/validate_checkpoint.py --manifest /tmp/jehad-v121-manifest.json --dotnet /absolute/path/to/dotnet --report /tmp/jehad-v121-validation.json
```

The builder copies V1.20, adds AirborneFeedbackV121 to the existing world
presentation root and MovementAudioReviewV121 to the EditorOnly UI review root.
It changes no existing serialized field. Re-running should write zero files.
Partial staging may pass `--base-tree-json` with the remote recursive tree to
preserve metadata for remote-only folders.

Independent new tests:

```sh
dotnet run --project Tools/Jehad/V1_21/Tests/MovementTests.csproj --configuration Release
```

Expected: **92 assertions**, actual MovementCuePolicyV121, C# 9.0, warnings as
errors, no Unity stubs or NuGet packages. Coverage includes upward versus ledge
departure, meaningful landing, small contact changes, pause, reset/teleport
settling, invalid samples and different sampling rates.

The validator also runs the eleven existing pure suites: **8,829 assertions
total**. Static checks cover three new C# syntax trees plus WorldFeedbackV15,
131 scene YAML documents, typed references and scoped source changes. Reversing
the optional property/two-cue guard must reproduce the verified original
feedback source. Reversing the two new scene components/version labels must
reproduce V1.20 exactly. Omitting `--dotnet` runs only static checks; the report
then has an empty compiled_tests object.

`--baseline` accepts a JSON mapping of existing repository paths to SHA256 and
permits only WorldFeedbackV15.cs and the contract log to change among those
files. Open `Assets/_Project/Scenes/Jehad/Jehad_V1.21.unity` for engine review.
The native context action resets the run and creates a temporary physics
fixture; do not run other review actions concurrently. See
`docs/jehad/versions/Jehad_V1.21.md` for manual checks and heuristic limits.
Unity compilation, actual movement/audio/dust, input and profiling remain
separate from pure model/static checks.
