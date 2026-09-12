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
│   ├── SeparatorItem / GroupHeaderItem   pure layout entries riding the same ordered list
│   ├── ItemStore                      ordered sections, dedupe, reorder, groups, remove-missing
│   └── PinnedItemFactory              path -> correct item kind
├── Integrations
│   ├── IApplicationIntegration        "what does this app have open, and how do I switch to it"
│   ├── AppContentItem / QuickAction   the entries the hover panel shows; reliable menu actions
│   ├── GenericIntegration             window-level fallback: Phase 1 behaviour, always available
│   ├── ApplicationManager             executable -> integration resolution, user can disable
│   └── VSCodeTitleParser              pure title parsing (fully unit tested)
├── Panel
│   └── TabPanelController             hover panel open/close delays as a clock-driven machine
├── Shortcuts
│   ├── ShortcutFormatter              "Ctrl + Shift + Space" formatting, capture rules
│   └── ShortcutValidator              reserved Windows combos are refused, never taken
├── Settings
│   ├── DockSettings / DockSizeScale / DockTheme / EdgeSensitivity / ShortcutRecord
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
├── Integrations
│   ├── IntegrationWorker              dedicated STA thread: UIA/COM never block the dock
│   ├── ChromiumIntegration            Chrome/Edge tabs via UI Automation
│   ├── VSCodeIntegration              projects/documents from window titles
│   ├── ExplorerIntegration            open folders via IShellWindows COM
│   └── WindowPreviewService           one-shot PrintWindow capture per hovered entry
├── Shell
│   ├── ShellLauncher / ShellLinkResolver / IconProvider
├── Settings
│   ├── SettingsWindow                 Dock / Appearance / Items / Applications / Shortcuts / General
│   └── StartupRegistration            HKCU Run key via advapi32
├── Ui
│   ├── TrayIcon                       Shell_NotifyIcon + message window (no WinForms)
│   ├── TabPanelWindow                 the non-activating hover panel
│   ├── ThemeService                   System/Light/Dark -> shared DynamicResource brushes
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
- **Integrations are opt-in per app and fail soft.** Every integration answers the same contract and
  must return its best fallback (the window list) instead of throwing; unknown or disabled apps get
  the generic integration, so Phase 1 behaviour is the floor, never the casualty. Heavy work (UIA,
  shell COM) runs on one STA thread and only while the hover panel is open — an idle dock never
  enumerates anything.
- **Groups are layout, not a new data structure.** A group is a header item in the same ordered
  list as everything else; reordering, drag & drop and persistence needed no special casing.
- **Shortcuts never hijack Windows.** Reserved combinations are rejected before registration,
  registration failures are reported, and bindings are stored in settings, not in code.

## Out of scope

Deliberately not built (the seams above are where they would plug in):

- A plugin marketplace, cloud sync, accounts, AI features.
- A full window manager or a Windows Search replacement: the dock filter only narrows its own items.
