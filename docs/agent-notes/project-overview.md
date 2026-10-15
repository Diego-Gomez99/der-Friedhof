# Project overview

## Game flow

1. `Main Menu` scene: Play calls `MainMenuManager.LoadGame`, Quit Game calls `MainMenuManager.QuitGame`.
2. `Final Scene`: the player walks with WASD / arrow keys. The active camera switches as the player crosses trigger colliders.
3. Touching an enemy sets game over. The Game Over panel shows two buttons wired to `GameManager.LoadScene(int)`: Play Again loads build index 1, Main Menu loads build index 0. Both fade to black and back in.
4. Reaching the `Endgame` trigger fades back to build index 0.

## Scripts

| Script | Role |
| --- | --- |
| `PlayerController` | Movement from `Horizontal`/`Vertical` axes in `FixedUpdate`, sets Rigidbody velocity and the animator floats `speed` and `turnSpeed`. Handles trigger contacts: tag `Enemy` ends the game, while the player stands in a trigger tagged `SS`, every `FixedUpdate` kills the enemies within 10 units (tracked with enter/exit counts, because `OnTriggerStay` stops while the Rigidbody sleeps). Stops moving while `GameManager.instance.isGameOver` is true. |
| `GameManager` | Singleton (`GameManager.instance`) present in every gameplay scene. Finds the object named `Game Over Panel` in `Awake`, hides it, and shows it when `GameOver()` is called. `LoadScene` darkens the used button and fades to the scene through `SceneFader`. It ignores every call after the first, because the Game Over buttons click on press and on release. Also starts the soundtrack. |
| `Enemy` (file `EnemyAI.cs`) | Chases the object tagged `Player` when within `detectionDistance`, plays a chase clip, sets the animator bool `Persiguiendo`. `Attack()` stops it and plays its scythe swing when it kills the player. `Die()` disables its colliders and destroys it after a delay. |
| `MainMenuManager` | Menu music and the Play and Quit handlers. Play fades into the game scene and Quit fades to black before quitting, both through `SceneFader`. |
| `SceneFader` | Static helper that loads a scene behind a fade to black and back, or fades to black and quits, see `known-issues.md` (Scene transitions). |
| `MenuButtonEffect` | On the `Play` and `Quit Game` buttons of `Main Menu` and the `Play Again` and `Main Menu` buttons of the Game Over panel in `Final Scene`. Grows and brightens the label while hovered or selected. Hovering selects the button in the EventSystem. Invokes the button's `onClick` on mouse press, so the handlers run again on release and must ignore the second call. |
| `CamerasController`, `FollowCamera`, `StaticCamera` (in `CamerasScripts/`), `SwitchCameraCollider`, `CameraController` | Per-area cameras and switching between them. |
| `Checkpoint` | Saves the player position to `PlayerPrefs`. The loading side in `PlayerController` is commented out, so checkpoints currently have no effect. |
| `SlashEffect` | Full-screen slash, hit-stop, shake and slice sound played when an enemy touches the player. Created at runtime by `SlashEffect.Play`. |
| `GroundImpactEffect` | Burst of dust and dirt clods where an enemy's scythe butt first touches the ground during the attack (or where it is planted, if it never reaches the ground). Built at runtime by `GroundImpactEffect.Play`, uses the always included `Sprites/Default` shader and destroys itself after 2.5 s. |
| `WalkSteps`, `LampEffect`, `HeadFalling`, `AreaNameShow` | Footstep audio and ambience effects. |
| `LimitFPS` | Caps the frame rate at 60. |

## How the player dies

The enemy has a child named `weapon` with a `BoxCollider`, plus a `CapsuleCollider` on the root. `PlayerController.OnTriggerEnter` checks the `Enemy` tag, sets `GameManager.instance.isGameOver = true`, plays the death animation and sound, and shows the Game Over panel once the animation ends (see `player-death-feature.md`). The enemy keeps its chase behaviour.

## Assets worth knowing

