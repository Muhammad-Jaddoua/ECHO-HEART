# V1.5 original world/audio presentation

Run from repository root. Required: Python 3.10+, NumPy, SciPy, PyYAML,
tree-sitter, tree-sitter-c-sharp, FFmpeg/FFprobe with libvorbis, and apply_patch.

```sh
python Tools/Jehad/V1_5/build_audio.py --report /tmp/jehad-v15-audio.json
python Tools/Jehad/V1_5/build_checkpoint.py --audio-manifest /tmp/jehad-v15-audio.json --manifest /tmp/jehad-v15-manifest.json
python Tools/Jehad/V1_5/validate_checkpoint.py --manifest /tmp/jehad-v15-manifest.json --report /tmp/jehad-v15-static.json
dotnet run --project Tools/Jehad/V1_5/Tests/WorldPresentationTests.csproj --configuration Release
```

The generator authors six music tracks, three ambient loops and 23 mono cues.
Seed 40215, all note arrangements and waveform/noise synthesis are in source;
no external samples are required. PCM is deterministic with the same dependency
versions; Vorbis bytes/checksums can change with encoder/container versions.
The validator decodes every asset and checks durations, peaks/RMS, loop-boundary
outliers and distinct music content; these checks do not replace listening.

Music/ambience have streaming imports and SFX have preloaded PCM imports.
The small authored files use .gitattributes exceptions only in their own V1_5
folders, so direct Git storage does not leave unuploaded LFS pointers. Root
attribute rules and other versions/assets are preserved.

Scene authoring reuses the existing V1.4 scene, V1.2 landscape/materials and
V1.3 flow/menu volume API. It writes only V1.5 scene/scripts metadata, audio
bank/import metadata, chest prefab/curved meshes, and particle shader/materials.
`--base-tree-json` optionally supplies a GitHub recursive tree for partial staging
directories so missing local remote-folder metadata is not recreated.

The static validator's optional `--baseline` accepts a pre-checkpoint map of
relative paths to SHA256 digests. Only the existing credits and contract log may
change. It checks syntax/serialization/geometry, not Unity API type checking.

.NET SDK 8 is needed only for the engine-independent tests; no NuGet packages
are needed. Expected: **6,335 assertions** for the actual region/mix and chest
policy implementations, using actual shared CoreTypes and C# 9.0.

Open `Assets/_Project/Scenes/Jehad/Jehad_V1.5.unity` and follow the version note
for the supplied (unexecuted here) WorldPolishReviewV15 Play-mode harness,
manual audio/particle/chest checks, and existing flow/population regressions.
The environment has no Unity Editor, so engine compilation/import, audible
quality, visual appearance and frame-time claims are deliberately pending.
