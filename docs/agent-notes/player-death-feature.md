# Starting points: player death animation and sound

Nothing here is implemented yet. This is what exists today and what the feature will need.

## Current behaviour

`PlayerController.OnTriggerEnter` sees the `Enemy` tag and calls `EndGame`, which sets `GameManager.instance.isGameOver = true`. On the next frame `GameManager.Update` shows the Game Over panel, and `PlayerController.FixedUpdate` zeroes the velocity and the `speed` / `turnSpeed` animator floats. The player simply stands still under the Game Over text.

## What is missing

- **Animation clip:** there is no death or fall clip in `Assets/Animations/`. The existing clips are Mixamo-style FBX files, so a death clip from the same source will fit the rig. The user has to supply or choose it; check the import settings (rig type and avatar) of an existing clip such as `idle.fbx` and match them.
- **Animator state:** `PlayerAnimations.controller` has a single blend-tree state `Movement` and two float parameters. It needs a death state, a trigger parameter, and a transition from Any State with no exit time. This is easier to do in the Animator window than by editing YAML.
- **Sound:** there is no death sound. `Assets/Horror Elements/Hits/` has candidates. Check whether the player object already has an `AudioSource` (`WalkSteps` plays footsteps) before adding another.

## Design points to settle with the user

- The Game Over panel appears immediately today. With a death animation it probably should wait until the animation ends, which means the "player is dead" moment and the "show Game Over" moment become two separate steps in `GameManager`. `GameManager.GameOver()` already exists but nothing calls it.
- `OnTriggerEnter` can fire more than once (weapon collider and capsule collider). Guard so the animation and sound play only once.
- Decide whether the enemy should stop chasing and its chase audio should stop when the player dies.
- The same scripts run in `Final Scene`, `Hector`, `Diego` and `Emma`. Only `Final Scene` is in the build, but changes to serialized fields need assigning in each scene that should keep working.

## Verifying

Use the test run in `working-with-the-editor.md`: it reaches the first enemy in about 16 seconds of input. Confirm the animation plays once, the sound plays once, input is ignored afterwards, the Game Over panel appears, and Play Again resets everything.
