# Upgrade from 2022.3 to Unity 6

Done on 2026-10-20 on the branch `actualizar-unity-6`, in a separate git worktree (`../der-Friedhof-unity6`) so the 2022.3 checkout stayed usable side by side. Upgraded to 6000.6.5f1 (Unity 6.6, a Supported release). Unity 6.7 LTS is expected in Q4 2026; moving to it should be a small step.

## Why

Official WebGL support on mobile browsers (2022.3 has none), WebGPU, the Build Analysis window, and support beyond 2022.3. Unity 6.3 LTS was the alternative; 6.6 was chosen because it was already installed with the Web module and 6.7 LTS is close.

## How

- Copying objects between a 2022.3 editor and a Unity 6 editor was considered and rejected: the editor clipboard does not cross editor instances, scenes reference assets by `guid`, and tags, input axes, layers, physics and lighting settings live outside the scene.
- The first open was done in batch mode so every error lands in one log: `Unity.exe -batchmode -quit -nographics -accept-apiupdate -projectPath <worktree> -logFile <worktree>/Logs/upgrade.log`. Without `-accept-apiupdate` the script updater does not run in batch mode.

## What changed

- The script updater replaced `Rigidbody.velocity` with `linearVelocity` in `EnemyAI.cs` and `PlayerController.cs`.
- `FindObjectsOfType` and `FindObjectOfType` are obsolete. In 6.6 `FindObjectsByType(FindObjectsSortMode)` and `FindObjectsSortMode` are obsolete too, so the code uses `FindObjectsByType<T>()` and `FindAnyObjectByType<T>()`. None of the callers depended on the order.
- TextMeshPro is no longer a package; it is part of `com.unity.ugui`. `com.unity.ide.vscode` no longer exists and was removed from the manifest.
- URP 14 went to URP 17. The project has no custom renderer features or hand-written shaders, so nothing broke. Unity added `Assets/DefaultVolumeProfile.asset` and rendering layers to `TagManager.asset`.
- Batch mode does not open scenes, so `.unity` files are upgraded when they are first opened and saved in the editor.

## Known, harmless log lines

- `Rig Error: Invalid copied Avatar Rig Configuration: No human bone found` for `Quixel/.../DeadCowboy/CB_squeleton_lowpolywithoutacc.fbx`: the unrigged skeleton, which the game does not use (see `project-overview.md`).
- A stack trace through `RenderPipelineGlobalSettingsUtils:TryEnsure`: an info log of URP saving its global settings, not an exception.
- Obsolete `PlayerSettings.*ScriptingDefineSymbolsForGroup` warnings in `Quixel/Scripts/BridgeImporter`: third-party, editor-only.

## Opening it in the editor for the first time

- A "URP Material upgrade" dialog appears: press OK, then File > Save Project so the upgraded materials are written to disk.
- The editor opens an empty `Untitled` scene, because the new folder has no last-opened scene.
- Texts render as solid red blocks until TMP Importer > Import TMP Essentials is run (not Examples & Extras): the shaders in `Assets/TextMesh Pro` were the old version. `TMP Settings.asset` had never been customised, so overwriting it was safe.
- The Quixel Bridge plugin window opens by itself once; close it. Its "Import Resolution: 8192" is where the 8192 max texture sizes on Quixel textures came from.
- Scenes are not rewritten until someone saves them with a change, so the first commit that touches each scene after the upgrade will have a larger diff than usual.

## Tree_Packs external materials

Unity 6 warned `MaterialLocation.External is obsolete. External Material Location is no longer supported` for the 12 FBX models in `Tree_Packs/URP_Tree_Pack/Models/` (3 trees, 4 LODs each). Their metas were switched to `materialLocation: 1` (materials embedded in the model) with `externalObjects` remapping the FBX materials `Leaf` and `Trunk` to the `Leaf.mat` and `Trunk.mat` in each tree folder, the same materials they resolved to before. This cannot change how the trees look in the game: `Final Scene` and the `URP_Tree_*` prefabs only take the mesh from the FBX and store their own material references on each renderer. The material names inside the FBX files can be read with `grep -a Material::` because they are ASCII FBX.
