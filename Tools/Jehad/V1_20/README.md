# V1.20 lid-synchronized chest rewards

Requirements: Python 3.10+, PyYAML, tree-sitter, tree-sitter-c-sharp and .NET SDK 8.
From the repository root:

```sh
python Tools/Jehad/V1_20/build_checkpoint.py --manifest /tmp/jehad-v120-manifest.json
python Tools/Jehad/V1_20/validate_checkpoint.py --manifest /tmp/jehad-v120-manifest.json --dotnet /absolute/path/to/dotnet --report /tmp/jehad-v120-validation.json
```

The builder copies V1.19, adds one chest-gate adapter to the existing world
presentation root and rebinds the existing world-polish review component.
Its serialized references remain unchanged. Re-running should write zero files.
Partial staging may pass `--base-tree-json` with the remote recursive tree to
preserve metadata for remote-only folders.

Independent new tests:

```sh
dotnet run --project Tools/Jehad/V1_20/Tests/RevealTests.csproj --configuration Release
```

Expected: **28 assertions** compile the actual ChestRevealPolicyV120 with the
original ChestStateV15 and CoreTypes under C# 9.0, warnings as errors. Coverage
includes legacy opt-out equivalence, closed/collected precedence, the 95% lid
threshold, invalid progress and original open/retry/New Game policy journeys.
No Unity stubs or NuGet packages.

The validator also runs the previous ten suites: **8,737 assertions total**.
Static checks cover three new C# syntax trees plus the extended chest source,
129 scene YAML documents, authored chest/review references and scoped source
changes. Reversing those changes must reproduce the verified original chest
source. The old pickup/counter/audio/particle implementations are preserved.

`--baseline` permits only SecretChestV15.cs and the contract log to change among
earlier files. Open `Assets/_Project/Scenes/Jehad/Jehad_V1.20.unity` for engine
review. See `docs/jehad/versions/Jehad_V1.20.md` for the updated acceptance
action. Unity compilation, lid/trigger execution, collection and visual/audio
quality remain separate from pure model/static checks.
