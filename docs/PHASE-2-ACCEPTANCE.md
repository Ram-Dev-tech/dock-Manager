# Phase 2 — Application-aware dock: acceptance checklist

Hover a running application → after the configured delay a secondary panel opens with what the app
has open. Click an entry → switch straight to it. Nothing else about the dock changes.

## Integrations

- [x] Chrome & Edge: open tabs listed (UI Automation), active tab subtly highlighted; clicking a
      tab selects it and brings the window forward (`ChromiumIntegration`).
- [x] VS Code: one entry per window, named after the project, document as the second line
      (`VSCodeTitleParser` — pure and unit tested).
- [x] Explorer: open folders resolved through the shell (`IShellWindows`), path as the subtitle.
- [x] Everything else: the generic window list (Phase 1 behaviour, `GenericIntegration`).
- [x] Any integration failure falls back to the window list — never an error, never a crash.
- [x] Integrations can be switched off per app in settings (`DisabledIntegrations`).

## Behaviour

- [x] Panel never steals focus (`WS_EX_NOACTIVATE`), docks beside the hovered item, stays fully
      inside the work area, never covers the dock itself.
- [x] Open/close delays (`TabPanelController`, clock driven, unit tested); moving from item to
      panel does not flicker; hiding the dock hides the panel.
- [x] Optional small window preview per hovered entry (one-shot `PrintWindow`, switchable).
- [x] No duplicate tabs: activating a tab selects it, it never opens a second one.
- [x] Nothing polls or scans while the dock is hidden; content refreshes only while the panel is
      open (~2 s), on the dedicated STA worker thread.

## Settings

- [x] "Show what an app has open when hovering it" (default on).
- [x] "Show a small window preview" (default on).
- [x] Hover delay slider (0–1000 ms, default 200 ms).
- [x] Per-app support switches (Chrome, Edge, VS Code, Explorer).

## Tests (DockManager.Core.Tests)

`GenericIntegrationTests`, `ApplicationManagerTests`, `VSCodeTitleParserTests`,
`TabPanelControllerTests`, `Phase2SettingsTests` (+ `Support/FakeWindowActivator`).
