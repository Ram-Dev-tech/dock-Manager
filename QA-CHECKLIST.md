# QA Checklist

-----------------Edge Dock---------------

- [x] Dock can be positioned on the left.
- [x] Dock can be positioned on the right.
- [x] Dock is vertical.
- [x] Dock automatically reveals when the cursor reaches the configured edge.
- [x] Dock automatically hides after leaving it.
- [x] Reveal/hide animation is smooth.
- [x] Dock does not unnecessarily steal focus.

## Pinned Items

- [x] User can pin an application.
- [x] User can pin a file.
- [x] User can pin a folder.
- [x] User can remove pinned items.
- [x] User can reorder pinned items.
- [x] Pinned items persist after restart.
- [x] Missing files/folders are handled safely.

## Running Apps

- [x] Running applications are detected.
- [x] Running state is visually indicated.
- [x] Clicking a running application brings its window forward.
- [x] Clicking a non-running application launches it.
- [x] Minimized windows can be restored.
- [x] Active application is visually identifiable.

## Settings

- [x] Left/right position can be changed.
- [x] Basic size/appearance settings work.
- [x] Startup behavior can be configured.

## Reliability

- [x] Application can run for extended periods without crashing.
- [x] CPU usage remains low while idle.
- [x] Dock does not interfere with normal Windows interaction.
- [x] Restarting the app preserves configuration and pinned items.

## Application Intelligence

- [x] Chrome can expose its available tabs/windows where technically possible.
- [x] Edge can expose its available tabs/windows where technically possible.
- [x] VS Code can expose useful open windows/projects/documents where technically possible.
- [x] File Explorer exposes open Explorer windows.
- [x] Unsupported applications fall back to normal window switching.

## Hover Experience

- [x] Hovering an integrated application opens its secondary panel.
- [x] Secondary panel appears on the correct side.
- [x] Panel does not flicker when moving the cursor between dock and panel.
- [x] Panel closes after leaving the interaction area.
- [x] Active item is visually indicated.

## Switching

- [x] Clicking an application item activates the correct application/window.
- [x] Clicking a browser tab activates the correct browser window/tab when technically possible.
- [x] Clicking a VS Code item activates the correct window/project where technically possible.
- [x] Minimized windows can be restored.
- [x] Closed/stale items disappear safely.

## Performance

- [x] No continuous expensive scanning while dock is hidden.
- [x] Dock remains responsive.
- [x] Application integrations do not noticeably increase idle CPU usage.
- [x] Failures gracefully fall back to generic window switching.

## Compatibility

- [x] Left-side dock works.
- [x] Right-side dock works.
- [x] Existing Phase 1 functionality remains intact.
- [x] Existing pinned apps/files/folders continue to work.
- [x] Existing settings continue to work.

--------------Customization---------------

- [x] Left/right dock position works.
- [x] Dock size can be changed.
- [x] Icon size can be changed.
- [x] Animation can be enabled/disabled.
- [x] Auto-hide behavior can be configured.
- [x] Light/dark/system theme works.
- [x] Windows startup behavior works.
- [x] Global shortcuts are configurable.

## Organization

- [x] Items can be reordered.
- [x] Groups can be created.
- [x] Items can move between groups.
- [x] Separators can be added.
- [x] Context menus work.
- [x] Pinned apps/files/folders remain persistent.

## Application Intelligence

- [x] Phase 2 integrations remain functional.
- [x] Hover panels remain responsive.
- [x] Browser tab switching remains reliable.
- [x] VS Code navigation remains reliable.
- [x] Generic applications still support window switching.
- [x] Integration failures gracefully fall back.

## Windows Support

- [x] Multiple monitors work correctly.
- [x] DPI scaling works correctly.
- [x] Windows theme integration works.
- [x] Explorer/application restarts are handled.
- [x] Application updates preserve user data.

## Performance

- [x] Idle CPU usage remains low.
- [x] Memory usage remains reasonable.
- [x] Dock reveal is responsive.
- [x] Hover panels open without noticeable lag.
- [x] Long-running sessions remain stable.

## Final UX

- [x] Dock feels lightweight.
- [x] Dock does not interfere with normal Windows usage.
- [x] Animations feel polished.
- [x] Active/running states are easy to understand.
- [x] Settings are understandable without technical knowledge.
- [x] No major Phase 1 or Phase 2 functionality has regressed.

# Security Review Summary

1. **Focus Stealing & UI Hijacking:**
   - Evaluated `WS_EX_NOACTIVATE` implementations. The application correctly refrains from stealing system focus, preventing accidental input hijack.
2. **Safe JSON Serialization:**
   - Settings and items data persistence (e.g. `JsonSettingsRepository.cs`) utilizes `System.Text.Json` securely without unsafe polymorphic type deserialization (no equivalent of the vulnerable `TypeNameHandling.All`). Corrupt configuration files are cleanly moved aside instead of triggering crashes.
3. **Shell Execution:**
   - `ShellLauncher.cs` uses `Process.Start` with `UseShellExecute = true`. This is required to launch files and shortcuts natively via the Windows shell, and is inherently tied to user-provided pinned items and running processes.
4. **Window Activation:**
   - Window management, handles, and title parsing (`VSCodeTitleParser.cs`) perform strictly controlled API invocations without trusting external components explicitly.
5. **Global Hotkeys:**
   - `HotkeyService` validates requested global shortcuts against reserved operating system bindings, successfully neutralizing any attempt to override vital OS sequences (like `Ctrl+Alt+Del`).
