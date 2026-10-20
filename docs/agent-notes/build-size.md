# Build size

Measured on 2026-10-20 with a Windows 64-bit build of `Main Menu` and `Final Scene`.

## Building from the command line

With the editor closed, this builds the scenes listed in Build Settings without any script:

    "C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe" -batchmode -quit -nographics -projectPath <project> -buildTarget Win64 -buildWindows64Player "<project>/Builds/Windows/der Friedhof.exe" -logFile "<project>/Builds/build.log"

It takes a few minutes. `Builds/` is ignored by git. The log contains a "Build Report" with the size by category and the biggest assets. A build (from the editor or batch mode) rewrites the shader prefiltering fields of the URP assets in `Assets/Settings/`. The rewritten version was committed on 2026-10-20, so later builds should leave them unchanged; if they show up modified again, a setting that affects shader stripping has changed.

## Where the size goes

- Full build: 1.8 GB. Meshes 907 MB, textures 521 MB, sounds 6 MB, scripts 0.2 MB.
- The meshes are almost all static batching. `Final Scene` has 285 static renderers, mostly Megascans props, and static batching writes a combined copy of their vertices into the scene data (`level1.resS`, 930 MB). The same build with static batching turned off was 635 MB, with meshes at 27 MB. Turning it off may cost draw calls; SRP Batcher in URP already reduces most of that cost.
- Textures: about a hundred 2K Megascans maps (albedo, normal, masks, AO) at about 5.3 MB each once compressed, plus the 24 MB `AllSkyFree` night sky. 115 textures still have an 8192 max size in their import settings. With the camera always top-down, 1K is likely enough for most of them.
- Unused assets in `Assets/` (for example most of `Horror Elements/Ambient`, 115 MB of WAV) do not reach the build; only what the two scenes reference is included.

## After the reduction (2026-10-20)

Static batching was turned off for Standalone and the textures under `Quixel`, `SpaceZeta_StreetLamps2`, `Characters`, `Tree_Packs` and `AllSkyFree` were set to a 1024 max size (the night sky to 256, since the camera never shows it and it only feeds ambient light and the 128 px reflection). The build went from 1.8 GB to 259 MB: textures 145 MB, meshes 27 MB, sounds 6 MB, shaders 5 MB. Nothing looked different in Play mode and the main thread stayed at about 4 ms per frame.

- Selecting `t:Texture2D` in `Assets/Quixel` also picks up two font textures from the Quixel Bridge plugin; the Inspector then shows no import settings until the selection is narrowed to the Texture Importer entries.
- The night sky `Equirect` texture is not picked up by the same multi-edit; it had to be set on its own.
- The biggest single asset left is `Quixel/.../3D_Reaper/reaper.fbx` (10.7 MB, listed twice in the report).

## Per-platform texture sizes (2026-10-20)

The user wants full quality on PC and small textures on mobile. The texture metas in those five folders now have Default at 512 (used by Android, iOS and WebGL, whose entries are not overridden) and the existing `Standalone` entry set to `overridden: 1` with 2048, so the PC build again carries about 520 MB of textures (about 635 MB in total). The night sky keeps 256 everywhere, with no Standalone override.

Every texture meta already had a `Standalone` platform entry, so this was a line edit (`maxTextureSize` and `overridden` inside the right `buildTarget` block, done with awk to keep the line endings), not an insertion of new YAML blocks. A 512 Default with every texture except the enemy skeleton's had been tried first: the PC build went to 199 MB (textures 85 MB).

## WebGL build (Unity 6.6, 2026-10-20)

- Batch mode has no `-buildWebGLPlayer` flag. A temporary editor script in `Assets/Editor` calling `BuildPipeline.BuildPlayer` with `BuildTarget.WebGL` worked; delete it (and its `.meta`) afterwards. The first WebGL build switches platform and reimports every texture, so it takes a while.
- Result: 66 MB to download (Brotli), textures 48 MB, meshes 36 MB, shaders 12 MB. The largest single asset is `3D_Reaper/reaper.fbx` (14 MB).
- `PlayerSettings.WebGL.decompressionFallback` is on, so the build also works on servers that do not send `Content-Encoding: br` (a plain `python3 -m http.server`, itch.io).
- The build leaves Burst output in a `Data/` folder at the project root; it is ignored in `.gitignore`. It also regenerates the URP assets' shader prefiltering and fills `Assets/DefaultVolumeProfile.asset`, as the PC build does.
- The main menu background video does not play: WebGL does not support video clips embedded in the build ("Embedded video clips are not supported on this platform"). It needs the file in `StreamingAssets` and the Video Player's URL source.
- To test it, serve `Builds/WebGL` with `python3 -m http.server 8080 --bind 127.0.0.1 --directory <build>` and open `http://localhost:8080`. Binding to `0.0.0.0` (to reach it from a phone on the same wifi) was refused by the permission system as exposing a local service; the user has to allow it. The built-in browser pane cannot be used to play it while the pane is hidden: the page runs in the background, the screenshots stay frozen, yet the game keeps running and its audio plays, so close the tab when done.
