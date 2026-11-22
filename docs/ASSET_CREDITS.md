# Asset Credits

Every third-party asset in the build. Add a row in the same commit that imports it.

| Asset | Author | Source URL | License | Used for | Added in |
|---|---|---|---|---|---|
| Nature Kit 2.1 (15 selected models) | Kenney | https://kenney.nl/assets/nature-kit | CC0 1.0; included LICENSE.txt | Trees, grass, bushes, flowers, mushrooms, rocks, log, stump, grotto arch in Jehad V1.1 | Jehad_V1.1 |
| Forest Nature Pack (18 selected models and shared gradient atlas) | Kay Lousberg | https://kaylousberg.itch.io/kaykit-forest | CC0 1.0; included LICENSE.txt | Textured trees, autumn variants, shrubs, grass and rocks in Jehad V1.2; derived distant tree LODs | Jehad_V1.2 |


Pinned download mirror, original file checksums, and conversion details:
`Assets/ThirdParty/KenneyNatureKit/PROVENANCE.json`. Source art is Kenney's,
not original team art. Native meshes preserve the OBJ geometry; grass and leaf
materials have warmer green tints, recorded in the provenance file.

KayKit provenance: `Assets/ThirdParty/KayKitForest/PROVENANCE.downloads.json`.
Source geometry, normals and UVs are retained in the full-resolution meshes;
distant tree LODs are derived with Blender. The original forest atlas is unchanged.

Engine/packages: Unity 6 (URP, Input System, Cinemachine, ProBuilder, AI Navigation). Unity Technologies.

## Project-original V1.5 presentation

The six environment/Guardian music loops, three ambience loops, and 23 sound effects in
`Assets/_Project/Audio/*/Jehad/V1_5` were generated for Echoheart from original note arrangements,
waveforms and filtered noise. No external recordings, samples, melodies or third-party audio packs
were imported. Reproducible source: `Tools/Jehad/V1_5/build_audio.py` (seed 40215); checksums and
signal-analysis evidence: `docs/jehad/versions/Jehad_V1.5.audio.json`. These are project-original
assets, not an additional third-party license entry. The hinged chest, arched lid/band meshes,
particle shader and effects configuration are also original project work; existing material
colors and nature-pack credits above still apply.