- Player animator: `Assets/Animations/PlayerAnimations.controller`. A blend tree state named `Movement`, driven by `speed` and `turnSpeed`, and a `Death` state entered from Any State by the `die` trigger.
- Enemy model and animator: the 32 enemies in `Final Scene` are instances of `Assets/Animations/Enemy Animations/T-Pose.fbx` (the Dead Cowboy skeleton rigged with Mixamo, humanoid), still named `CB_squeleton_lowpolywithoutacc (n)`. The rigged model's origin is at the feet (the unrigged one had it at the body centre), so the enemy `CapsuleCollider` centre is at y = 0.909, half its height. Their controller is `Assets/Animations/CB_squeleton_lowpolywithoutacc.controller`: `idle` plays `Enemy Animations/Idle.fbx` and `EnemyWalking 0` plays `Enemy Animations/Wheelbarrow Walk (1).fbx` while `Persiguiendo` is true, at a rate set by the float `walkSpeed` (the enemy's actual horizontal Rigidbody speed divided by `WalkClipSpeed`, 1.8 m/s, so a blocked enemy freezes mid-step), and `Attack` plays `Enemy Animations/Enemy Attack.fbx` on the `attack` trigger. `Enemy.LateUpdate` overrides the right arm after the Animator so it points ahead, opened `armOpenAngle` degrees to the enemy's right, turns the scythe (`weapon`, a child of the enemy root) `weaponTurnAngle` degrees to the right and moves it so that `weaponGripPoint` sits in the right hand. The scythe's `BoxCollider` is the trigger that kills the player; its centre is shifted back every frame so the kill zone stays at the authored height (at shoulder height it passes over the player). The unrigged `Quixel/.../DeadCowboy/CB_squeleton_lowpolywithoutacc.fbx` has no bones and cannot be animated; `EnemyWalking.anim` is an empty clip.
- Safe spots: the six `SS` objects in `Final Scene` are instances of `Assets/Prefabs/SS.prefab`, a ProBuilder hexagon whose `MeshRenderer` is disabled, so only its trigger `MeshCollider` is left. Each one sits under a `StreetLampRound2A` (unnamed, `(5)`, `(6)`, `(8)`, `(9)`, `(10)`) whose `Spot Light` and `Point Light` are overridden in the scene to green, with a 75° spot angle, so the lamp marks the spot by casting a green pool on the ground. The other lamps keep the prefab's warm light.
- Animation clips: `Assets/Animations/` (Mixamo-style FBX: idle, walking, running, strafes, turns, jump, dying).
- Audio: `Assets/Audios/` has the footsteps and the player death scream. `Assets/Horror Elements/` has ambience and hit sounds (for example `Hits/Hit_Drama.wav`).
- Fonts: the horror fonts in `Assets/Fonts/` are TextMeshPro SDF assets in dynamic mode, so TMP adds glyphs from the `.ttf` on demand. The character sets are small: `horrh___` (Game Over title) has letters, digits, space and `! $ . ?`; `HorrorFont-Regular` (buttons) has only letters and space. Other characters, such as `-`, are drawn with TMP's default font, LiberationSans.
- Both `.ttf` files were edited to add an underscore, because TMP warns about a missing Underline character on every text that uses a font without one. The `_` is the last glyph in each font, so existing glyph indices, which the font assets store, did not change. The optional `hdmx` (`horrh___`) and `DSIG` (`HorrorFont-Regular`) tables were dropped because they were no longer valid. Any further edit to these fonts should also append glyphs at the end.
- TMP remembers characters it failed to find in a font until the next domain reload. After changing a `.ttf`, enter Play mode (or recompile) before judging whether a font warning is gone.
- Unity's native font engine keeps the previously loaded `.ttf` in memory even after the file is reimported, and reimporting the `SDF.asset` does not clear it. If an existing glyph's outline changes, discard the glyph TMP already added to the `SDF.asset` (`git checkout` it) and restart the editor before checking the result; otherwise the old outline is drawn into the atlas again. Check the glyph's `m_Metrics` in the `.asset` diff to confirm which version was used.
