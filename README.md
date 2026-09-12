# Dock Manager

A small, polished, **vertical edge dock** for Windows. It stays hidden until you move the cursor to
the configured screen edge, then slides out so you can launch pinned apps/files/folders and switch to
running applications — without a permanent panel in the way.

## What it does

**The dock (Phase 1)**

1. **Edge dock** — vertical, attached to the left or right edge, auto reveal / hide.
2. **Pinned items** — applications, files and folders, with pin / unpin / reorder and persistence.
3. **Running apps & switching** — detect running apps, show a subtle indicator, click to bring a
   window forward (or launch it when it is not running).

**Application awareness (Phase 2)**

4. **Hover panel** — hovering a running application shows what it has open: Chrome/Edge **tabs**
   (UI Automation), VS Code **projects and documents** (window title parsing), Explorer **folders**
   (shell COM), plain windows for everything else. Click an entry to switch straight to it.
   Unsupported apps keep working through the generic window fallback, and the panel can be switched
   off entirely in settings.

**Customization, organization and polish (Phase 3)**

5. **Settings** organized into *Dock / Appearance / Items / Applications / Shortcuts / General*:
   edge with a visual preview, edge sensitivity in plain wording, dock and icon size, theme
   (System / Light / Dark), calm animations with a reduced-motion-respecting switch, and start with
   Windows.
6. **Organization** — optional **groups** (header + the items below it) and purely visual
   **separators**; everything reorders by drag & drop or menu, and pinned items stay independent of
   what is currently running.
7. **Quick actions & context menu** — type aware: Open / Open file location, integration provided
   actions where they are reliable (Chrome & Edge *New tab / New window*, Explorer *New window /
   Open Downloads*, VS Code *New window*), *Move to group*, *Add separator*, *Remove from dock*
   (never uninstalls or deletes anything).
8. **Global shortcuts** — configurable, not hard-coded (defaults: `Ctrl+Shift+Space` open dock /
   open the selected item, `Ctrl+Alt+→` / `Ctrl+Alt+←` step through items with a visible selection
   ring). Windows shortcuts are never taken: reserved combinations are refused and conflicts are
   reported in settings.
9. **Many items** — a lightweight filter box appears on the dock once it holds enough items, and
   overflow scrolls inside the work area instead of growing off screen.
10. **Windows integration** — multi-monitor with a preferred-display setting and hot-plug fallback,
    per-monitor DPI, tray icon, clean shutdown, no notification spam.

## Requirements

- Windows 10 / 11
- .NET SDK 8.0 to build (the published output is self-contained and needs no runtime installed)

## Build & run

```powershell
# build Release + run unit tests
.\build.ps1

# build Release + emit a single-file self-contained executable
.\build.ps1 -Publish
```

Then run `publish\DockManager.exe`. The app lives in the tray; the dock appears when the cursor
touches the configured edge.

`dotnet run --project src/DockManager.App` also works for development.

## Layout

```text
App
├── src/DockManager.Core        platform independent: layout math, visibility state machine,
│                               items (incl. groups/separators), settings (incl. shortcuts/theme),
│                               persistence, integration contracts, hover panel state machine
│                               (unit tested)
├── src/DockManager.App         WPF + Win32: the dock window, hover panel, app integrations
│                               (Chrome/Edge/VS Code/Explorer), hotkeys, theme, tray, settings UI
└── tests/DockManager.Core.Tests  xUnit tests for all of the core logic
```

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the module map, and the acceptance checklists:
[Phase 1](docs/PHASE-1-ACCEPTANCE.md), [Phase 2](docs/PHASE-2-ACCEPTANCE.md),
[Phase 3](docs/PHASE-3-ACCEPTANCE.md).

## Using the dock

- **Reveal** – move the cursor to the configured edge (default: left), or press your *Open dock*
  shortcut.
- **Launch / switch** – click an item. If the app is already running its window is brought forward
  (restored if minimized); otherwise the app is launched.
- **Dive into an app** – hover a running app to see its tabs / documents / folders and click one to
  switch straight to it.
- **Keyboard** – `Ctrl+Alt+→` / `←` step through items, `Ctrl+Shift+Space` opens the dock and
  activates the highlighted item (all rebindable in settings).
- **Pin** – drag a file / folder / `.exe` / `.lnk` onto the dock, or use the `+` button / right-click.
- **Unpin** – right-click an item → *Remove from dock* (never uninstalls or deletes anything).
- **Organize** – right-click the dock background → *New group…* / *Add separator*; right-click an
  item → *Move to group*.
- **Reorder** – drag an item within its section, or right-click → *Move up / down*.
- **Settings** – gear button or tray menu.

## Notes

- The dock never steals keyboard focus (`WS_EX_NOACTIVATE`) and never appears in the taskbar.
- Missing targets are shown dimmed with a badge and can still be removed; the dock never crashes.
- Integrations only run while you hover an app — nothing polls or scans while the dock is hidden.
- Everything (pinned items, groups, separators, settings, shortcuts) is stored as JSON under
  `%APPDATA%\DockManager`; a corrupt file is backed up and replaced with safe defaults.
