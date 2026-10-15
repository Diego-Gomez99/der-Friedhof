# Working with the Unity editor

## Driving the editor (computer use)

- Several Unity versions are installed. Requesting access to the app named "Unity" resolves to a different version (6000.x), and `open_application "Unity"` launches that version instead of focusing the running editor. Request access with the full path of the running editor instead:
  `C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe`
- Do not launch the editor yourself. Ask the user to open the project and leave the editor window visible and not minimised.
- The editor loses keyboard focus easily. Start every batch of key presses with a click inside the Game view, and make sure no other window (for example File Explorer) is covering the spot you click.
- After editing a script, click the editor and wait about 15 seconds for the recompile before pressing Play. Entering Play mode takes another 10 to 20 seconds.
- Clicking an editor that already has focus does not trigger the refresh, and Play then runs the old code. Press Ctrl+R (Assets > Refresh) instead, and confirm in `Editor.log` that the changed scripts were imported (`Start importing Assets/Scripts/...`) before testing.
- Changing the default of a `[SerializeField]` in code does not change objects in the scene that is already open: they keep the value they got when the field first appeared. Write the value into the `.unity` file as well, and check it in the Inspector.
- The editor does not always offer to reload a scene edited on disk. If it does not, double-click the scene asset in the Project tab to reopen it.
- If the field was never saved into the scene file, reopening the scene (double-click it in the Project tab) is enough to pick up the new default, as long as the scene has no unsaved changes.
- Always stop Play mode when finished. Changes made in the editor during Play mode are discarded.
- Check that the editor is not in Play mode before editing scripts (the user may have left it running). A recompile during Play reloads the scripts and loses static state, so `PlayerController.FixedUpdate` throws a `NullReferenceException` every frame on `GameManager.instance`. Those errors come from the recompile, not from the change: stop Play, clear the Console and test again.
- The player (`FINALE`) cannot be moved from the Inspector in Play mode: its Rigidbody interpolates and overwrites the Transform every frame. To test a trigger without walking there, move the trigger object onto the player instead. For example, setting the end coffin to X 434.6, Y -0.5, Z 359.2 right after entering Play fires `Endgame`.
- The Scene view can start rendering everything washed out in white, even with Fog turned off in its effects menu, while the Game view looks normal. It is an editor glitch, not something in the scene: nothing in `Final Scene` or the render settings had changed, and restarting the PC fixed it. Do not go hunting for fog or post-processing changes when it happens.

## A reliable test run

1. Open `Main Menu`, press Play, click the in-game Play button.
2. In `Final Scene`, hold W for about 5 s to reach the gate, D for 1 s, W for 4 s, A for 2 s, then W for 4 s. The first enemy kills the player and the Game Over panel appears.
3. Check Play Again (restarts the level) and Main Menu (returns to the menu).
4. Open the Console tab and confirm the error count is 0.

## Logs

The editor log is at `%LOCALAPPDATA%\Unity\Editor\Editor.log`. It contains binary bytes, so search it with `grep -a`. Exceptions thrown in Play mode appear there with full stack traces.

## Editing files outside the editor

- `.unity` and `.controller` files are YAML. Small mechanical edits as text are fine; anything structural (new GameObjects, new animator states, wiring references) is safer done in the editor, because objects are linked by `fileID` and assets by `guid`.
- Never create or move an asset without its `.meta` file. Unity generates the `.meta` when it imports a new file; commit both together.
- Scripts have mixed CRLF/LF endings and `core.autocrlf` is `true`. Running `sed -i` from Git Bash strips the CRs from the whole file. Check `git diff --ignore-cr-at-eol --stat` to see the real size of a change.
- Python is not installed on this machine. Use Bash or PowerShell for scripting.

## Git

- The author identity is configured globally. The remote is on GitHub and the default branch is `main`.
- `Library/`, `Temp/`, `Logs/`, `UserSettings/` and `.vscode/` are ignored.
