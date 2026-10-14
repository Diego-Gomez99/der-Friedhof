# Known issues and unfinished work

None of these block the game. They are listed so they are not mistaken for new regressions.

## Console warnings that are expected

- `The character used for Underline is not available in font asset [HorrorFont-Regular SDF]` / `[horrh___ SDF]`: the horror fonts have no underscore glyph. Harmless.
- `Color primaries 0 is unknown or unsupported by WindowsMediaFoundation`: comes from the main menu background video. Harmless.

## Scene transitions

Menu to game is done in code: `MainMenuManager.LoadGame` builds a black overlay canvas at runtime (`Scene Fade`, kept with `DontDestroyOnLoad`), darkens the button that was used, loads the scene with `LoadSceneAsync`, waits until it is ready (at least `minimumLoadTime` after the click), fades to black while fading out the menu music, activates the scene, fades back in and destroys the overlay. The coroutine runs on the overlay's `Image` because the manager is destroyed with the menu scene.

Leftovers from an earlier Animator-based attempt that was never finished:

- `GameManager` has an unused `fadedAnimator` field and a commented-out `FadedSceneLoad`.
- The Animator on the menu Canvas points to a controller that does not exist in the project or in git history.

The other scene loads (Play Again, Main Menu, the end trigger) are still synchronous `LoadScene` calls with no fade.

## Checkpoints do nothing

`Checkpoint` saves the player position to `PlayerPrefs`, but the code that restores it in `PlayerController` is commented out. Play Again always restarts from the scene's initial position.

## Other loose ends

- `PlayerController.UpdatePlayerDirection` is never called, so movement is always relative to north regardless of the active camera's `direction`.
- The disabled first entry in the build settings points to `Assets/Scenes/SampleScene.unity`, which no longer exists.
- The enemy keeps chasing and its chase audio keeps playing after the player dies.
- `Enemy.FadeOut` only waits and destroys the object; the actual fade is commented out.
