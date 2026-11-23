# V1.13 local collectible sparkles

Requirements: Python 3.10+, PyYAML, tree-sitter, tree-sitter-c-sharp, .NET SDK 8.
From repository root:

```sh
python Tools/Jehad/V1_13/build_checkpoint.py --manifest /tmp/jehad-v113-manifest.json
python Tools/Jehad/V1_13/validate_checkpoint.py --manifest /tmp/jehad-v113-manifest.json --dotnet /absolute/path/to/dotnet --report /tmp/jehad-v113-validation.json
```

The builder copies V1.12 and adds two components to existing world/harness
roots. It reuses the existing particle material. Re-running should write zero
files. For partial staging, pass `--base-tree-json` with the recursive remote
Git tree to preserve metadata for folders absent locally.

Independent new tests:

```sh
dotnet run --project Tools/Jehad/V1_13/Tests/GlintTests.csproj --configuration Release
```

Expected: **61 assertions** compile the actual pure C# selection source with
C# 9.0 and warnings as errors. No Unity stubs or NuGet dependencies. Coverage:
eligibility inputs, invalid/duplicate candidates, nearest-six capacity, stable
emitter slots, deterministic ties, 10m/12m hysteresis, clearing and distance fade.
The validator also runs the existing 80 guide, 51 Guardian, 111 notification,
335 layout and 1,586 menu/focus/settings assertions: **2,224 total**.

Static checks cover C# syntax, scene YAML, references, unchanged prior scene
content, chest/cover/view guards, passive ownership and the 48-particle cap.
`--baseline` accepts a relative-path to SHA256 mapping; the contract log is
the only allowed earlier-file edit.

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.13.unity` for engine review.
See `docs/jehad/versions/Jehad_V1.13.md` for the context-menu harness and manual
checks. Unity compilation, rendering, native physics, audio and profiling
remain separate; none is established by these pure C# or static checks.
