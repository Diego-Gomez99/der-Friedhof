# Player death animation and sound

Both are implemented.

## How it works

1. `PlayerController.OnTriggerEnter` sees the `Enemy` tag and calls `EndGame` with the `Enemy` the collider belongs to (`GetComponentInParent`).
2. `EndGame` returns early if `GameManager.instance.isGameOver` is already true, because the enemy's weapon collider and capsule collider can both fire the trigger. Otherwise it sets `isGameOver = true`, calls `Attack()` on that enemy, starts the slash effect (below), sets the animator trigger `die`, plays `deathSound` and starts the `ShowGameOverAfterDeath` coroutine.
3. `PlayerController.FixedUpdate` zeroes the velocity and the `speed` / `turnSpeed` floats while `isGameOver` is true, so input is ignored during the animation.
4. After `gameOverDelay` seconds (serialized on `PlayerController`, default 3.5) the coroutine calls `GameManager.GameOver()`, which shows the Game Over panel.

`isGameOver` therefore means "the player is dead", not "the panel is visible". `GameManager` no longer shows the panel from `Update`.

## Assets

- **Clip:** `Assets/Animations/Dying.fbx`, the Mixamo "Dying" clip downloaded without skin, 105 frames at 30 fps. It is imported as Humanoid with the avatar created from the model itself (the other clips copy the character's avatar). Loop Time is off, and root rotation and position are baked into the pose because the player's Animator has Apply Root Motion off; without the bake the body would not drop to the ground.
- **Animator:** `PlayerAnimations.controller` has a `Death` state playing that clip, a `die` trigger, and a transition from Any State with no exit time. Nothing transitions out of `Death`; reloading the scene resets it.

If the clip is replaced, set `gameOverDelay` to the new clip's length.

- **Sound:** `Assets/Audios/player-death.mp3` (Pixabay "Man death scream", Pixabay Content License), assigned to `deathSound` on the `PlayerController` in `Assets/Prefabs/FINALE.prefab`. The scream is roughly the first second; the rest of the 3 s file is silence. Preload Audio Data is on in its import settings; with it off the clip loaded on first play and the scream came late.
- **Playback:** the sound plays through the player's `AudioSource`, the one `WalkSteps` uses for footsteps. That component is added to the player instance in the scene, not in the prefab; without it the death is silent. `deathSoundStartOffset` (default 0) skips that many seconds at the start of the clip, which is why the code sets `clip` and `time` and calls `Play` instead of `PlayOneShot`.

## Slash effect

`SlashEffect.Play(slashImpactSound, target, attacker)` (`Assets/Scripts/SlashEffect.cs`) shows a full-screen pixel-art hit when the enemy touches the player. `target` and `attacker` are the player and enemy positions one unit above the feet. It builds its own overlay canvas at runtime and destroys itself after about 1.7 s, so nothing in the scenes references it.

- **Visuals:** a white frame, a black impact frame and a red tint that fades out; one crescent slash drawn across the whole screen through the player, perpendicular to the on-screen direction from the enemy to the player (the camera used is the enabled game camera with the highest depth); red and white shards snapped to a 6 px grid. The textures are generated in code with point filtering.
- **Hit-stop:** `Time.timeScale` is 0 for the first 0.18 real seconds, so the death animation and the enemy freeze on impact. The effect uses unscaled time, and `OnDestroy` restores the time scale if the scene unloads early. The Game Over delay is scaled time, so it starts counting after the freeze.
- **Shake:** the cameras are moved by `FollowCamera` and others in `LateUpdate`, so the shake offsets the camera only inside `RenderPipelineManager.beginCameraRendering` and puts it back in `endCameraRendering`. The slash and shards also shake on the canvas.
- **Sound:** the slash plays a slice synthesized in code (noise swish, metallic ring, low thump, soft-clipped). `slashImpactSound` on the `PlayerController` in `FINALE.prefab` is `Horror Elements/Hits/Hit_Drama.wav`, played once on impact. All of it is 2D at full volume.

Timings, sizes and colours are constants at the top of the file and inside `Run` and `Slash`.

## Enemy attack

`Enemy.Attack()` stops the enemy that killed the player, stops its chase audio, turns it to face the player and sets the animator trigger `attack`. `CB_squeleton_lowpolywithoutacc.controller` has an `Attack` state reached from Any State by that trigger, with no exit; it plays `Enemy Animations/Enemy Attack.fbx` (the Mixamo "Injured Idle", 281 frames at 30 fps, 9.33 s), imported as Humanoid with its own avatar, Loop Time on and root motion baked into the pose. Only its content was swapped in, so the `.meta` and the GUID the controller references stay the same.

While attacking, `Enemy.LateUpdate` no longer aims the right arm. The scythe instead keeps the rotation it had relative to the right hand on the frame the attack started and its grip stays in the hand, so it swings with the clip. The hit-stop freezes the first 0.18 s of the swing.

## Still open

- The other enemies keep chasing and their chase audio keeps playing while the player dies. The one that killed the player stops (see Enemy attack above).
- `gameOverDelay` uses its script default in every scene. If it is overridden in one scene's Inspector, the other gameplay scenes (`Hector`, `Diego`, `Emma`) keep the default.

## Verifying

Use the test run in `working-with-the-editor.md`: it reaches the first enemy in about 16 seconds of input. Confirm the animation and the sound play once, input is ignored afterwards, the Game Over panel appears after the animation, and Play Again resets everything.
