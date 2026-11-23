# V1.4 world population authoring

This step reuses the existing V1.3 flow and V1.2 landscape. It does not regenerate
older assets, alter shared Core contracts, or import the unmerged player branch.
See `docs/jehad/versions/Jehad_V1.4.md` for Unity review and integration limits.

From the repo root, with Python 3.10+, the `apply_patch` command, and Python
packages `PyYAML`, `tree-sitter`, `tree-sitter-c-sharp` installed:

```sh
python Tools/Jehad/V1_4/build_checkpoint.py --manifest /tmp/jehad-v14-manifest.json
python Tools/Jehad/V1_4/validate_checkpoint.py --manifest /tmp/jehad-v14-manifest.json --report /tmp/jehad-v14-static.json
```

The builder uses `Tools/Jehad/V1_2/unity_authoring.py` for deterministic native
Unity assets and `landscape.py` for source heights. It creates only V1.4 scripts'
metadata, the V1.4 scene and three reusable source prefabs. For a partial staging
directory, `--base-tree-json` takes a GitHub recursive tree JSON so existing
remote folder metadata is preserved even when absent locally.

The validator reads native box colliders from the actual landscape prefab,
transforms their bounds, checks source clearance and a conservative terrain
walking graph, then checks source/scene references and C# syntax. Optional
`--baseline` accepts a pre-change JSON map of relative paths to SHA256 digests
and rejects changes outside the existing contract log. It does not execute
Unity code or establish runtime NavMesh correctness.

The unchanged flow regression can be run separately with .NET SDK 8:

```sh
dotnet run --project Tools/Jehad/V1_3/Tests/FlowPolicyTests.csproj --configuration Release
```

Expected: 445 assertions across six policy scenarios. This is not an engine test.
The new engine smoke test is the PopulationReviewV14 context menu on the scene's
EditorOnly harness root. It was supplied but not executed in this environment.
Follow it with the existing V1.3 flow/combat/AI checks and a real player playtest.

Unity API references used during implementation:

- [NavMeshBuilder.CollectSources](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/ai/navmeshbuilder/collectsources)
- [NavMeshBuildSourceShape.ModifierBox](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/ai/navmeshbuildsourceshape/modifierbox)
- [NavMeshBuildSettings](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/ai/navmeshbuildsettings)
