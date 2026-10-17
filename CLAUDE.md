# der Friedhof

Top-down 3D horror game made in Unity. The player walks through a graveyard, enemies chase and kill on contact, and reaching the end trigger returns to the main menu.

- **Unity version:** 2022.3.62f3 (LTS), URP 14, TextMeshPro 3.0.7. Open the project only with this editor version; other versions are installed on the machine.
- **Scenes in the build:** `Main Menu` (index 0) and `Final Scene` (index 1). `Hector`, `Diego`, `Emma` are per-developer working scenes; `BSP` and `Emma scene(respaldo)` are leftovers.
- **Scripts:** `Assets/Scripts/`, plus `Assets/Endgame.cs` and `Assets/LimitFPS.cs` at the Assets root.

## Conventions

- Commit messages and user-facing conversation are in Spanish. Code, comments and these docs are in English.
- One concern per commit. Do not push or open a PR unless asked.
- Scene files (`.unity`) are YAML and can be edited as text for small, mechanical changes. Unity asks to reload them afterwards.
- Script files have mixed CRLF/LF line endings. Check `git diff --ignore-cr-at-eol --stat` before committing so a small change does not turn into a whole-file diff.

## Verifying changes

There are no automated tests. A change is verified by pressing Play in the editor and exercising it, then checking the Console for errors. See `docs/agent-notes/working-with-the-editor.md` for how to do that and the pitfalls.

## More notes

`docs/agent-notes/` holds lessons learned the hard way, including approaches that already failed. Read the matching note BEFORE starting the task. When you learn something new, add or update a note (in English) and its line here, and record approaches that did not work, not only the one that did.

- Finding which scene or script does what, or the game flow from menu to end → `docs/agent-notes/project-overview.md`
- Changing how enemies chase, appear, vanish or respawn, or their smoke and trails → `docs/agent-notes/enemy-behaviour.md`
- Changing the player's death, the slash effect, the enemy attack or the planted scythe → `docs/agent-notes/player-death-feature.md`
- Adding or moving menu or Game Over UI, or text wrapping one letter per line → `docs/agent-notes/ui-and-canvas.md`
- Pressing Play, driving the editor with computer use, editing scenes or scripts outside Unity, reading logs → `docs/agent-notes/working-with-the-editor.md`
- A warning, dead code or odd behaviour that might already be known → `docs/agent-notes/known-issues.md`
