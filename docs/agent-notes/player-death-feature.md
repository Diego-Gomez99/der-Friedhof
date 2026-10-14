# Player death animation and sound

Both are implemented.

## How it works

1. `PlayerController.OnTriggerEnter` sees the `Enemy` tag and calls `EndGame`.
2. `EndGame` returns early if `GameManager.instance.isGameOver` is already true, because the enemy's weapon collider and capsule collider can both fire the trigger. Otherwise it sets `isGameOver = true`, sets the animator trigger `die`, plays `deathSound` and starts the `ShowGameOverAfterDeath` coroutine.
3. `PlayerController.FixedUpdate` zeroes the velocity and the `speed` / `turnSpeed` floats while `isGameOver` is true, so input is ignored during the animation.
4. After `gameOverDelay` seconds (serialized on `PlayerController`, default 3.5) the coroutine calls `GameManager.GameOver()`, which shows the Game Over panel.

`isGameOver` therefore means "the player is dead", not "the panel is visible". `GameManager` no longer shows the panel from `Update`.

## Assets

- **Clip:** `Assets/Animations/Dying.fbx`, the Mixamo "Dying" clip downloaded without skin, 105 frames at 30 fps. It is imported as Humanoid with the avatar created from the model itself (the other clips copy the character's avatar). Loop Time is off, and root rotation and position are baked into the pose because the player's Animator has Apply Root Motion off; without the bake the body would not drop to the ground.
- **Animator:** `PlayerAnimations.controller` has a `Death` state playing that clip, a `die` trigger, and a transition from Any State with no exit time. Nothing transitions out of `Death`; reloading the scene resets it.

If the clip is replaced, set `gameOverDelay` to the new clip's length.

- **Sound:** `Assets/Audios/player-death.mp3` (Pixabay "Man death scream", Pixabay Content License), assigned to `deathSound` on the `PlayerController` in `Assets/Prefabs/FINALE.prefab`. The scream is roughly the first second; the rest of the 3 s file is silence. Preload Audio Data is on in its import settings; with it off the clip loaded on first play and the scream came late.
- **Playback:** the sound plays through the player's `AudioSource`, the one `WalkSteps` uses for footsteps. That component is added to the player instance in the scene, not in the prefab; without it the death is silent. `deathSoundStartOffset` (default 0) skips that many seconds at the start of the clip, which is why the code sets `clip` and `time` and calls `Play` instead of `PlayOneShot`.

## Still open

- The enemy keeps chasing and its chase audio keeps playing while the player dies.
- `gameOverDelay` uses its script default in every scene. If it is overridden in one scene's Inspector, the other gameplay scenes (`Hector`, `Diego`, `Emma`) keep the default.

## Verifying

Use the test run in `working-with-the-editor.md`: it reaches the first enemy in about 16 seconds of input. Confirm the animation and the sound play once, input is ignored afterwards, the Game Over panel appears after the animation, and Play Again resets everything.
