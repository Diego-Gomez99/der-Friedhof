# Known issues and unfinished work

None of these block the game. They are listed so they are not mistaken for new regressions.

## Console warnings that are expected

- `The character used for Underline is not available in font asset [HorrorFont-Regular SDF]` / `[horrh___ SDF]`: the horror fonts have no underscore glyph. Harmless.
- `Color primaries 0 is unknown or unsupported by WindowsMediaFoundation`: comes from the main menu background video. Harmless.

## Unfinished scene transition

`MainMenuManager` has a `transitionAnimator` field and a `SceneLoad` coroutine that triggers `StartTransition`, and `GameManager` has a commented-out `FadedSceneLoad`. The fade was never finished:

- `transitionAnimator` is not assigned in the `Main Menu` scene.
- The Animator on the menu Canvas points to a controller that does not exist in the project or in git history.
- No animator controller defines a `StartTransition` trigger.

`LoadGame` now loads the scene directly when no animator is assigned, so there is no error. Finishing the fade means creating the controller and clips and assigning the reference.

## Checkpoints do nothing

`Checkpoint` saves the player position to `PlayerPrefs`, but the code that restores it in `PlayerController` is commented out. Play Again always restarts from the scene's initial position.

## Other loose ends

- `PlayerController.UpdatePlayerDirection` is never called, so movement is always relative to north regardless of the active camera's `direction`.
- The disabled first entry in the build settings points to `Assets/Scenes/SampleScene.unity`, which no longer exists.
- The enemy keeps chasing and its chase audio keeps playing after the player dies.
- `Enemy.FadeOut` only waits and destroys the object; the actual fade is commented out.
