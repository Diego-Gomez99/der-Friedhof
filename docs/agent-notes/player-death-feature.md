# Player death animation and sound

The death animation is implemented. The death sound is not.

## How it works

1. `PlayerController.OnTriggerEnter` sees the `Enemy` tag and calls `EndGame`.
2. `EndGame` returns early if `GameManager.instance.isGameOver` is already true, because the enemy's weapon collider and capsule collider can both fire the trigger. Otherwise it sets `isGameOver = true`, sets the animator trigger `die` and starts the `ShowGameOverAfterDeath` coroutine.
3. `PlayerController.FixedUpdate` zeroes the velocity and the `speed` / `turnSpeed` floats while `isGameOver` is true, so input is ignored during the animation.
4. After `gameOverDelay` seconds (serialized on `PlayerController`, default 3.5) the coroutine calls `GameManager.GameOver()`, which shows the Game Over panel.

`isGameOver` therefore means "the player is dead", not "the panel is visible". `GameManager` no longer shows the panel from `Update`.

## Assets

- **Clip:** `Assets/Animations/Dying.fbx`, the Mixamo "Dying" clip downloaded without skin, 105 frames at 30 fps. It is imported as Humanoid with the avatar created from the model itself (the other clips copy the character's avatar). Loop Time is off, and root rotation and position are baked into the pose because the player's Animator has Apply Root Motion off; without the bake the body would not drop to the ground.
- **Animator:** `PlayerAnimations.controller` has a `Death` state playing that clip, a `die` trigger, and a transition from Any State with no exit time. Nothing transitions out of `Death`; reloading the scene resets it.

If the clip is replaced, set `gameOverDelay` to the new clip's length.

## Still open

- **Sound:** there is no death sound. `Assets/Horror Elements/Hits/` has candidates. Check whether the player object already has an `AudioSource` (`WalkSteps` plays footsteps) before adding another.
- The enemy keeps chasing and its chase audio keeps playing while the player dies.
- `gameOverDelay` uses its script default in every scene. If it is overridden in one scene's Inspector, the other gameplay scenes (`Hector`, `Diego`, `Emma`) keep the default.

## Verifying

Use the test run in `working-with-the-editor.md`: it reaches the first enemy in about 16 seconds of input. Confirm the animation plays once, input is ignored afterwards, the Game Over panel appears after the animation, and Play Again resets everything.
