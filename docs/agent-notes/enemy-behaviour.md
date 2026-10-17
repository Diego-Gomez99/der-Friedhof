# Enemy behaviour

`Enemy` lives in `Assets/Scripts/EnemyAI.cs`. The model, prefab and animator are described in `project-overview.md` (Assets worth knowing); the attack and the planted scythe in `player-death-feature.md` (Enemy attack).

## Chasing

The enemy chases the object tagged `Player` when it is within `DetectionDistance`, plays a chase clip and sets the animator bool `Persiguiendo`. `Attack()` stops it and plays its scythe swing when it kills the player. `Die()` disables its colliders and destroys it after a delay.

## Visibility

- An enemy is invisible (all its renderers, scythe included, are off) and its scythe trigger is disabled, so it cannot kill, until the player comes within `DetectionDistance`. Attacking and dead enemies stay visible.
- Only one living enemy is shown at a time: the closest to the player among those that could chase it (in range or already shown, out of cooldown, and not blocked by `RespawnMinDistance`). This is worked out once per frame in `Enemy.ClosestCandidate` from a static list of enemies.
- Once shown it stays at least 1 s, so a player at the edge of the range does not set off a puff every frame. Its visible time counts from `appearTime`.

## Vanishing and coming back

A shown enemy keeps chasing until one of these happens:

| Cause | What follows |
| --- | --- |
| The player is more than `VanishDistance` (40 m) away | `VanishCooldown` (3 s). Once the smoke hides it, with `TeleportChance` (25 %) it is moved to the player's position; otherwise it stays where it vanished (the player walks through hidden enemies, see `ApplyVisibility`). After the cooldown, `RespawnMinDistance` keeps it hidden until the player has walked that far from it. |
| `MaxVisibleTime` (30 s) visible in a row | It vanishes where it stands and, for `RespawnCooldown` (10 s), neither chases nor shows up. After that it comes back only with the player at least `RespawnMinDistance` (5 m) away, so it does not appear on top of the player. |
| A closer enemy takes over | The shown enemy vanishes and the new one appears once it has (after the shown one's minimum visible time). The one that gave way gets `YieldCooldown` (1 s), and then `RespawnMinDistance` applies to it too. |

After any enemy vanishes, no enemy may appear for `AppearAfterVanishDelay` (`SmokeEffect.CoverDelay`, 0.2 s, shared by all enemies through a static `lastVanishTime`).

## Smoke and trails

- Each switch plays `SmokeEffect` and happens `SmokeEffect.CoverDelay` (0.2 s) later, once the smoke covers the body. Hiding disarms the scythe at once.
- The position where the last enemy vanished is kept in a static `lastVanishPosition` (cleared when the first enemy of a newly loaded scene is enabled). When a living enemy appears at least 1 m from it, `SmokeEffect.PlayTrail` draws a faint thread of black smoke from there to the new enemy.
- The trail goes first: the new enemy stands still and hidden while it crosses (`SmokeEffect.TrailTravelTime`, 0.3 s) and for `AppearAfterTrailPause` (0.5 s) more, until `appearTime`. Only then its puff plays, and it is shown `CoverDelay` later.
- A living enemy turns to face the player as it starts appearing. A second trail (`holdTime` 0), the weapon sweep, traces the scythe's outline through `WeaponSweepPoints` to hint at its size: from 1 m to its right, at the height of the other trail, to 4 m ahead and 1 m up (the handle), then 2 m left, 1 m back and 2 m up (the blade). It starts when the first trail arrives, or together with the puff when there is no trail.

## Constants, not serialized fields

The visibility values, `DetectionDistance`, the speeds, the chase volume and the arm pose are constants in `EnemyAI.cs`, the same for every enemy. A change in code reaches every enemy without touching the scene or the prefab.

## Debugging in Play mode

- `OnDrawGizmos` outlines hidden enemies (Scene view, and Game view with Gizmos on): white while waiting for the player, orange during the cooldown, red once the cooldown is over but the player is still closer than `RespawnMinDistance`.
- A label above the enemy (`Handles.Label`, inside `#if UNITY_EDITOR`) shows the seconds until it vanishes, until the cooldown ends, or "Player too close".
- The scythe's kill trigger is drawn as a red box while it is armed.

## What doesn't work

- **`[SerializeField]` for tuning values.** A changed default does not reach the 32 enemies already in the scenes: they keep the value they got when the field first appeared (see `working-with-the-editor.md`). That is why these values are constants.
- **`OnTriggerStay` for the safe spots.** It stops firing while the player's Rigidbody sleeps, so `PlayerController` counts trigger enters and exits instead (see `project-overview.md`).
- **Following the right hand's rotation with the scythe during the attack.** It copies the clip's sword moves, and the backswing spins the long scythe and drags the blade along the ground. The angle comes from the hand's height instead (see `player-death-feature.md`).
