# PZA's UI Probe for Meitou

> **Version 0.3.3** (2026-11-19): built on PZA's Meitou UI 0.3.2 (`fbf46db`), so its window works on Meitou 0.21 and newer (list rows and text fit through the running Meitou's own constructor and overload); loads on Meitou 0.20.1 and newer, no upper cap. Not released.

Author: PZA. Licence: GPLv3 or later. A MeitouClient 0.20.1 test mod (`pza.uiprobe`, v0.3.3), not released on the Workshop. **Since 0.2.0 it is built on PZA's Meitou UI** (`..\PZAMeitouUi`, compiled in: clone both repos side by side): one in-game test proves the library. It proves that a code mod
can run a real Kenshi (MyGUI) window on Meitou's own window engine (`MyGuiScreenHost`), the route the PZA mods' Meitou windows will take.

## Open it

F10 (Meitou's debug menu) > **PZA's UI Probe: open / close**, or the tiny **UI PROBE** button (top right: click = open / close, + = open, drag it, mouse wheel over it = opacity). No hotkey: every free Shift+letter in Meitou is a Direct Control key or kept for LEAF.

0.1.0 was Confirmed in game 2026-11-16 (every widget, the drag, the tooltip, Escape, F7, no pause). 0.2.0 adds: the place remembered across restarts (Settings > Mods > PZAUiProbe shows it), the tiny button. 0.3.0 adds the Faces tab (the library's character faces, 0.2.0).

## The window

A Kenshi window (`Kenshi_WindowCX`, caption "PZA'S UI PROBE") with three tabs:
- **Widgets:** wrapped intro text, a tick box, a slider (`Kenshi_SliderHBig`, 0.10 - 0.60 in 0.05 steps, the SlowMo scale), a text box, a list of six lines.
- **Faces:** the faces of the first four selected characters, drawn live by Meitou's portrait renderer.
- **Help:** a long wrapped paragraph.
- Under the tabs: a status line, **PRESS** (with a native Kenshi tooltip) and **CLOSE** (Enter presses it).

## What to try in game (each first event is logged as `FIRST ...`; every event as `event: ...`, the first 60)

1. Drag it by its title; it stays where you leave it, also after Close and reopen.
2. Click PRESS (the status line counts), hover it for the tooltip.
3. Tick box, slider (arrows and dragging the thumb), type in the box (hotkeys must not fire while it has the keyboard), pick a list line,
   double-click one (`accept`).
4. The Faces tab: select one to four characters (a recruit, a squad member, an NPC). Each must show their OWN face and gear, not a generic
   one, and follow a change of selection. The Help tab and back.
5. Escape closes it; CLOSE closes it; F7 (does it hide with the HUD?); does the game pause while it is open (it should not)?
6. 1080p / 1440p / 4K; Faithful and Meitou UI mode.

## Offline check (`mod\build.ps1 -Check`, never starts the game)

Meitou's own code-mod checks; both patches applied to the real MeitouClient.dll; then the window on a **real `MyGuiScreenHost` with Kenshi's own
MyGUI resources from the install**: registered, opened, bound (after the game's default binding that disables unknown buttons), on screen at
720p / 1080p / 4K, every widget laid out, PRESS and the tick box raising events, a list line selecting, the slider's arrow and thumb drag,
typing in the text box (it takes the keyboard), the Help tab, a title-bar drag moving it and the offset kept across Close and reopen, Escape
closing it; the Faces tab binding atlas cells, and the face requests (added once a frame, moved off a taken cell, dropped when no longer
shown). Two findings made by this check: tooltips come through the binding's `Properties` (layout `UserString`s do
not reach the control), and binding `TabCaptions` puts a tab control in one-page mode.

## Build

Clone this repo and PZA's Meitou UI side by side (`PZAMeitouUi` next to `PZAMeitouUiProbe`). Tell the build where an unpacked Meitou release
is, either with `.\build.ps1 -MeitouDir <dir>` or once in `mod\local.props` (git-ignored):

```xml
<Project><PropertyGroup><MeitouDir>D:\Meitou-0.20.1-win-x64</MeitouDir></PropertyGroup></Project>
```

## Install (Meitou closed)

1. `cd mod`, `.\build.ps1` -> copy `mod\out\PZAUiProbe\` to `Kenshi\mods\PZAUiProbe\`.
2. Add `PZAUiProbe.mod` to Meitou's `mods.cfg` and start Meitou.
3. The log shows `PZA's UI Probe: 0.3.0`, the `patched` line (with `GameHost.GatherPortraitKeys (faces)`), then `the player's window host is ready ...` and `FIRST register ...`.
