# Jehad V1.0 - World layout checkpoint

Scene: `Assets/_Project/Scenes/Jehad/Jehad_V1.0.unity`

This first checkpoint establishes the main clearing and boss arena as two reusable,
version-specific world prefabs. The clearing has a direct route and two reconnecting
side paths. A bridge and landmark arch lead to the arena. Environment geometry has
colliders on the Environment layer; decorative foliage does not block walking.
The two secret locations, enemy positions, retry positions, and Heart location have
named anchor transforms for subsequent systems. All new materials use the existing
URP shader. No external assets or new runtime scripts are introduced.

The scene uses the repository's existing test controller (WASD/Space) for the layout
walkthrough. It is a review scene, not the release build or Tariq's production player.
The Heart visual is a placement placeholder. Combat, collectible logic, menus,
retry, audio, NavMesh baking, and full visual polish belong to later checkpoints.

## Verification

- Checked Unity YAML syntax, unique object identifiers, local references, hierarchy,
  generated asset GUID references, floor continuity, and asset/meta pairing.
- Not opened, imported, rendered, or play-tested in Unity in this environment.
- NavMesh has not been baked. No enemy agents are instantiated by this checkpoint.
- Existing scenes, player/combat scripts, build scene list, and shared assets are untouched.

## Layout review

Open the named scene in Unity 6000.3.27f1 and walk from the entry through each side
path and across the bridge into the arena. Check widths, collisions, jump steps,
the arena entrance, and the Heart placement. Record import/console issues and any
awkward camera/collision spots before approving the next version.

Next: `Jehad_V1.1` adds the game-state flow and menus after the V1.0 layout review.
