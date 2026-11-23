# V1.17 open title composition

Requirements: Python 3.10+, PyYAML, tree-sitter, tree-sitter-c-sharp and .NET SDK 8.
From the repository root:

```sh
python Tools/Jehad/V1_17/build_checkpoint.py --manifest /tmp/jehad-v117-manifest.json
python Tools/Jehad/V1_17/validate_checkpoint.py --manifest /tmp/jehad-v117-manifest.json --dotnet /absolute/path/to/dotnet --report /tmp/jehad-v117-validation.json
```

The deterministic builder copies V1.16 and rebinds the menu, its two typed
harnesses, and the vista's two typed components. No serialized component field
or scene root changes. Re-running should write zero files. Partial staging may
pass `--base-tree-json` with the recursive remote Git tree to retain metadata
for remote-only folders.

Validation checks seven C# syntax trees, 124 scene YAML documents, references,
prior-scene preservation, inherited behavior and the scoped title/shade edits.
It checks authored button/text rectangles against the left panel, the smaller
power strip, the open logical focal region and backdrop state conditions.
`--baseline` accepts relative-path to SHA256 values; only the contract log may
change among earlier files.

With `--dotnet`, the existing nine pure C# suites run **2,374 assertions**:
vista 30, vitality 41, preferences 79, sparkles 61, guide 80, Guardian 51,
notifications 111, layout 335 and menus 1,586. These cover the reused models;
they do not render or assess the new title composition. No new pure C# test
project is added for the visual layout change.

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.17.unity` for native review.
See `docs/jehad/versions/Jehad_V1.17.md` for the title-layout action, inherited
vista checks and manual review. Compilation/import in Unity, fonts, rendering,
input, actual scenic framing, context actions and profiling remain separate.
