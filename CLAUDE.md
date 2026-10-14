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

Read these before starting work in the matching area:

- `docs/agent-notes/project-overview.md` — scenes, scripts and game flow
- `docs/agent-notes/ui-and-canvas.md` — how the menus are laid out and why they used to break
- `docs/agent-notes/working-with-the-editor.md` — driving the Unity editor, line endings, logs
- `docs/agent-notes/known-issues.md` — existing warnings, dead code and unfinished features
- `docs/agent-notes/player-death-feature.md` — starting points for the player death animation and sound
