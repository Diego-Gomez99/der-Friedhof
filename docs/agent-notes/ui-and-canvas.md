# UI and canvas layout

## How the menus are built

The Main Menu and Game Over UI were authored by hand at 1920x1080. Titles and buttons use stretch anchors (`AnchorMin 0,0` / `AnchorMax 1,1`) with large fixed negative `SizeDelta` values, so each element's size is "canvas size minus a constant". For example the Play button has `SizeDelta.x = -1577`, which is 343 px wide on a 1920 px canvas.

The `Play` and `Quit Game` buttons of `Main Menu` no longer follow this pattern: they are centre-anchored with fixed sizes (220x100 and 540x100). As stretched rects they grew with the canvas on wide aspect ratios until their click areas overlapped. The title and the Game Over UI are still stretched.

## The bug this caused

With the Canvas Scaler on Constant Pixel Size, any resolution narrower than about 1600 px made those widths negative. TextMeshPro then wrapped the text one letter per line, so buttons looked vertical and the title disappeared. This showed up in the editor whenever the Game view was on Free Aspect, and looked like a regression from the Unity upgrade although it was not.

## Current setup

In `Main Menu`, `Final Scene`, `Hector`, `Diego` and `Emma`, the Canvas Scaler is:

- UI Scale Mode: Scale With Screen Size
- Reference Resolution: 1920x1080
- Screen Match Mode: Expand

Expand guarantees the canvas is never smaller than 1920x1080 on either axis, so the subtracted constants can never produce a negative size.

## Things to keep in mind

- On aspect ratios wider than 16:9 the canvas is wider than 1920, so stretch-anchored elements get wider. Two-line button labels ("PLAY AGAIN", "MAIN MENU") then fit on one line. At exactly 16:9 they wrap to two lines as originally designed.
- New UI elements should use fixed-size, centre-anchored rects rather than copying the stretch-plus-negative-SizeDelta pattern.
- Always check UI changes in the Game view on both Full HD (1920x1080) and Free Aspect.
