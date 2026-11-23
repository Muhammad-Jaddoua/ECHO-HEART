# Jehad V1.3 authoring and verification

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.3.unity` in the project Unity version.
The review note at `docs/jehad/versions/Jehad_V1.3.md` explains the controls,
integration boundary and required Unity acceptance checks.

## Rebuild scene wiring

Python 3.10+, the existing `Tools/Jehad/V1_2/unity_authoring.py`, and the
`apply_patch` command are required. Run from the repository root:

```sh
python Tools/Jehad/V1_3/build_checkpoint.py --manifest /tmp/jehad-v13-manifest.json
```

This copies the V1.2 scene into its own version, replaces only its review player
script, adds the flow/boss/Heart bindings and writes the new gate prefab and
deterministic metadata. It reuses V1.2 landscape/arena prefabs. Do not regenerate
old versioned assets when iterating on V1.3. In a partial staging directory,
provide `--base-tree-json` with the GitHub recursive tree JSON to avoid creating
metadata for existing remote folders absent locally.

## Static checks

Install Python dependencies `PyYAML`, `tree-sitter`, `tree-sitter-c-sharp` and run:

```sh
python Tools/Jehad/V1_3/validate_checkpoint.py --manifest /tmp/jehad-v13-manifest.json --report /tmp/jehad-v13-static.json
```

An optional `--baseline` accepts a JSON map of repository-relative paths to
SHA256 digests taken before the checkpoint. Validation permits an existing
contract-log update and rejects other changes or deletion of baseline files.
Syntax parsing checks the new C# files but does not type-check Unity APIs.

## Executable policy tests

With .NET SDK 8 installed, from the repository root:

```sh
dotnet run --project Tools/Jehad/V1_3/Tests/FlowPolicyTests.csproj --configuration Release
```

No NuGet packages are required. Tests compile the actual scene policy and Core
enums in C# 9.0, with warnings as errors. Expected output is `PASS: 445 assertions
across 6 flow scenarios; actual SessionPolicyV13 + CoreTypes compiled.` Tests
cover title guards, retries, one-shot boss/completion, pause, new runs and all
reachable policy states. They do not exercise Unity engine components.

For those components, run the Editor-only `FlowReviewHarnessV13` acceptance
context menu in Play mode, then manually test menus, arena navigation and the
production player. This harness was supplied but not run in the authoring
environment. Do not commit `bin/`, `obj/` or `__pycache__/` outputs.
