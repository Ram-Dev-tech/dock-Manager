# Windows Vertical Dock

[![GitHub Release](https://img.shields.io/github/v/release/Ram-Dev-tech/dock-Manager?label=version&color=brightgreen)](https://github.com/Ram-Dev-tech/dock-Manager/releases/latest)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)](https://github.com/Ram-Dev-tech/dock-Manager)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

> **A lightweight, fully offline Windows vertical edge dock for launching apps, opening files and folders, and quickly switching between running applications and supported application tabs.**

```text
Platform: Windows 10/11
Mode: Offline-first
Status: Phase 3 Complete
Version: 0.1.1
Repository: github.com/Ram-Dev-tech/dock-Manager
```

## Download

### Windows Executable (Recommended)

**Latest release: v0.1.1**

Download and run the Windows executable directly:

**[DockManager-0.1.1.exe](https://github.com/Ram-Dev-tech/dock-Manager/releases/download/v0.1.1/DockManager-0.1.1.exe)**

*No installation required. Just download and run. Works offline.*

---

### Alternative: Portable EXE (Generic Name)

**[DockManager.exe (Portable)](https://github.com/Ram-Dev-tech/dock-Manager/releases/download/v0.1.1/DockManager.exe)**

*Same executable with a generic filename for easier scripting.*

---

### Installation via Terminal (PowerShell)

You can also download and run directly from PowerShell:

```powershell
# Download the executable
Invoke-WebRequest -Uri "https://github.com/Ram-Dev-tech/dock-Manager/releases/download/v0.1.1/DockManager-0.1.1.exe" -OutFile "$env:USERPROFILE\Desktop\DockManager.exe"

# Launch the application
Start-Process "$env:USERPROFILE\Desktop\DockManager.exe"
```

Or use the generic filename:

```powershell
# Download with generic name
Invoke-WebRequest -Uri "https://github.com/Ram-Dev-tech/dock-Manager/releases/download/v0.1.1/DockManager.exe" -OutFile "$env:USERPROFILE\Desktop\DockManager.exe"

# Launch the application
Start-Process "$env:USERPROFILE\Desktop\DockManager.exe"
```

---

## What It Is

Windows Vertical Dock is a productivity utility that attaches to the left or right edge of your Windows desktop. Inspired by the simplicity of the macOS Dock and the organization concepts of Arc's sidebar, it provides a lightweight vertical dock that stays hidden until you need it.

### What It Is NOT

This application does **NOT** replace:
- Windows Taskbar
- Windows Start Menu
- Windows Search
- File Explorer
- Your browser
- Task Manager

Instead, it provides a lightweight productivity layer attached to the edge of your Windows desktop.

---

## Core Experience

The central interaction is simple:

```
Move to edge → Dock appears → Hover → See → Click → Switch/Open
```

When you move your cursor to the configured screen edge, the vertical dock reveals itself. You can then:

- Launch pinned applications
- Switch to running applications
- Open pinned files
- Open pinned folders
- Hover applications to see their available windows/tabs/items (when supported)
- Switch directly to the desired window or tab

### Visual Concept

**Left-side dock:**
```text
┌──────┐   ┌─────────────────┐
│  🌐  │ → │ Chrome          │
│  💻  │   │ YouTube         │
│  📁  │   │ Gmail           │
│  🎵  │   │ ChatGPT         │
└──────┘   └─────────────────┘
   Dock      Secondary Panel
```

**Right-side dock:**
```text
┌─────────────────┐   ┌──────┐
│ Chrome          │ ← │  🌐  │
│ YouTube         │   │  💻  │
│ Gmail           │   │  📁  │
│ ChatGPT         │   │  🎵  │
└─────────────────┘   └──────┘
Secondary Panel        Dock
```

---

## Features

| Feature                       | Status |
| ----------------------------- | ------ |
| Vertical left/right dock      | ✅      |
| Edge reveal/hide              | ✅      |
| Pinned applications           | ✅      |
| Pinned files                  | ✅      |
| Pinned folders                | ✅      |
| Running application detection | ✅      |
| Window switching              | ✅      |
| Chrome tab integration        | ✅      |
| Edge tab integration          | ✅      |
| VS Code project integration   | ✅      |
| File Explorer integration     | ✅      |
| Generic app fallback          | ✅      |
| Groups & separators           | ✅      |
| Drag-and-drop reordering      | ✅      |
| Context menus                 | ✅      |
| Global shortcuts              | ✅      |
| Theme support (Light/Dark)    | ✅      |
| Multi-monitor support         | ✅      |
| DPI scaling                   | ✅      |
| Startup with Windows          | ✅      |
| Tray icon                     | ✅      |
| Fully offline operation       | ✅      |

---

## Offline-First Principle

**This is a NON-NEGOTIABLE requirement.**

The application is **fully functional offline**. Core functionality does **NOT** require:

- Internet access
- Cloud APIs
- Remote databases
- User accounts
- Authentication servers
- Cloud synchronization
- Online AI services
- Remote configuration
- Analytics services

The application continues working normally when the computer has **no internet connection**.

### Offline Functionality Includes:

- Dock reveal/hide
- Application launching
- Window switching
- Pinned applications, files, and folders
- Application integrations (Chrome, Edge, VS Code, File Explorer)
- Browser tab/window navigation
- VS Code navigation
- Settings
- Keyboard shortcuts
- Groups and separators
- Local configuration
- Multi-monitor behavior

All application integrations communicate with applications **locally on the Windows machine**:

```text
Chrome/Edge/VS Code/Explorer
         │
         │ Local integration (UI Automation, COM, window titles)
         ▼
Windows Vertical Dock
```

---

## Installation

### For Normal Users (Windows)

**Option 1: Download the Executable (Recommended)**

1. Download `DockManager-0.1.1.exe` from the [Releases page](https://github.com/Ram-Dev-tech/dock-Manager/releases/tag/v0.1.1)
2. Place it anywhere on your system (e.g., Desktop or Programs folder)
3. Double-click to run
4. The dock will appear in the system tray
5. Move your cursor to the left or right edge of your screen to reveal the dock

**Option 2: Download Portable EXE with Generic Name**

1. Download `DockManager.exe` from the [Releases page](https://github.com/Ram-Dev-tech/dock-Manager/releases/tag/v0.1.1)
2. Place it anywhere on your system
3. Double-click to run

**Option 3: Install via PowerShell**

Open PowerShell and run:

```powershell
# Download the executable
Invoke-WebRequest -Uri "https://github.com/Ram-Dev-tech/dock-Manager/releases/download/v0.1.1/DockManager-0.1.1.exe" -OutFile "$env:USERPROFILE\Desktop\DockManager.exe"

# Launch the application
Start-Process "$env:USERPROFILE\Desktop\DockManager.exe"
```

Or use the generic filename:

```powershell
# Download with generic name
Invoke-WebRequest -Uri "https://github.com/Ram-Dev-tech/dock-Manager/releases/download/v0.1.1/DockManager.exe" -OutFile "$env:USERPROFILE\Desktop\DockManager.exe"

# Launch the application
Start-Process "$env:USERPROFILE\Desktop\DockManager.exe"
```

**Requirements:**
- Windows 10 version 1903 or later / Windows 11
- .NET 8.0 Runtime (included with self-contained build, no separate installation needed)

The application works completely offline after download.

---

## Development

### Prerequisites

- Windows 10/11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (recommended) or VS Code with C# extension

### Clone and Build

```bash
# Clone the repository
git clone https://github.com/Ram-Dev-tech/dock-Manager.git
cd dock-Manager

# Restore dependencies
dotnet restore DockManager.sln

# Build in Release mode
dotnet build DockManager.sln --configuration Release

# Run tests (optional)
dotnet test tests/DockManager.Core.Tests/DockManager.Core.Tests.csproj

# Run the application in development
dotnet run --project src/DockManager.App/DockManager.App.csproj
```

### Publish Self-Contained Executable

```bash
# Create a self-contained single-file executable
dotnet publish src/DockManager.App/DockManager.App.csproj ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    --output publish

# The executable will be at: publish/DockManager.exe
```

Or use the provided build script on Windows:

```powershell
.\build.ps1 -Publish
```

---

## Building a Windows Release

The production Windows build creates a self-contained `.exe` executable.

**Version:** 0.1.1

**Build process:**

1. Install .NET SDK 8.0
2. Run tests: `dotnet test`
3. Build the production application: `dotnet build --configuration Release`
4. Generate the single-file executable: `.\build.ps1 -Publish`
5. Verify the executable on Windows
6. Verify offline functionality

**Output:**

- `publish/DockManager.exe` - Self-contained portable executable
- For release: The executable is uploaded to GitHub Releases as both `DockManager-{version}.exe` and `DockManager.exe`

**Release Artifacts:**

When you tag a release with `v0.1.1`, the CI/CD pipeline will:
1. Build the self-contained executable
2. Create two versions: `DockManager-0.1.1.exe` and `DockManager.exe`
3. Generate SHA256 checksums
4. Attach all files to the GitHub Release

---

## Architecture

The application is split into two assemblies:

```text
DockManager.Core (net8.0, platform-independent, unit tested)
├── Dock
│   ├── DockEdge / DockRect            Edge enum + geometry
│   ├── DockLayoutMetrics              Spacing derived from icon size
│   ├── DockMetrics                    Panel geometry, hit testing
│   ├── DockVisibilityController       Reveal/hide state machine
│   └── DockAnimator                   Eased interpolation for slide
├── Items
│   ├── PinnedItem / AppItem / FileItem / FolderItem
│   ├── SeparatorItem / GroupHeaderItem
│   ├── ItemStore                      Ordered sections, groups, remove-missing
│   └── PinnedItemFactory              Path -> correct item kind
├── Integrations
│   ├── IApplicationIntegration        "What does this app have open?"
│   ├── AppContentItem / QuickAction   Entries shown in hover panel
│   ├── GenericIntegration             Window-level fallback
│   ├── ApplicationManager             Executable -> integration resolution
│   └── VSCodeTitleParser              Pure title parsing (unit tested)
├── Panel
│   └── TabPanelController             Hover panel open/close delays
├── Shortcuts
│   ├── ShortcutFormatter              "Ctrl + Shift + Space" formatting
│   └── ShortcutValidator              Reserved Windows combos refused
├── Settings
│   ├── DockSettings / DockSizeScale / DockTheme
│   ├── SettingsStore                  Validate -> save -> broadcast
│   └── IStartupRegistration
├── Persistence
│   └── JSON repositories (atomic writes, corrupt recovery)
├── Shell
│   ├── IFileSystemProbe / IShortcutResolver / IShellLauncher / IIconProvider
│   └── PathNormalizer                 Case/separator-insensitive path keys
├── Windows
│   ├── WindowInfo / RunningApp        Window grouping + matching
│   └── IWindowManager
└── Ui
    └── DockItemViewModel / DockViewModel  Presentation state

DockManager.App (net8.0-windows, WPF + Win32)
├── Dock
│   ├── Win32                          P/Invokes
│   ├── MonitorInfoSource / DockPosition  Per-monitor physical geometry
│   ├── EdgeDetector                   Cursor poll -> visibility controller
│   └── DockWindow                     Reveal/hide, animation, drag & drop
├── Windows
│   ├── WindowEnumerator               EnumWindows filter
│   ├── ProcessResolver                Executable path resolution
│   ├── WindowActivator                Restore + SetForegroundWindow
│   ├── WindowManager                  IWindowManager implementation
│   └── ForegroundWatcher              SetWinEventHook for active indicator
├── Integrations
│   ├── IntegrationWorker              Dedicated STA thread: UIA/COM
│   ├── ChromiumIntegration            Chrome/Edge tabs via UI Automation
│   ├── VSCodeIntegration              Projects/documents from window titles
│   ├── ExplorerIntegration            Open folders via IShellWindows COM
│   └── WindowPreviewService           One-shot PrintWindow capture
├── Shell
│   ├── ShellLauncher / ShellLinkResolver / IconProvider
├── Settings
│   ├── SettingsWindow                 Organized settings UI
│   └── StartupRegistration            HKCU Run key
├── Ui
│   ├── TrayIcon                       Shell_NotifyIcon
│   ├── TabPanelWindow                 Non-activating hover panel
│   ├── ThemeService                   System/Light/Dark brushes
│   └── IconConverter / DockLook
└── Composition
    └── DockServices                   Service bag / composition root
```

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for detailed design decisions.

---

## Application Integrations

### Chrome & Edge

**Detection:**
- Windows
- Tabs (via UI Automation)

**Activation:**
- Window
- Specific tab

**Quick Actions:**
- New tab
- New window

**Fallback:** Generic Chrome/Edge window activation

---

### VS Code

**Detection:**
- Windows
- Projects (from window title)
- Open documents (from window title)

**Activation:**
- Window

**Quick Actions:**
- New window

**Fallback:** Generic VS Code window activation

---

### File Explorer

**Detection:**
- Windows
- Open folders (via IShellWindows COM)

**Activation:**
- Window

**Quick Actions:**
- New window
- Open Downloads

**Fallback:** Generic Explorer window activation

---

### Unsupported Applications

**Detection:**
- Windows only

**Activation:**
- Window

**Fallback:** Built-in (generic integration always available)

---

## Configuration

### Storage Location

Configuration is stored in:

```
%APPDATA%\DockManager\
```

### What Is Stored

- Pinned applications, files, and folders
- Groups and separators
- Dock settings (position, size, theme)
- Global shortcuts
- Application integration preferences
- Multi-monitor configuration

### Persistence

- All settings persist across restarts
- Atomic file writes prevent corruption
- Corrupt configuration files are backed up and replaced with safe defaults
- User data is stored separately from application binaries

---

## Privacy

### Data Handling

- **All user data is stored locally** in `%APPDATA%\DockManager`
- **No cloud services** are used for core functionality
- **No account is required** for normal operation
- **No telemetry** is collected
- **No analytics services** are integrated
- **No internet access** is required for core functionality

The dock does not send any data to external servers. All processing happens locally on your Windows machine.

---

## Security

### Security Considerations

As a Windows desktop utility, the application:

- **Local data handling:** All configuration stored in user-controlled `%APPDATA%`
- **Application launching:** Uses standard Windows shell execution (`UseShellExecute=true`)
- **File/folder access:** Only accesses paths explicitly pinned by the user
- **Windows permissions:** Runs under the user's security context; no elevation required
- **External process interaction:** Uses documented Windows APIs (UI Automation, COM, Win32)
- **Integration boundaries:** Each integration is modular and fails gracefully

### Input Validation

- All configuration values are validated and clamped to safe ranges
- Shortcut combinations reserved by Windows are refused
- File paths are normalized and validated before use
- JSON deserialization uses safe patterns

### Safe Operations

- Atomic file writes prevent configuration corruption
- Missing files/folders are handled safely (shown dimmed, can be removed)
- Exception handling throughout prevents crashes from propagating
- No network exposure (completely local application)

---

## Performance

### Design Goals

The application prioritizes:

- **Low idle CPU usage:** Cursor polling only when hidden; no continuous scanning
- **Reasonable memory usage:** Lightweight WPF interface
- **Fast dock reveal:** Smooth animations without lag
- **Responsive hover interaction:** Dedicated STA thread for integrations
- **No unnecessary background polling:** Window enumeration only when dock is visible
- **No unnecessary network activity:** Fully offline operation

### Performance Characteristics

- **Idle state:** Minimal CPU usage (cursor polling only)
- **Active state:** Window enumeration happens once while dock is visible
- **Hover panels:** Integration work runs on dedicated thread (~2 second refresh)
- **Animations:** Short, calm transitions respecting Windows reduced-motion setting

---

## Using the Dock

### Basic Interaction

- **Reveal:** Move cursor to configured edge (default: left), or press `Ctrl+Shift+Space`
- **Launch/Switch:** Click an item. Running apps are brought forward; non-running apps launch
- **Dive deeper:** Hover a running app to see its tabs/documents/folders
- **Keyboard navigation:** `Ctrl+Alt+→` / `Ctrl+Alt+←` step through items

### Pinning Items

- **Pin:** Drag a file/folder/.exe/.lnk onto the dock, or use the `+` button
- **Unpin:** Right-click an item → *Remove from dock* (never uninstalls or deletes)

### Organization

- **Groups:** Right-click dock background → *New group…*
- **Separators:** Right-click → *Add separator*
- **Reorder:** Drag items within their section, or right-click → *Move up/down*
- **Move to group:** Right-click item → *Move to group*

### Settings

Access via gear button or tray menu. Organized into:
- **Dock:** Edge position, monitor selection
- **Appearance:** Size, theme, animations
- **Items:** Pinned items management
- **Applications:** Integration preferences
- **Shortcuts:** Global keyboard shortcuts
- **General:** Startup behavior, advanced options

---

## Testing

### Automated Tests

The project includes xUnit tests for core logic:

```bash
dotnet test tests/DockManager.Core.Tests/DockManager.Core.Tests.csproj
```

**Test coverage includes:**
- Dock visibility controller
- Dock animator
- Dock metrics and layout
- Item store (pins, groups, separators)
- Running application index
- Shortcut validation and formatting
- Tab panel controller
- VS Code title parsing
- Settings persistence
- JSON repository (including corrupt file recovery)

### Manual Windows Test Checklist

#### Dock
- [ ] Left edge positioning works
- [ ] Right edge positioning works
- [ ] Edge reveal works
- [ ] Auto-hide works
- [ ] Hover interaction works
- [ ] Click interaction works
- [ ] Keyboard interaction works

#### Applications
- [ ] Launch application works
- [ ] Switch to running application works
- [ ] Restore minimized application works
- [ ] Multiple windows handled correctly

#### Files
- [ ] Pin file works
- [ ] Open file works
- [ ] Missing file handling works

#### Folders
- [ ] Pin folder works
- [ ] Open folder works
- [ ] Missing folder handling works

#### Integrations
- [ ] Chrome tab detection works
- [ ] Edge tab detection works
- [ ] VS Code project detection works
- [ ] File Explorer folder detection works
- [ ] Unsupported application fallback works

#### System
- [ ] Application restart preserves settings
- [ ] Windows restart preserves settings
- [ ] Offline operation works
- [ ] Multiple monitors work
- [ ] DPI scaling works
- [ ] Windows theme integration works

---

## Troubleshooting

### Dock Does Not Appear

**Solution:**
- Check if the dock is positioned on a disconnected monitor (change in Settings → Dock)
- Try the global shortcut `Ctrl+Shift+Space`
- Check the system tray for the dock icon
- Restart the application

### Edge Activation Does Not Work

**Solution:**
- Adjust edge sensitivity in Settings → Dock
- Ensure no other application is intercepting the edge
- Try revealing via keyboard shortcut

### Application Cannot Be Activated

**Solution:**
- The application may have crashed; check if it's still running
- Try launching it directly from the dock
- Some applications may not respond to activation requests

### Pinned File/Folder No Longer Exists

**Solution:**
- Missing items are shown dimmed with a badge
- Right-click → *Remove from dock* to clean up
- The dock never crashes due to missing targets

### Integration Unavailable

**Solution:**
- Unsupported applications fall back to generic window switching
- Check Settings → Applications to ensure integration is enabled
- Some applications may not expose tabs/windows reliably

### Dock Behaves Incorrectly After Monitor Changes

**Solution:**
- Disconnect/reconnect the monitor
- Change the monitor setting in Settings → Dock
- Restart the application

### Settings Fail to Persist

**Solution:**
- Check `%APPDATA%\DockManager` for corrupt configuration files
- Corrupt files are automatically backed up and replaced
- Manually delete the configuration folder to reset to defaults

---

## Roadmap

### Phase 1 — Core Dock ✅ Complete

**Edge Dock:**
- ✅ Left/right position
- ✅ Edge activation
- ✅ Reveal/hide behavior
- ✅ Vertical layout
- ✅ Smooth animation

**Pinned Items:**
- ✅ Applications
- ✅ Files
- ✅ Folders
- ✅ Add/remove
- ✅ Reorder
- ✅ Persistence

**Running Apps & Switching:**
- ✅ Running application detection
- ✅ Active state indication
- ✅ Window activation
- ✅ Minimized window restoration
- ✅ Launch when not running

---

### Phase 2 — Intelligent Application Tabs ✅ Complete

**Application-aware tabs/windows:**
- ✅ Chrome: tabs via UI Automation
- ✅ Edge: tabs via UI Automation
- ✅ VS Code: projects and documents from window titles
- ✅ File Explorer: open folders via COM
- ✅ Generic fallback for unsupported applications

**Smart previews and switching:**
- ✅ Contextual hover panels
- ✅ Active item indication
- ✅ Direct switching
- ✅ Optional window previews

**Integration architecture:**
- ✅ Modular integration system
- ✅ Per-application enable/disable
- ✅ Graceful failure fallback

---

### Phase 3 — Advanced Settings & Final Polish ✅ Complete

**Advanced Settings & Customization:**
- ✅ Dock position with visual preview
- ✅ Dock and icon size
- ✅ Edge sensitivity (plain wording)
- ✅ Animation on/off (respects Windows reduced-motion)
- ✅ Auto-hide with delay
- ✅ Theme (System/Light/Dark)
- ✅ Startup with Windows
- ✅ Organized settings UI

**Organization & Power User Features:**
- ✅ Groups (header + items)
- ✅ Separators
- ✅ Drag-and-drop reordering
- ✅ Context menus
- ✅ Quick actions (where reliable)
- ✅ Filter box for many items

**Final Polish & Windows Integration:**
- ✅ Multi-monitor support
- ✅ Per-monitor DPI scaling
- ✅ Windows theme integration
- ✅ Tray icon
- ✅ Clean shutdown
- ✅ Corrupt config recovery
- ✅ Global shortcuts (configurable, Windows-reserved combos refused)

---

## Design Philosophy

### Minimal

The dock should not become another task manager. It focuses on launching and switching.

### Fast

The user should be able to reach an app/window with minimal interaction: move → see → click.

### Contextual

Hovering an application exposes useful information (tabs, documents, folders) without overwhelming the user.

### Local

Core functionality works completely offline. No cloud dependencies.

### Native

The product behaves naturally on Windows, respecting Windows conventions and settings.

### Unobtrusive

The dock disappears when not needed. It never steals focus or interferes with normal Windows usage.

---

## Frequently Asked Questions

### Is this a replacement for the Windows taskbar?

**No.** Windows Vertical Dock complements the taskbar. It provides quick access to pinned items and running applications but does not replace taskbar functionality like system notifications, clock, or full window management.

### Does it work offline?

**Yes.** The application is designed to work fully offline. No internet connection is required for any core functionality.

### Can I put the dock on the right side?

**Yes.** The dock can be positioned on either the left or right edge of your screen.

### Can I pin files and folders?

**Yes.** You can pin applications, files, and folders. Missing files/folders are shown dimmed and can be safely removed.

### Can it show Chrome tabs?

**Yes.** Chrome (and Edge) tabs are detected via UI Automation and displayed when you hover over a running Chrome/Edge window.

### Can it show VS Code projects/tabs?

**Yes.** VS Code projects and open documents are extracted from window titles and displayed when hovering.

### Does it require an account?

**No.** The application requires no account, login, or registration.

### Does it send my data to the cloud?

**No.** All data is stored locally in `%APPDATA%\DockManager`. No telemetry, analytics, or cloud synchronization occurs.

### Does it work with unsupported applications?

**Yes.** Unsupported applications fall back to generic window switching. The dock detects running windows and can activate them, even without special integration.

---

## Contributing

### Development Setup

1. Clone the repository
2. Install .NET SDK 8.0
3. Run `dotnet restore`
4. Run tests: `dotnet test`
5. Run development build: `dotnet run --project src/DockManager.App`

### Code Quality

- Follow existing code style
- Write unit tests for new core logic
- Ensure all tests pass before submitting PRs
- Document public APIs

### Testing Expectations

- Core logic must have unit test coverage
- Manual testing on Windows is required for UI changes
- Verify offline functionality is preserved

### Pull Request Expectations

- Describe the change clearly
- Reference any related issues
- Include test results
- Ensure no regressions in existing functionality

### Offline-First Requirement

**Internet connectivity must never be required for core application functionality.**

Before adding a dependency or feature, determine whether it introduces:
- Network requirements
- Telemetry
- Remote APIs
- Account requirements
- Cloud storage

Prefer local implementations whenever possible.

### Avoiding Regressions

- Do not break existing pinned items or settings
- Preserve backward compatibility where possible
- Test upgrade scenarios

---

## Project Structure

```text
dock-Manager/
├── .github/workflows/
│   └── ci.yml                 CI/CD pipeline
├── docs/
│   ├── ARCHITECTURE.md        Detailed architecture documentation
│   ├── PHASE-1-ACCEPTANCE.md  Phase 1 acceptance checklist
│   ├── PHASE-2-ACCEPTANCE.md  Phase 2 acceptance checklist
│   ├── PHASE-3-ACCEPTANCE.md  Phase 3 acceptance checklist
│   └── SECURITY_REVIEW.md     Security review documentation
├── src/
│   ├── DockManager.App/       WPF application (Windows-specific)
│   │   ├── Assets/            Icons and resources
│   │   ├── Composition/       Service composition
│   │   ├── Diagnostics/       Logging
│   │   ├── Dock/              Dock window and positioning
│   │   ├── Integrations/      Application integrations
│   │   ├── Settings/          Settings UI and startup registration
│   │   ├── Shell/             Shell operations
│   │   ├── Ui/                Converters, themes, tray
│   │   └── Windows/           Window management
│   └── DockManager.Core/      Platform-independent core (unit tested)
│       ├── Dock/              Dock logic and math
│       ├── Integrations/      Integration contracts
│       ├── Items/             Pinned items, groups, separators
│       ├── Panel/             Hover panel logic
│       ├── Persistence/       JSON storage
│       ├── Settings/          Settings model
│       ├── Shell/             Shell abstractions
│       ├── Shortcuts/         Shortcut handling
│       ├── Ui/                View models
│       └── Windows/           Window info model
├── tests/
│   └── DockManager.Core.Tests/    xUnit tests
├── build.ps1                  Build script
├── Directory.Build.props      Version and build properties
├── DockManager.sln            Solution file
├── LICENSE                    MIT License
└── README.md                  This file
```

---

## v0.1.1 Release Checklist

### Build
- [x] Version is 0.1.1
- [x] Production build succeeds
- [x] Self-contained executable is generated
- [x] Executable is an `.exe`
- [x] Application launches after download
- [x] Two versions created: `DockManager-0.1.1.exe` and `DockManager.exe`

### Core Functionality
- [x] Left dock works
- [x] Right dock works
- [x] Edge activation works
- [x] Dock hides correctly
- [x] Applications can be pinned
- [x] Files can be pinned
- [x] Folders can be pinned
- [x] Items persist after restart
- [x] Running applications are detected
- [x] Windows can be activated
- [x] Minimized applications can be restored

### Offline
- [x] Disable internet connection
- [x] Launch downloaded application
- [x] Dock still works
- [x] Pinned applications still work
- [x] Pinned files still work
- [x] Pinned folders still work
- [x] Window switching still works
- [x] Settings still work
- [x] No core feature requires a network connection

### Windows
- [x] Windows 10/11 compatibility verified
- [x] DPI scaling verified
- [x] Startup behavior verified
- [x] Multiple monitors verified

### Release
- [x] Git tag `v0.1.1`
- [x] GitHub Release created
- [x] `.exe` files attached (`DockManager-0.1.1.exe` and `DockManager.exe`)
- [x] Checksums generated
- [x] Release notes written
- [x] Download instructions verified

---

## v0.1.1 Release Notes

### Dock Manager v0.1.1

**First Public Release**

Dock Manager v0.1.1 is the first downloadable version of the project.

### Included

- Vertical dock on left or right edge
- Auto reveal/hide with smooth animation
- Pin applications, files, and folders
- Detect and switch to running applications
- Chrome and Edge tab integration
- VS Code project/document integration
- File Explorer folder integration
- Groups and separators for organization
- Drag-and-drop reordering
- Configurable global shortcuts
- Light/Dark/System theme support
- Multi-monitor support with DPI scaling
- Startup with Windows
- Tray icon
- Fully offline operation

### Download

Download the Windows executable:

**[DockManager-0.1.1.exe](https://github.com/Ram-Dev-tech/dock-Manager/releases/download/v0.1.1/DockManager-0.1.1.exe)**

Or use the generic filename:

**[DockManager.exe](https://github.com/Ram-Dev-tech/dock-Manager/releases/download/v0.1.1/DockManager.exe)**

### Installation

1. Download the `.exe` file
2. Run the executable
3. The dock will appear in the system tray
4. Move your cursor to the left or right edge of your screen to reveal the dock

### Offline

The application's core functionality works without an internet connection.

### Known Limitations

- Pre-1.0 release (some features may change)
- Single-instance only (second launch focuses existing instance)
- Portable mode only (no traditional installer)

---

## Versioning Policy

This project uses semantic-style versioning:

```text
0.1.1
│ │ │
│ │ └── Patch (bug fixes, minor improvements)
│ └──── Minor (new features, backward compatible)
└────── Major (breaking changes)
```

For the early development stage:
- `0.x.x` = pre-1.0 development
- `0.1.x` = first development release line
- `0.1.1` = first installable release
- `1.0.0` = stable production release

**Note:** v0.1.1 is **not** a fully mature 1.0 release. It represents the first public installable version with complete Phase 1-3 functionality.

---

## Update Safety

Future releases will preserve user data. When upgrading from `0.1.1` → `0.1.2`, the installer/update process will **not** remove:

- Pinned applications
- Pinned files
- Pinned folders
- Groups
- Separators
- Settings
- Keyboard shortcuts

Configuration is stored separately from application binaries in `%APPDATA%\DockManager`.

### Offline Update Principle

The application itself remains fully functional offline. Updates may require downloading a newer installer, but:

> **Internet access is never required to use an already-installed version.**

---

## License

This project is licensed under the [MIT License](LICENSE).

---

## Acknowledgments

Inspired by:
- macOS Dock (simplicity and auto-hide behavior)
- Arc Browser sidebar (organization and contextual panels)

Built with:
- .NET 8.0
- WPF (Windows Presentation Foundation)
- xUnit (testing)

---

## Contact

For issues, feature requests, or contributions, please use the GitHub repository.
