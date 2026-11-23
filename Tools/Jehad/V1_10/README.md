# V1.10 notification checkpoint

Requirements: Python 3.10+, PyYAML, tree-sitter, tree-sitter-c-sharp, .NET SDK 8.
From repository root:

```sh
python Tools/Jehad/V1_10/build_checkpoint.py --manifest /tmp/jehad-v110-manifest.json
python Tools/Jehad/V1_10/validate_checkpoint.py --manifest /tmp/jehad-v110-manifest.json --dotnet /absolute/path/to/dotnet --report /tmp/jehad-v110-validation.json
```

Independent new test:

```sh
dotnet run --project Tools/Jehad/V1_10/Tests/NotificationTests.csproj --configuration Release
```

Expected: **111 queue/layout assertions**, plus **335** existing V1.9 layout
and **1,586** V1.8 menu/focus/settings regression assertions when running the
validator with `--dotnet`. Tests compile actual pure C# sources with C# 9.0 and
warnings as errors. No Unity stubs or external NuGet packages are used.

The deterministic builder clones V1.9. It replaces the toast, discovery skin
with an observer, UI harness and two secret-location script bindings; it clears
the old feedback component's optional toast reference. The main menu/HUD,
audio, particles, chest/relic reward owners and gameplay scene objects remain
reused. No build settings or old snapshots change. For partial staging,
`--base-tree-json` accepts the remote recursive Git tree to preserve remote-only
folder metadata. Re-running the builder should write zero files.

Validation checks syntax, GUIDs/serialization, explicit presentation wiring,
scene preservation outside declared changes, no duplicate text owner and exact
secret-rule preservation after removing the added presentation call. Optional
`--baseline` accepts a pre-checkpoint relative-path→SHA256 map; only the contract
log may change among those files.

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.10.unity` for actual Unity testing.
See `docs/jehad/versions/Jehad_V1.10.md` for the two UI/notification harness
actions, manual chest/relic/boss cases and the explicit runtime-testing limits.
