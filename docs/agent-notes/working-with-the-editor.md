# Working with the Unity editor

## Driving the editor (computer use)

- Several Unity versions are installed. Requesting access to the app named "Unity" resolves to a different version (6000.x), and `open_application "Unity"` launches that version instead of focusing the running editor. Request access with the full path of the running editor instead:
  `C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe`
- Do not launch the editor yourself. Ask the user to open the project and leave the editor window visible and not minimised.
- The editor loses keyboard focus easily. Start every batch of key presses with a click inside the Game view, and make sure no other window (for example File Explorer) is covering the spot you click.
- After editing a script, click the editor and wait about 15 seconds for the recompile before pressing Play. Entering Play mode takes another 10 to 20 seconds.
- Always stop Play mode when finished. Changes made in the editor during Play mode are discarded.

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
