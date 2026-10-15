# Known issues and unfinished work

None of these block the game. They are listed so they are not mistaken for new regressions.

## Console warnings that are expected

- `Project ID request failed ... 404` and `Unknown Unity Connect error (400)` on editor startup: `ProjectSettings.asset` is still linked to a Unity Cloud project (`cloudProjectId`) that no longer exists or is not accessible. Cloud services are off (`cloudEnabled: 0`). Unlinking it in Project Settings > Services removes the message.

## Scene transitions

Menu to game, both Game Over buttons and the end trigger go through `SceneFader.LoadScene`. It builds a black overlay canvas at runtime (`Scene Fade`, kept with `DontDestroyOnLoad`), loads the scene with `LoadSceneAsync`, waits until it is ready (at least `minimumLoadTime` after the click: 2 s from the menu, 0 from Game Over), fades to black while fading out the given music, activates the scene, fades back in and destroys the overlay. The coroutine runs on the overlay's `Image` because the caller is destroyed with the old scene. Before loading, the caller runs `MenuButtonEffect.LockAll`, which darkens the button that was used and stops the hover effect on all of them.

Leftovers from an earlier Animator-based attempt that was never finished:

- `GameManager` has an unused `fadedAnimator` field and a commented-out `FadedSceneLoad`.
- The Animator on the menu Canvas points to a controller that does not exist in the project or in git history.

Quit Game uses `SceneFader.Quit`: it fades to black with the menu music, then stops Play mode in the editor (`#if UNITY_EDITOR`) or calls `Application.Quit` in a build.

`Endgame` calls `GameManager.LoadScene(0)`, so it shares the fade and the guard against repeated calls. Two objects in `Final Scene` have it: the coffin `Roman Stone Coffin_wj3keafqx (23)`, whose trigger box is the real end of the level, and `reaper (1)`, which has no collider of its own, so its `Endgame` never fires.

## Checkpoints do nothing

`Checkpoint` saves the player position to `PlayerPrefs`, but the code that restores it in `PlayerController` is commented out. Play Again always restarts from the scene's initial position.

## Other loose ends

- `PlayerController.UpdatePlayerDirection` is never called, so movement is always relative to north regardless of the active camera's `direction`.
- The disabled first entry in the build settings points to `Assets/Scenes/SampleScene.unity`, which no longer exists.
- The other enemies keep chasing and their chase audio keeps playing after the player dies. Only the one that killed the player stops.
- `Enemy.FadeOut` only waits and destroys the object; the actual fade is commented out.
- The main menu background video (`Assets/Videos/MainMenuVideo.mp4`) plays but is never seen. Its `VideoPlayer` renders onto the `Quad` (Material Override), but the Quad sits at (955, 550, 0) with scale 1920x1080, far outside the view of `Main Camera` at (0, 1, -10). Its material (GUID `31321ba15b8f8eb4c954353edc038b1d`) is also missing from the project, and the Screen Space Overlay canvas draws on top of the 3D scene in any case.
- That video is H.264 Constrained Baseline with BT.709 color tags (in the SPS and a `colr` box), imported with Transcode off. Keep it that way if it is replaced: Unity's transcoder drops the color tags (`Color primaries 0 is unknown or unsupported by WindowsMediaFoundation`), and a High profile file played without transcoding logs `Unexpected timestamp values detected`.
