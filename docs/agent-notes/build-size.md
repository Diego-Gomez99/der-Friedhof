# Build size

Measured on 2026-10-20 with a Windows 64-bit build of `Main Menu` and `Final Scene`.

## Building from the command line

With the editor closed, this builds the scenes listed in Build Settings without any script:

    "C:/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Unity.exe" -batchmode -quit -nographics -projectPath <project> -buildTarget Win64 -buildWindows64Player "<project>/Builds/Windows/der Friedhof.exe" -logFile "<project>/Builds/build.log"

It takes a few minutes. `Builds/` is ignored by git. The log contains a "Build Report" with the size by category and the biggest assets. A batch-mode run rewrites the URP assets in `Assets/Settings/` (shader prefiltering fields) and `ProjectSettings.asset`; revert them with `git checkout` if they were not meant to change.

## Where the size goes

- Full build: 1.8 GB. Meshes 907 MB, textures 521 MB, sounds 6 MB, scripts 0.2 MB.
- The meshes are almost all static batching. `Final Scene` has 285 static renderers, mostly Megascans props, and static batching writes a combined copy of their vertices into the scene data (`level1.resS`, 930 MB). The same build with static batching turned off was 635 MB, with meshes at 27 MB. Turning it off may cost draw calls; SRP Batcher in URP already reduces most of that cost.
- Textures: about a hundred 2K Megascans maps (albedo, normal, masks, AO) at about 5.3 MB each once compressed, plus the 24 MB `AllSkyFree` night sky. 115 textures still have an 8192 max size in their import settings. With the camera always top-down, 1K is likely enough for most of them.
- Unused assets in `Assets/` (for example most of `Horror Elements/Ambient`, 115 MB of WAV) do not reach the build; only what the two scenes reference is included.
