# Architecture

The application is split into two assemblies so that every decision that does not need Windows can be
unit tested anywhere, and the Windows-specific shell stays small.

```text
DockManager.Core  (net8.0, no Windows deps)
├── Dock
│   ├── DockEdge / DockRect            edge enum + a Rect that is not System.Windows.Rect
│   ├── DockLayoutMetrics              spacing derived from one icon size
│   ├── DockMetrics                    panel geometry, hit testing, insertion index, activation zone
│   ├── DockPlan                       resolved layout (slot + separator rectangles)
│   ├── DockVisibilityController       reveal/hide state machine, driven by a clock
│   └── DockAnimator                   eased 0..1 interpolation for the slide
├── Items
│   ├── PinnedItem / AppItem / FileItem / FolderItem
│   ├── ItemStore                      ordered sections, dedupe, reorder, remove-missing
│   └── PinnedItemFactory              path -> correct item kind
├── Settings
│   ├── DockSettings / DockSizeScale   the small Phase 1 settings surface + clamping
│   ├── SettingsStore                  validate -> save -> broadcast
│   └── IStartupRegistration
├── Persistence
│   ├── ItemStore / Settings repositories (JSON, atomic writes, corrupt recovery)
├── Shell
│   ├── IFileSystemProbe / IShortcutResolver / IShellLauncher / IIconProvider
│   └── PathNormalizer                 case/separator-insensitive path keys
├── Windows
│   ├── WindowInfo / RunningApp / RunningAppIndex   window grouping + matching (no Win32 types)
│   └── IWindowManager
└── Ui
    └── DockItemViewModel / DockViewModel   bindable, observable, testable presentation state

DockManager.App  (net8.0-windows, WPF + Win32)
├── Dock
│   ├── Win32                          the small set of P/Invokes
│   ├── MonitorInfoSource / DockPosition  per-monitor physical geometry, DPI-safe
│   ├── EdgeDetector (in DockWindow)   cursor poll -> visibility controller
│   └── DockWindow                     reveal/hide, animation, menus, drag & drop, activation
├── Windows
│   ├── WindowEnumerator               EnumWindows filter (visible, unowned, titled, not cloaked)
│   ├── ProcessResolver                executable path via QueryFullProcessImageName
│   ├── WindowActivator                restore + SetForegroundWindow with thread attach
│   ├── WindowManager                  IWindowManager implementation
│   └── ForegroundWatcher              SetWinEventHook for the active indicator
├── Shell
│   ├── ShellLauncher / ShellLinkResolver / IconProvider
├── Settings
│   ├── SettingsWindow                 minimal settings UI
│   └── StartupRegistration            HKCU Run key via advapi32
├── Ui
│   ├── TrayIcon                       Shell_NotifyIcon + message window (no WinForms)
│   ├── IconConverter / DockLook
└── Composition
    └── DockServices                   the service bag / composition root
```

## Design decisions

- **Deterministic geometry.** `DockMetrics` (DIPs) is the single source of truth; the WPF template is
  given the same numbers through `DynamicResource`, so the rendered panel exactly matches the math that
  the tests exercise. Positioning is done with `SetWindowPos` in *physical* pixels to stay correct on
  multi-monitor, mixed-DPI setups.
- **The visibility controller and animator are pure.** They take a clock and produce progress; the WPF
  layer only renders. This keeps reveal/hide timing and easing unit-testable.
- **Window switching is O(1) per item.** The window list is enumerated once into a `RunningAppIndex`
  (grouped by executable) and the dock looks up each pinned item against it. Enumeration only happens
  while the dock is visible, so an idle dock costs nothing.
- **The active indicator is event driven** (`SetWinEventHook`), not polled.

## Phase 2 boundary

Deliberately out of scope here (the seams above are where they would plug in):

- Browser / editor tab extraction (a per-app plugin would extend `WindowEnumerator` + `RunningApp`).
- Per-window previews, workspace management, themes marketplace, cloud sync, accounts, AI features.
