# Phase 3 — Advanced settings, organization & polish: acceptance checklist

## Customization

- [x] Position Left / Right with a mini visual preview; default Left; never top/bottom.
- [x] Dock size Small / Medium / Large + icon size slider; compact spacing by default.
- [x] Edge activation as *Less sensitive / Normal / More sensitive* — no pixel values exposed.
- [x] Reveal animation on/off; also honours the Windows reduced-motion setting
      (`SystemParameters.ClientAreaAnimation`); animations are short and calm.
- [x] Auto-hide with hide-delay slider; the dock is never permanently visible by default.
- [x] Theme System / Light / Dark (default System, resolved from the Windows setting and refreshed
      on `WM_SETTINGCHANGE`); subtle transparency only.
- [x] Settings organized into **Dock / Appearance / Items / Applications / Shortcuts / General**;
      no internal technical concepts in the wording.

## Global shortcuts

- [x] Configurable, stored in settings (defaults: `Ctrl+Shift+Space` open dock / activate selected
      item, `Ctrl+Alt+→` next, `Ctrl+Alt+←` previous) — nothing hard-coded.
- [x] Capture UI: click a row, press keys; Esc cancels; Clear removes the binding.
- [x] Windows-reserved combinations (Win+*, Alt+Tab, Alt+Esc, Ctrl+Esc, Ctrl+Shift+Esc,
      Ctrl+Alt+Del, F1) are refused with an explanation — never silently overridden.
- [x] Combos another application already holds fail registration and surface as a warning in the
      Shortcuts section (`HotkeyService.Conflicts`).
- [x] Keyboard selection is visible (accent ring) and Enter-equivalent activation happens via the
      open-dock shortcut; hover is never the only way to use the dock.

## Organization & power users

- [x] Optional groups: *New group…* from the dock background menu or an item's *Move to group*;
      rename / remove (items stay on the dock); a group is a header over the existing ordered list,
      so drag & drop, persistence and reorder work unchanged.
- [x] Separators: purely visual, add/remove from context menus, persisted.
- [x] Drag & drop reorder with insertion indicator; dragging never launches; *Move up / down* as
      the accessible alternative.
- [x] Pinned items stay independent from running state: running indicator on the pinned item, no
      duplicates.
- [x] Quick actions only where reliable: Chrome/Edge *New tab / New window*, Explorer *New window /
      Open Downloads*, VS Code *New window* — supplied by integrations, empty otherwise.
- [x] Context menu is type dependent: Open / Open file location, quick actions, Move to group,
      Add separator, Remove from dock (never deletes files or uninstalls).

## Windows support

- [x] Multi-monitor: dock can be assigned to a specific display (settings combo lists device name,
      resolution and primary flag); unknown/unplugged display falls back to the primary;
      `WM_DISPLAYCHANGE` / `WM_DPICHANGED` re-evaluate geometry.
- [x] Physical-pixel positioning + per-monitor DPI (existing Phase 1 mechanism, unchanged).
- [x] Start with Windows (HKCU Run), tray icon, clean shutdown, no notifications.
- [x] Corrupt config: backed up and replaced with safe defaults (Phase 1 repositories, unchanged);
      all Phase 3 fields (theme, sensitivity, monitor, shortcuts, search) load from older files
      with defaults.

## Performance & final UX

- [x] Overflow: with more pins than fit, the item area scrolls (hidden bar, wheel) and is clamped
      to the work area — the dock never exceeds the screen; footer buttons stay reachable.
- [x] Search: filter box appears only when the dock holds at least the threshold number of items
      (default 8); covers pins, files, folders (running apps are the pins themselves); groups and
      separators hide while filtering.
- [x] Tooltips never cover the hover panel (panel is placed beside the dock; tooltips stay on the
      dock side).
- [x] Idle cost unchanged: cursor poll only; integrations, previews and refreshes run solely while
      the hover panel is open.
- [x] Phase 1/2 behaviour intact: reveal/hide, launch/switch, pin/unpin, persistence, hover panel.

## Tests (DockManager.Core.Tests)

`OrganizationTests` (groups, separators, move-to-group, persistence round trip),
`ShortcutTests` (defaults, formatting, reserved combos, sanitize), `SearchAndQuickActionTests`,
`Phase3` fields covered by `ShortcutTests.Phase2_files_without_phase3_fields_still_load` and the
existing settings suites.
