# Jehad V1.2 authoring tools

The scene and native assets are already checked in. Unity users do not need to
run these tools to open or play the scene.

Rebuild on a development machine with Python 3.10+, Blender 4.3+ on PATH, and
`pip install -r Tools/Jehad/V1_2/requirements.txt`:

```
python Tools/Jehad/V1_2/build_world.py
python Tools/Jehad/V1_2/validate_world.py
blender -b --python Tools/Jehad/V1_2/render_preview.py -- Temp/JehadV12 docs/jehad/versions/previews
```

`build_world.py` writes only the V1.2 scene, V1.2 generated art/prefabs and KayKit
import subset, plus the asset-credit row. It reads immutable V1.0 component
templates and the V1.1 scene. Keep those checkpoint files in the checkout.
Writes currently use the `apply_patch` CLI, which must also be on PATH.

`landscape.py` contains deterministic height/biome, route and secret definitions.
The main terrain uses four 2 m grid meshes with matching static MeshColliders.
Paths are clipped against those same terrain triangles. The lake is deliberately
shallow enough to wade; no swimming ability or water physics system is required.

Blender creates actual decimated distant tree meshes and illustrative geometry
previews. The previews are not Unity renders or measurements of runtime speed.
The validator checks native buffers, GUIDs, component wiring, source geometry/UV
equivalence, instance bounds, and conservative terrain routes. It cannot compile
Unity C#/HLSL, cook PhysX meshes, bake NavMesh, or play-test the game.

All third-party art is documented under `Assets/ThirdParty/KayKitForest/` and
`docs/ASSET_CREDITS.md`. Scene-specific review controls stay in Testing; they
do not replace the production PlayerMotor or HUD.
