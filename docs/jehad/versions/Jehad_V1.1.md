# Jehad V1.1 — expanded meadow and nature art

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.1.unity` in Unity 6000.3.27f1.
WASD/Space uses the existing layout test controller, not the production player.

The main clearing is now 72 × 68 m (4,896 m²), up from 36 × 34 m (1,224 m²).
It has a curved central route, two reconnecting side loops, six secret locations,
three reserved enemy encounter spaces, and 12 collectible placement anchors.
The bridge now leads to the compact boss arena at world Z=56. Two areas remain.
This user-requested expansion supersedes the earlier two-secret layout target;
the game retains Sword, Bow and Fire only. No extra enemy or region is introduced.

Fifteen real CC0 Kenney models replace the primitive main-area vegetation and
rocks. There are 27 placed trees and 326 ground-cover instances combined into
four regional meshes. URP Lit materials use warmer grass/leaf tints, use
instancing, and foliage casts no shadows. Trunks, boulders and boundaries use
simple colliders; grass/flowers do not obstruct walking. GPU timings are unmeasured.

## Secret location review

| Anchor suffix | World position (X, Y, Z) |
|---|---|
| MushroomHollow | -28, 0.15, -24 |
| FallenLogCache | 28, 0.15, -20 |
| HighPerch | -29, 1.4, 12 |
| OldShrine | 29, 0.15, 15 |
| CanopySanctuary | -23.5, 0.15, 27 |
| RockGrotto | 28, 0.15, 29 |

These are traversable hiding spaces and named integration anchors. Secret reward
logic, collectible pickup/counters, enemies and boss combat are not implemented by
this environment checkpoint. Their systems remain subsequent integration work.
The compact arena, Heart visual placeholder, and arena anchors are retained.

## Verification and remaining Unity review

Static checks cover YAML syntax, unique GUIDs, asset/meta pairing, cross-asset
references, decoded mesh buffers/index ranges, original OBJ geometry/material
equivalence, floor continuity, six accessible secret anchors and V1.0 preservation.
A Blender geometry preview can check placement, but is not a Unity render.
The attached [layout preview](previews/Jehad_V1.1.jpg) is rendered in Blender,
with illustrative lighting. Unity appearance and performance remain unverified.
No Unity Editor is available here: import, console, camera clearance, collider
walkthrough, jump feel, URP appearance, performance and NavMesh bake need Unity
review before this scene is considered production-ready. NavMesh is not baked.
The original scenes, build list and teammate runtime systems are unchanged.

## Asset Store comparison

Reviewed these official public listings while sourcing the art:
- Low Poly Nature Pack – PVA (free, URP listed): https://assetstore.unity.com/packages/3d/environments/low-poly-nature-pack-pva-243603
- Stylized Low Poly Nature Lite (free): https://assetstore.unity.com/packages/3d/environments/landscapes/stylized-low-poly-nature-lite-275732

These Store packs were not downloaded or imported. The selected CC0 Kenney subset
is available without a Unity account and includes its license and source files.

Next after environment review: game flow, menus and retry in a new version.
