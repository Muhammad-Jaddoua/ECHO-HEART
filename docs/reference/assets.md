# Free Asset Sources (researched 13 Nov 2026)

Goal: make the game look good with minimum time. Prefer **CC0** (no attribution needed, but we still credit everything
in `docs/ASSET_CREDITS.md`). Always re-check the license on the download page before importing.

## Import rules
1. Download packs **outside** the repo (e.g. `~/Downloads/echoheart-assets/`). Import **only the files we use** into
   `Assets/ThirdParty/<PackName>/`. Whole packs bloat the repo and LFS quota.
2. Log the asset in `docs/ASSET_CREDITS.md` (name, author, URL, license, what we used) **in the same PR**.
3. Convert materials to URP (`Edit → Rendering → Materials → Convert…` or swap shader to URP/Lit). Fix pink materials before merging.
4. Recolor to our palette (green / turquoise / cream / **violet = corrupted**) so mixed packs look cohesive.
5. Nothing that resembles Kirby/Nintendo characters. Our spirit, enemies, and boss must be original silhouettes.

## Recommended picks by need

### Milestone 2 (graybox + key models)
| Need | Pick | License | Notes |
|---|---|---|---|
| Graybox textures | **Kenney Prototype Textures** (kenney.nl) | CC0 | Grid textures make graybox readable and scale-checkable |
| Graybox blocks | Unity **ProBuilder** (package) | Unity | Build both areas fast; replace later |
| Enemies + boss | **Quaternius Ultimate Monsters** (quaternius.com; also [Sketchfab listing](https://sketchfab.com/3d-models/ultimate-monsters-pack-fd72e114d119488da71fe3a16f216c4f)) | CC0 | 50 animated monsters (walk/run/attack/death), FBX/glTF. Pick 3 distinct normal enemies + 1 big one for the Guardian (scale up, violet tint) |
| Alt. enemies | [Quaternius LowPoly Animated Monsters](https://quaternius.itch.io/lowpoly-animated-monsters), [OGA Quaternius CC0 collection](https://opengameart.org/content/all-cc0-uploader-quaternius) | CC0 | Smaller/simpler set |
| Player spirit | Build from primitives (sphere body + eyes + small feet/leaf) **or** a round Quaternius monster recolored | CC0 / ours | A primitive spirit with squash-and-stretch is original, cheap, and reads well. Ability accessory = sword/bow/flame prop on top |
| Weapon accessories | [KayKit Adventurers](https://kaylousberg.itch.io/kaykit-adventurers) (swords, bows, etc.) | CC0 | Free tier; tiny props for the "held power" accessory |
| Movement/camera base | Unity **Starter Assets – ThirdPerson** (search the Asset Store; [Unity blog intro](https://unity.com/en/blog/games/say-hello-to-the-new-starter-asset-packages)) | Unity Asset Store EULA | CharacterController + Cinemachine + Input System, Unity 6 compatible. Use as a *reference or base* for the PlayerMotor (we must understand/own the code; credit it) |

### Phase 3 (art pass), but useful early for the gameplay video
| Need | Pick | License | Notes |
|---|---|---|---|
| Nature / island | **[Quaternius Stylized Nature MegaKit](https://quaternius.itch.io/stylized-nature-megakit)** (Standard = free) | CC0 | 110+ trees, plants, flowers, rocks, grass. "Ghibli-inspired", fits our tone |
| Nature alt. | **[KayKit Forest Nature Pack](https://kaylousberg.itch.io/kaykit-forest)** | CC0 | 200+ stylized trees/bushes/rocks/grass/terrain; on itch + Unity Asset Store |
| Ruins / props | [KayKit Dungeon](https://kaylousberg.itch.io/kaykit-dungeon), [KayKit Platformer](https://kaylousberg.itch.io/kaykit-platformer), Quaternius Fantasy Props MegaKit | CC0 | Ruin pieces, Heart pedestal, platforms for secrets |
| VFX | **[Cartoon FX Remaster Free](https://assetstore.unity.com/packages/p/cartoon-fx-remaster-free-109565)** (Jean Moreno) | Asset Store EULA | URP-compatible stylized hits, fire, sparkles, poofs. Enable **Depth Texture** in URP asset or effects vanish |
| VFX textures | Kenney Particle Pack | CC0 | For custom inhale swirl / capture puff |
| Sky | [Fantasy Skybox FREE](https://assetstore.unity.com/packages/2d/textures-materials/sky/fantasy-skybox-free-18353) | Asset Store EULA | Check material on URP; a URP gradient/procedural sky is the fallback |
| Toon look (optional) | Unity Toon Shader package (`com.unity.toonshader`) or plain URP Lit with saturated colors + rim | Unity | Only if time; plain Lit + good lighting is fine |
| Animation | Models' built-in clips; [Mixamo](https://www.mixamo.com) for humanoids | Mixamo terms (free, Adobe account) | Spirit can be animated procedurally (bob, squash) |

### UI, fonts, audio
| Need | Pick | License | Notes |
|---|---|---|---|
| UI kit | Kenney UI Pack / UI Pack Adventure | CC0 | Buttons, panels, bars |
| Power icons | [game-icons.net](https://game-icons.net) | **CC BY 3.0** (credit required) | Sword / bow / flame icons, recolorable SVG |
| Fonts | Google Fonts, e.g. Fredoka or Baloo 2 | SIL OFL | Rounded, playful; import via TextMeshPro Font Asset Creator |
| SFX (fast) | **Kenney audio packs** (Interface, Impact, RPG, Digital) | CC0 | Pickup, hit, UI clicks |
| SFX (quality) | **Sonniss GDC Game Audio Bundle** (sonniss.com/gameaudiogdc) | Royalty-free, commercial OK, no AI training | Huge (tens of GB). Grab only the few files you need |
| SFX (specific) | Freesound.org (filter **CC0**), OpenGameArt | per-file | Check every file's license |
| Music | Pixabay Music; OpenGameArt (filter CC0); Incompetech (Kevin MacLeod) | Pixabay license / CC0 / **CC BY** (credit) | One whimsical loop for the island + optional boss loop |

## Things to avoid
- Ripped Nintendo/HAL assets, fan models, or Kirby-lookalikes.
- BBC Sound Effects (non-commercial RemArc license), unclear "free" YouTube music.
- Importing a full 1–2 GB pack into the repo.
- Paid tiers (Quaternius Pro/Source, KayKit Extra): the free tiers are enough.

## Sources consulted
- https://quaternius.itch.io/stylized-nature-megakit · https://opengameart.org/content/stylized-nature-megakit
- https://sketchfab.com/3d-models/ultimate-monsters-pack-fd72e114d119488da71fe3a16f216c4f
- https://kaylousberg.itch.io/kaykit-adventurers · https://kaylousberg.itch.io/kaykit-forest
- https://assetstore.unity.com/packages/p/cartoon-fx-remaster-free-109565
- https://unity.com/en/blog/games/say-hello-to-the-new-starter-asset-packages
- https://app.cinevva.com/guides/free-sound-effects-music (secondary guide: Kenney/Sonniss/Freesound/Pixabay/Incompetech licensing)
