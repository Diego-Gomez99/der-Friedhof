# Project overview

## Game flow

1. `Main Menu` scene: Play calls `MainMenuManager.LoadGame`, Quit Game calls `MainMenuManager.QuitGame`.
2. `Final Scene`: the player walks with WASD / arrow keys. The active camera switches as the player crosses trigger colliders.
3. Touching an enemy sets game over. The Game Over panel shows two buttons wired to `GameManager.LoadScene(int)`: Play Again loads build index 1, Main Menu loads build index 0.
4. Reaching the `Endgame` trigger loads build index 0.

## Scripts

| Script | Role |
| --- | --- |
| `PlayerController` | Movement from `Horizontal`/`Vertical` axes in `FixedUpdate`, sets Rigidbody velocity and the animator floats `speed` and `turnSpeed`. Handles trigger contacts: tag `Enemy` ends the game, tag `SS` kills enemies within 10 units. Stops moving while `GameManager.instance.isGameOver` is true. |
| `GameManager` | Singleton (`GameManager.instance`) present in every gameplay scene. Finds the object named `Game Over Panel` in `Awake`, hides it, and shows it when `GameOver()` is called. Also starts the soundtrack. |
| `Enemy` (file `EnemyAI.cs`) | Chases the object tagged `Player` when within `detectionDistance`, plays a chase clip, sets the animator bool `Persiguiendo`. `Die()` disables its colliders and destroys it after a delay. |
| `MainMenuManager` | Menu music, Play and Quit handlers, and the fade into the game scene. |
| `MenuButtonEffect` | On the `Play` and `Quit Game` buttons of `Main Menu`. Grows and brightens the label while hovered or selected. Hovering selects the button in the EventSystem. Invokes the button's `onClick` on mouse press, so the handlers run again on release and must ignore the second call. The Game Over buttons do not have it. |
| `CamerasController`, `FollowCamera`, `StaticCamera` (in `CamerasScripts/`), `SwitchCameraCollider`, `CameraController` | Per-area cameras and switching between them. |
| `Checkpoint` | Saves the player position to `PlayerPrefs`. The loading side in `PlayerController` is commented out, so checkpoints currently have no effect. |
| `WalkSteps`, `LampEffect`, `HeadFalling`, `AreaNameShow` | Footstep audio and ambience effects. |
| `LimitFPS` | Caps the frame rate at 60. |

## How the player dies

The enemy has a child named `weapon` with a `BoxCollider`, plus a `CapsuleCollider` on the root. `PlayerController.OnTriggerEnter` checks the `Enemy` tag, sets `GameManager.instance.isGameOver = true`, plays the death animation and sound, and shows the Game Over panel once the animation ends (see `player-death-feature.md`). The enemy keeps its chase behaviour.

## Assets worth knowing

- Player animator: `Assets/Animations/PlayerAnimations.controller`. A blend tree state named `Movement`, driven by `speed` and `turnSpeed`, and a `Death` state entered from Any State by the `die` trigger.
- Enemy model and animator: the 32 enemies in `Final Scene` are instances of `Assets/Animations/Enemy Animations/T-Pose.fbx` (the Dead Cowboy skeleton rigged with Mixamo, humanoid), still named `CB_squeleton_lowpolywithoutacc (n)`. Their controller is `Assets/Animations/CB_squeleton_lowpolywithoutacc.controller`: `idle` plays `Enemy Animations/Idle.fbx` and `EnemyWalking 0` plays `Enemy Animations/Wheelbarrow Walk (1).fbx` while `Persiguiendo` is true. The scythe (`weapon`) is a child of the enemy root, not of the hand bone. The unrigged `Quixel/.../DeadCowboy/CB_squeleton_lowpolywithoutacc.fbx` has no bones and cannot be animated; `EnemyWalking.anim` is an empty clip.
- Animation clips: `Assets/Animations/` (Mixamo-style FBX: idle, walking, running, strafes, turns, jump, dying).
- Audio: `Assets/Audios/` has the footsteps and the player death scream. `Assets/Horror Elements/` has ambience and hit sounds (for example `Hits/Hit_Drama.wav`).
- Fonts: the horror fonts in `Assets/Fonts/` are TextMeshPro SDF assets with a limited character set.
