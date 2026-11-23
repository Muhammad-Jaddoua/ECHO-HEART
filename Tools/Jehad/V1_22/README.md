# V1.22 namespace fix and source audit

Requirements: Python 3.10+, PyYAML, tree-sitter, tree-sitter-c-sharp and .NET SDK 8.
Run from the repository root:

```sh
python Tools/Jehad/V1_22/build_checkpoint.py --manifest /tmp/jehad-v122-manifest.json
dotnet run --project Tools/Jehad/V1_22/NamespaceAudit/NamespaceAudit.csproj --configuration Release -- "$PWD" /tmp/jehad-v122-namespaces.json
python Tools/Jehad/V1_22/validate_checkpoint.py --manifest /tmp/jehad-v122-manifest.json --namespace-report /tmp/jehad-v122-namespaces.json --report /tmp/jehad-v122-validation.json
```

On Windows, replace `"$PWD"` with your absolute repository directory. The audit
uses Roslyn assemblies bundled with the .NET SDK and needs no NuGet packages.
It compiles the audit tool itself, not the Unity game.

The builder copies V1.21 and updates version labels, adding no components or
scene roots. It should write zero files on its second run. Partial staging may
pass `--base-tree-json` with the remote recursive tree to preserve folder metadata.

The audit indexes top-level Echoheart type names from every project script and
checks candidate references in Jehad scripts with Editor symbols and with Player
symbols. It checks namespace imports, enclosing namespaces and explicit names;
it excludes member-access names and locally declared delegate/event method
groups. It is a focused source audit: it does not bind symbols, inspect Unity
assemblies, resolve every C# construct, validate external APIs or run gameplay.
Aliases, nested types and ambiguous value/type references need manual/compiler
review. Findings are candidates to inspect, not compiler diagnostics. A PASS
does not imply Unity compilation passed.

The checkpoint run indexed 152 scripts and audited 116 Jehad scripts in both
configurations. Before the fix it identified the two WorldCueV15 references in
AirborneFeedbackV121 in each configuration; after adding Echoheart.Audio it
reported no missing-namespace candidates or syntax errors. A source digest
prevents passing an audit report produced for different current sources.

The validator checks 131 scene YAML documents and proves the scene differs from
V1.21 only in version labels. Removing the one added using directive must
reproduce the verified original presenter source. No gameplay body changes.

Optional `--baseline` accepts a path-to-SHA256 JSON mapping and allows only the
presenter import fix and contract log to change among earlier files.
`--base-tree-json` verifies that all original project scripts are present and
match remote blob hashes after reversing the import. `--before-namespace-report`
can include captured evidence of the original failure. It is historical evidence
from the pre-fix source; the current audit always reads the current source.

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.22.unity` in Unity for the next pass.
Check Console compilation first, then follow the movement, chest, audio, menu
and flow reviews documented in their checkpoint notes. Native compilation,
playtesting, final appearance and performance still need Unity verification.
