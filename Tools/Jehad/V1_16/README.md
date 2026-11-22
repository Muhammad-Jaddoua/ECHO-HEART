# V1.16 scenic title vista

Requirements: Python 3.10+, PyYAML, tree-sitter, tree-sitter-c-sharp and .NET SDK 8.
From the repository root:

```sh
python Tools/Jehad/V1_16/build_checkpoint.py --manifest /tmp/jehad-v116-manifest.json
python Tools/Jehad/V1_16/validate_checkpoint.py --manifest /tmp/jehad-v116-manifest.json --dotnet /absolute/path/to/dotnet --report /tmp/jehad-v116-validation.json
```

The deterministic builder copies V1.15 and adds the vista and EditorOnly
review components to existing roots. It copies the camera/focus references
from the existing flow's title fields. No earlier serialized component fields
or scene roots change. Re-running should write zero files. Partial staging
may pass `--base-tree-json` with the remote recursive Git tree to preserve
metadata for remote-only folders.

Independent tests:

```sh
dotnet run --project Tools/Jehad/V1_16/Tests/VistaTests.csproj --configuration Release
```

Expected: **30 assertions** compile the actual pure motion model under C# 9.0
with warnings as errors. Coverage includes initial/reset poses, page/focus
holds, reduced motion, invalid/large frame times, bounded steps, quarter-cycle
positions, seam continuity and a 100,000-step run. No Unity stubs or NuGet
packages. The validator also runs the eight prior suites: **2,374 assertions
total**.

Static checks cover three C# syntax trees, 124 scene YAML documents,
camera/focus/menu/settings references, prior-scene preservation and camera
write guards. `--baseline` accepts relative-path to SHA256 values; only the
contract log may change among earlier files.

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.16.unity` for native review.
See `docs/jehad/versions/Jehad_V1.16.md` for the context action and manual cases.
Unity compilation, actual camera execution, composition, geometry clearance,
input/focus, context actions and profiling remain outside these checks.
