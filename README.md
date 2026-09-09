# Dock Manager

A small, polished, **vertical edge dock** for Windows. It stays hidden until you move the cursor to
the configured screen edge, then slides out so you can launch pinned apps/files/folders and switch to
running applications — without a permanent panel in the way.

Phase 1 implements exactly three capabilities, deliberately kept minimal:

1. **Edge dock** — vertical, attached to the left or right edge, auto reveal / hide.
2. **Pinned items** — applications, files and folders, with pin / unpin / reorder and persistence.
3. **Running apps & switching** — detect running apps, show a subtle indicator, click to bring a
   window forward (or launch it when it is not running).

It is intentionally *not* a copy of the macOS Dock or the Arc sidebar, and it does **not** implement
Phase 2 features (browser tab extraction, per-app plugins, cloud sync, themes, etc.).

---

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
│                               items, settings, persistence, window-state tracking  (unit tested)
├── src/DockManager.App         WPF + Win32: the dock window, edge detection, window switching,
│                               icons, tray, settings UI, startup registration
└── tests/DockManager.Core.Tests  xUnit tests for all of the core logic
```

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the module map and the Phase 2 boundary, and
[docs/PHASE-1-ACCEPTANCE.md](docs/PHASE-1-ACCEPTANCE.md) for the acceptance checklist.

## Using the dock

- **Reveal** – move the cursor to the configured edge (default: left).
- **Launch / switch** – click an item. If the app is already running its window is brought forward
  (restored if minimized); otherwise the app is launched.
- **Pin** – drag a file / folder / `.exe` / `.lnk` onto the dock, or use the `+` button / right-click.
- **Unpin** – right-click an item → *Remove from dock* (never uninstalls or deletes anything).
- **Reorder** – drag an item within its section, or right-click → *Move up / down*.
- **Settings** – gear button or tray menu → position, size, auto-hide and startup behaviour.

## Notes

- The dock never steals keyboard focus (`WS_EX_NOACTIVATE`) and never appears in the taskbar.
- Missing targets are shown dimmed with a badge and can still be removed; the dock never crashes.
- Everything (pinned items + settings) is stored as JSON under `%APPDATA%\DockManager`.
