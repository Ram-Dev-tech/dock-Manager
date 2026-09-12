using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using DockManager.App.Composition;
using DockManager.App.Ui;
using DockManager.App.Windows;
using DockManager.Core.Diagnostics;
using DockManager.Core.Dock;
using DockManager.Core.Integrations;
using DockManager.Core.Items;
using DockManager.Core.Panel;
using DockManager.Core.Settings;
using DockManager.Core.Shell;
using DockManager.Core.Ui;
using DockManager.Core.Windows;
using Microsoft.Win32;

namespace DockManager.App.Dock;

/// <summary>
/// The edge dock window. It owns the cursor poll that drives reveal/hide, the slide animation, the
/// item context menus, drag and drop (both pinning files and reordering items) and the activation
/// of running applications.
/// </summary>
public sealed partial class DockWindow : Window
{
    private const string ItemIdDataFormat = "DockManager.PinnedItemId";

    private readonly DockServices _services;
    private readonly DockLook _look = new();
    private readonly DockVisibilityController _visibility;
    private readonly DockAnimator _animator = new();
    private readonly MonitorInfoSource _monitors = new();
    private readonly DispatcherTimer _cursorTimer;
    private readonly DispatcherTimer _runningTimer;
    private readonly ForegroundWatcher _foregroundWatcher;
    private readonly TabPanelController _panelController;
    private readonly DispatcherTimer _panelRefreshTimer;

    private TabPanelWindow? _tabPanel;
    private DockItemViewModel? _hoveredApp;
    private bool _overDockItem;
    private CancellationTokenSource? _contentLoadCts;
    private CancellationTokenSource? _previewCts;
    private HotkeyService? _hotkeys;
    private DockItemViewModel? _selected;

    private IntPtr _hwnd;
    private MonitorGeometry _monitor = MonitorGeometry.Unknown;
    private Win32.RECT _windowRect;
    private RunningAppIndex _running = RunningAppIndex.Empty;
    private bool _animationSubscribed;
    private bool _dragStarted;
    private string? _pressedItemId;
    private FrameworkElement? _pressedElement;
    private Point _pressOrigin;

    public DockWindow(DockServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));

        InitializeComponent();

        DataContext = services.ViewModel;
        IconConverter.Provider = services.Icons;

        _visibility = new DockVisibilityController(services.Settings.Current.CreateVisibilityOptions());
        _visibility.StateChanged += OnVisibilityStateChanged;

        _panelController = new TabPanelController(services.Settings.Current.HoverDelayMs, 250);
        _panelController.StateChanged += OnPanelStateChanged;

        _panelRefreshTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(2),
        };
        _panelRefreshTimer.Tick += (_, _) => RefreshPanelContent();

        _animator.Frame += progress => ApplyPlacement(progress);

        _cursorTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(services.Settings.Current.CursorPollIntervalMs),
        };
        _cursorTimer.Tick += OnCursorTick;

        _runningTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(1200),
        };
        _runningTimer.Tick += OnRunningTick;

        _foregroundWatcher = new ForegroundWatcher(services.Logger);
        _foregroundWatcher.ForegroundChanged += OnForegroundChanged;

        services.ViewModel.ContentChanged += OnContentChanged;
        services.Settings.Changed += OnSettingsChanged;

        ApplyLook(services.Settings.Current);
        Loaded += OnLoaded;
    }

    public event EventHandler? SettingsRequested;

    public event EventHandler? QuitRequested;

    /// <summary>Reveals the dock regardless of the cursor position.</summary>
    public void Reveal() => _visibility.Show(NowMs());

    private static long NowMs() => Environment.TickCount64;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        if (PresentationSource.FromVisual(this) is not HwndSource source)
        {
            return;
        }

        _hwnd = source.Handle;
        source.AddHook(WndProc);

        // Never show in alt+tab and never steal activation.
        var exStyle = Win32.GetWindowLongPtrSafe(_hwnd, Win32.GwlExStyle).ToInt64();
        exStyle &= ~Win32.WsExAppWindow;
        exStyle |= Win32.WsExToolWindow | Win32.WsExNoActivate;
        Win32.SetWindowLongPtrSafe(_hwnd, Win32.GwlExStyle, new IntPtr(exStyle));

        try
        {
            var preference = Win32.DwmwcpRound;
            Win32.DwmSetWindowAttribute(_hwnd, Win32.DwmwaWindowCornerPreference, ref preference, sizeof(int));
        }
        catch (EntryPointNotFoundException)
        {
            // Older Windows: the window already has rounded corners from the WPF border.
        }

        _monitor = _monitors.GetFor(_hwnd, _services.Settings.Current.MonitorName);

        _hotkeys = new HotkeyService(_hwnd);
        _hotkeys.Pressed += OnHotkeyPressed;
        _hotkeys.Apply(_services.Settings.Current.Shortcuts);

        ApplyPlacement(0d);
    }

    /// <summary>Shortcuts that Windows refused or another application already holds.</summary>
    public IReadOnlyList<ShortcutRecord> HotkeyConflicts => _hotkeys?.Conflicts ?? [];

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        var message = (uint)msg;
        if (message is Win32.WmDisplayChange or Win32.WmDpiChanged or Win32.WmSettingChange)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                RefreshMonitorGeometry();

                // The Windows theme may have changed; re-resolve System.
                ApplyLook(_services.Settings.Current);
            }));
        }

        if (_hotkeys is not null && _hotkeys.HandleMessage(message, wParam))
        {
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void RefreshMonitorGeometry()
    {
        if (_hwnd == IntPtr.Zero)
        {
            return;
        }

        _monitor = _monitors.GetFor(_hwnd, _services.Settings.Current.MonitorName);
        UpdateOverflowClamp();
        ApplyPlacement(_animator.Progress);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _monitor = _monitors.GetFor(_hwnd, _services.Settings.Current.MonitorName);
        UpdateSeparatorVisibility();
        UpdateSearchVisibility();
        UpdateOverflowClamp();
        ApplyPlacement(0d);

        _cursorTimer.Start();
        _foregroundWatcher.Start();
    }

    private void OnCursorTick(object? sender, EventArgs e)
    {
        if (_hwnd == IntPtr.Zero || !Win32.GetCursorPos(out var cursor))
        {
            return;
        }

        var overPanel = _tabPanel is not null
            && _panelController.State == TabPanelState.Open
            && _tabPanel.ContainsPoint(cursor);

        var inside = IsInsideDock(cursor) || overPanel;
        var atEdge = !inside && IsCursorAtEdge(cursor);
        _visibility.Update(inside, atEdge, NowMs());

        _panelController.Update(
            _services.Settings.Current.ShowAppItemsOnHover && _overDockItem && _hoveredApp is not null,
            overPanel,
            NowMs());
    }

    private void OnRunningTick(object? sender, EventArgs e)
    {
        if (_visibility.State is DockVisibilityState.Hidden)
        {
            return;
        }

        RefreshRunningApps();
    }

    private void OnForegroundChanged(object? sender, EventArgs e)
    {
        // The hook fires on an arbitrary thread; hop to the UI thread.
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (_visibility.State is not DockVisibilityState.Hidden)
            {
                _services.ViewModel.RefreshRunning(_running, _services.Windows.GetForegroundWindowHandle());
            }
        }));
    }

    private void OnVisibilityStateChanged(object? sender, DockVisibilityChangedEventArgs e)
    {
        switch (e.Current)
        {
            case DockVisibilityState.Revealing:
                RefreshRunningApps();
                StartAnimation(1d, _visibility.Options.RevealDurationMs);
                break;

            case DockVisibilityState.Visible:
                _runningTimer.Start();
                break;

            case DockVisibilityState.Hiding:
                _panelController.Hide(NowMs());
                StartAnimation(0d, _visibility.Options.HideDurationMs);
                break;

            case DockVisibilityState.Hidden:
                _runningTimer.Stop();
                break;
        }
    }

    private void StartAnimation(double to, int durationMs)
    {
        var settings = _services.Settings.Current;
        if (!settings.AnimationsEnabled || !SystemParameters.ClientAreaAnimation)
        {
            // Reduced motion or animations off: snap instead of sliding.
            durationMs = 0;
        }

        SubscribeRendering();
        _animator.Start(_animator.Progress, to, durationMs, NowMs());
    }

    private void SubscribeRendering()
    {
        if (_animationSubscribed)
        {
            return;
        }

        CompositionTarget.Rendering += OnRendering;
        _animationSubscribed = true;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (!_animator.Tick(NowMs()))
        {
            UnsubscribeRendering();
        }
    }

    private void UnsubscribeRendering()
    {
        if (!_animationSubscribed)
        {
            return;
        }

        CompositionTarget.Rendering -= OnRendering;
        _animationSubscribed = false;
    }

    private bool IsInsideDock(Win32.POINT cursor)
        => cursor.X >= _windowRect.Left && cursor.X < _windowRect.Right
           && cursor.Y >= _windowRect.Top && cursor.Y < _windowRect.Bottom;

    private bool IsCursorAtEdge(Win32.POINT cursor)
    {
        var settings = _services.Settings.Current;
        if (_monitors.MonitorFromPoint(cursor.X, cursor.Y) != _monitor.Monitor)
        {
            return false;
        }

        return DockPosition.IsAtEdge(settings.Edge, _monitor, settings.EdgeActivationPixels, cursor.X, cursor.Y);
    }

    private void ApplyPlacement(double progress)
    {
        if (_hwnd == IntPtr.Zero)
        {
            return;
        }

        var settings = _services.Settings.Current;
        var dipWidth = ActualWidth > 0 ? ActualWidth : _services.ViewModel.Plan.Width;
        var dipHeight = ActualHeight > 0 ? ActualHeight : _services.ViewModel.Plan.Height;

        var placement = DockPosition.Compute(
            settings.Edge,
            _monitor,
            (int)Math.Ceiling(dipWidth),
            (int)Math.Ceiling(dipHeight),
            progress);

        _windowRect = new Win32.RECT
        {
            Left = placement.X,
            Top = placement.Y,
            Right = placement.X + placement.Width,
            Bottom = placement.Y + placement.Height,
        };

        Win32.SetWindowPos(
            _hwnd,
            IntPtr.Zero,
            placement.X,
            placement.Y,
            0,
            0,
            Win32.SwpNoSize | Win32.SwpNoZOrder | Win32.SwpNoActivate);
    }

    private void ApplyLook(DockSettings settings)
    {
        _look.Apply(settings);

        Resources["IconSize"] = _look.IconSize;
        Resources["ItemSize"] = _look.ItemSize;
        Resources["DockPadding"] = new Thickness(_look.DockPadding);
        Resources["SeparatorThickness"] = _look.SeparatorThickness;
        Resources["SeparatorMargin"] = new Thickness(0, _look.SectionGap, 0, _look.SectionGap);
        Resources["SeparatorWidth"] = Math.Max(8, _look.ItemSize - 10);
        Resources["ItemCornerRadius"] = new CornerRadius(_look.ItemCornerRadius);
        Resources["PanelCornerRadius"] = new CornerRadius(_look.PanelCornerRadius);
        Resources["GlyphFontSize"] = _look.GlyphFontSize;
        Resources["RunningIndicatorSize"] = _look.RunningIndicatorSize;

        // Theme brushes live at application level so the dock and the hover panel swap together.
        ThemeService.Apply(ThemeService.Resolve(settings.Theme), _look.PanelOpacity);

        // Tooltips open toward the screen edge, away from where the hover panel appears.
        Resources["TooltipPlacement"] = settings.Edge == DockEdge.Left
            ? System.Windows.Controls.Primitives.PlacementMode.Left
            : System.Windows.Controls.Primitives.PlacementMode.Right;

        ToolTipService.SetIsEnabled(Panel, settings.ShowTooltips);
    }

    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        ApplyLook(e.Settings);
        _visibility.ApplyOptions(e.Settings.CreateVisibilityOptions());
        _cursorTimer.Interval = TimeSpan.FromMilliseconds(e.Settings.CursorPollIntervalMs);
        _panelController.ApplyDelays(e.Settings.HoverDelayMs, _panelController.CloseDelayMs);
        if (!e.Settings.ShowAppItemsOnHover)
        {
            _panelController.Hide(NowMs());
        }

        _hotkeys?.Apply(e.Settings.Shortcuts);
        UpdateSearchVisibility();
        UpdateOverflowClamp();
        RefreshMonitorGeometry();
    }

    private void OnContentChanged(object? sender, EventArgs e)
    {
        UpdateSeparatorVisibility();
        UpdateSearchVisibility();
        UpdateOverflowClamp();
        Dispatcher.BeginInvoke(new Action(() => ApplyPlacement(_animator.Progress)), DispatcherPriority.Loaded);
    }

    private void UpdateSeparatorVisibility()
    {
        // Groups and separators do not count as content for the section dividers.
        var applications = _services.ViewModel.Applications.Count(viewModel => viewModel.Kind.IsOpenable());
        var files = _services.ViewModel.Files.Count(viewModel => viewModel.Kind.IsOpenable());

        ApplicationsSeparator.Visibility = applications > 0 && files > 0 ? Visibility.Visible : Visibility.Collapsed;
        FilesSeparator.Visibility = applications > 0 || files > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Keeps the dock inside the work area: the item area scrolls when there are more pins than the
    /// screen can show, so the dock and its footer buttons always stay reachable.
    /// </summary>
    private void UpdateOverflowClamp()
    {
        if (_hwnd == IntPtr.Zero)
        {
            return;
        }

        var workAreaDips = _monitor.WorkAreaInDips.Height;
        var reserved = (_look.DockPadding * 2)
            + (2 * _look.ItemSize)                       // footer buttons
            + (2 * (_look.SectionGap * 2 + _look.SeparatorThickness)) // section dividers
            + 24;                                        // breathing room

        if (SearchHost.Visibility == Visibility.Visible)
        {
            reserved += 32;
        }

        ItemsScroll.MaxHeight = Math.Max(80, workAreaDips - reserved);
    }

    // ----- search -------------------------------------------------------------------------------

    private void UpdateSearchVisibility()
    {
        var settings = _services.Settings.Current;
        var openable = _services.ViewModel.Applications.Count(viewModel => viewModel.Kind.IsOpenable())
            + _services.ViewModel.Files.Count(viewModel => viewModel.Kind.IsOpenable());

        var visible = settings.SearchEnabled && openable >= settings.SearchThreshold;
        SearchHost.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

        if (!visible && SearchBox.Text.Length > 0)
        {
            SearchBox.Text = string.Empty;
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e) => ApplySearchFilter();

    private void OnSearchPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        // The dock window never activates on its own; while the user types in the filter it has
        // to take activation explicitly or the keystrokes would go to the previous window.
        Win32.SetForegroundWindow(_hwnd);
        SearchBox.Focus();
    }

    private void ApplySearchFilter()
    {
        var query = SearchBox.Text;

        foreach (var source in new object[] { _services.ViewModel.Applications, _services.ViewModel.Files })
        {
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(source);
            view.Filter = item => item is DockItemViewModel viewModel && FilterMatches(viewModel, query);
            view.Refresh();
        }
    }

    private static bool FilterMatches(DockItemViewModel viewModel, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        // While filtering, groups and separators would only add noise.
        return ItemSearchFilter.Matches(viewModel.Item, query);
    }

    // ----- keyboard selection ---------------------------------------------------------------------

    private void OnHotkeyPressed(DockShortcutAction action)
    {
        switch (action)
        {
            case DockShortcutAction.OpenDock:
                if (_visibility.State == DockVisibilityState.Hidden)
                {
                    Reveal();
                }
                else if (_selected is not null)
                {
                    ActivateItem(_selected);
                }
                else
                {
                    Reveal();
                }

                break;

            case DockShortcutAction.NextItem:
                Reveal();
                MoveSelection(+1);
                break;

            case DockShortcutAction.PreviousItem:
                Reveal();
                MoveSelection(-1);
                break;
        }
    }

    private void MoveSelection(int delta)
    {
        var candidates = VisibleOpenableItems();
        if (candidates.Count == 0)
        {
            return;
        }

        var index = _selected is null ? -1 : candidates.IndexOf(_selected);
        if (index < 0)
        {
            index = delta > 0 ? -1 : 0;
        }

        index = ((index + delta) % candidates.Count + candidates.Count) % candidates.Count;
        SelectItem(candidates[index]);
    }

    private void SelectItem(DockItemViewModel? viewModel)
    {
        if (_selected is not null)
        {
            _selected.IsSelected = false;
        }

        _selected = viewModel;

        if (_selected is not null)
        {
            _selected.IsSelected = true;
        }
    }

    /// <summary>All openable items currently shown (search filter applied), in dock order.</summary>
    private List<DockItemViewModel> VisibleOpenableItems()
    {
        var result = new List<DockItemViewModel>();

        foreach (var viewModel in _services.ViewModel.Applications.Concat(_services.ViewModel.Files))
        {
            if (viewModel.Kind.IsOpenable() && FilterMatches(viewModel, SearchBox.Text))
            {
                result.Add(viewModel);
            }
        }

        return result;
    }

    private void RefreshRunningApps()
    {
        _running = _services.Windows.GetRunningApps();
        _services.ViewModel.RefreshRunning(_running, _services.Windows.GetForegroundWindowHandle());
    }

    // ----- pointer / item interaction ---------------------------------------------------------

    private void OnPanelMouseEnter(object sender, MouseEventArgs e) => _visibility.Update(true, true, NowMs());

    private void OnPanelMouseLeave(object sender, MouseEventArgs e)
    {
        if (Win32.GetCursorPos(out var cursor))
        {
            _visibility.Update(false, IsCursorAtEdge(cursor), NowMs());
        }
    }

    private void OnItemMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: DockItemViewModel viewModel }
            || viewModel.Kind != PinnedItemKind.Application)
        {
            return;
        }

        _overDockItem = true;

        if (!ReferenceEquals(_hoveredApp, viewModel))
        {
            _hoveredApp = viewModel;

            if (_panelController.State == TabPanelState.Open)
            {
                // Moving between app items swaps the panel content immediately.
                ShowPanelForHoveredApp();
            }
        }
    }

    private void OnItemMouseLeave(object sender, MouseEventArgs e) => _overDockItem = false;

    // ----- secondary hover panel ---------------------------------------------------------------

    private void OnPanelStateChanged(object? sender, TabPanelStateChangedEventArgs e)
    {
        if (e.Current == TabPanelState.Open)
        {
            ShowPanelForHoveredApp();
            return;
        }

        _contentLoadCts?.Cancel();
        _previewCts?.Cancel();
        _panelRefreshTimer.Stop();
        _tabPanel?.MoveOffScreen();
    }

    private void ShowPanelForHoveredApp()
    {
        var settings = _services.Settings.Current;
        if (!settings.ShowAppItemsOnHover || _hoveredApp?.Item is not AppItem app)
        {
            _panelController.Hide(NowMs());
            return;
        }

        var running = _running.FindByExecutable(app.RunningMatchKey);
        if (running is null)
        {
            // The panel only has something to show for running applications.
            _panelController.Hide(NowMs());
            return;
        }

        EnsureTabPanel();
        if (_tabPanel is null)
        {
            return;
        }

        _tabPanel.EnsureShown();
        _tabPanel.SetPreview(null);
        _tabPanel.SetApplication(app.EffectiveName, app.TargetPath);
        _tabPanel.SetLoading(true);
        _tabPanel.SetItems([]);
        PositionPanel();

        RefreshPanelContent();
    }

    private void EnsureTabPanel()
    {
        if (_tabPanel is not null)
        {
            return;
        }

        var panel = new TabPanelWindow();
        panel.ItemActivated += OnTabPanelItemActivated;
        panel.ItemHovered += OnTabPanelItemHovered;
        panel.ItemHoverCleared += (_, _) => _tabPanel?.SetPreview(null);
        _tabPanel = panel;
    }

    private void RefreshPanelContent()
    {
        if (_tabPanel is null || _hoveredApp?.Item is not AppItem app)
        {
            return;
        }

        var running = _running.FindByExecutable(app.RunningMatchKey);
        if (running is null)
        {
            RefreshRunningApps();
            running = _running.FindByExecutable(app.RunningMatchKey);
        }

        if (running is null)
        {
            _panelController.Hide(NowMs());
            return;
        }

        var settings = _services.Settings.Current;
        var integration = _services.Applications.Resolve(running, _services.DisabledIntegrationSet());
        var options = new IntegrationOptions(
            settings.GroupMultipleWindows,
            settings.ShowWindowPreviews,
            _services.Windows.GetForegroundWindowHandle());

        _contentLoadCts?.Cancel();
        var cts = new CancellationTokenSource();
        _contentLoadCts = cts;

        _ = LoadPanelContentAsync(integration, running, options, cts.Token);
    }

    private async Task LoadPanelContentAsync(
        IApplicationIntegration integration,
        RunningApp running,
        IntegrationOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            var items = await integration.GetContentAsync(running, options, cancellationToken);
            if (cancellationToken.IsCancellationRequested || _tabPanel is null)
            {
                return;
            }

            if (_panelController.State != TabPanelState.Open)
            {
                return;
            }

            if (items.Count <= 1 && !integration.SupportsItemLevelNavigation)
            {
                // One plain window adds nothing over the dock click itself; stay quiet.
                _panelController.Hide(NowMs());
                return;
            }

            _tabPanel.SetItems(items);
            _tabPanel.SetLoading(false);
            PositionPanel();
            _panelRefreshTimer.Start();
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer load.
        }
        catch (Exception ex)
        {
            _services.Logger.Warn($"Could not read {integration.DisplayName} content.", ex);
            _tabPanel?.SetLoading(false);
        }
    }

    /// <summary>
    /// Places the panel beside the dock, next to the hovered item, kept fully inside the work area
    /// so it never covers the dock or reaches the wrong monitor.
    /// </summary>
    private void PositionPanel()
    {
        if (_tabPanel is null || _hwnd == IntPtr.Zero)
        {
            return;
        }

        _tabPanel.UpdateLayout();
        if (!_tabPanel.TryGetPhysicalRect(out var panelRect))
        {
            return;
        }

        var panelWidth = panelRect.Right - panelRect.Left;
        var panelHeight = panelRect.Bottom - panelRect.Top;
        if (panelWidth <= 0 || panelHeight <= 0)
        {
            return;
        }

        const int gap = 10;
        var settings = _services.Settings.Current;
        var x = settings.Edge == DockEdge.Left
            ? _windowRect.Right + gap
            : _windowRect.Left - gap - panelWidth;

        var y = _windowRect.Top;
        if (_hoveredApp is not null && FindItemContainer(_hoveredApp) is { } container)
        {
            var relative = container.TranslatePoint(new Point(0, 0), this);
            y = _windowRect.Top + (int)Math.Round(relative.Y * _monitor.DpiScale);
        }

        var workArea = _monitor.WorkArea;
        var maxY = workArea.Bottom - panelHeight - 8;
        y = Math.Clamp(y, workArea.Top + 8, Math.Max(workArea.Top + 8, maxY));

        _tabPanel.ApplyPhysicalPlacement(x, y);
    }

    private FrameworkElement? FindItemContainer(DockItemViewModel viewModel)
    {
        var list = viewModel.Kind == PinnedItemKind.Application ? ApplicationsList : FilesList;
        var index = list.Items.IndexOf(viewModel);
        return index >= 0 ? list.ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement : null;
    }

    private async void OnTabPanelItemActivated(object? sender, AppContentItem item)
    {
        if (_hoveredApp?.Item is not AppItem app)
        {
            return;
        }

        var running = _running.FindByExecutable(app.RunningMatchKey);
        if (running is null)
        {
            ActivateItem(_hoveredApp);
            return;
        }

        var integration = _services.Applications.Resolve(running, _services.DisabledIntegrationSet());

        try
        {
            var activated = await integration.ActivateItemAsync(item, CancellationToken.None);
            if (!activated)
            {
                _services.Windows.Activate(running);
            }
        }
        catch (Exception ex)
        {
            _services.Logger.Warn($"Could not switch to \"{item.Title}\".", ex);
            _services.Windows.Activate(running);
        }

        _panelController.Hide(NowMs());
    }

    private async void OnTabPanelItemHovered(object? sender, AppContentItem item)
    {
        if (!_services.Settings.Current.ShowWindowPreviews || item.WindowHandle == IntPtr.Zero)
        {
            return;
        }

        _previewCts?.Cancel();
        var cts = new CancellationTokenSource();
        _previewCts = cts;

        try
        {
            var preview = await _services.Previews.CaptureAsync(item.WindowHandle, cts.Token);
            if (!cts.IsCancellationRequested && _tabPanel is not null && _panelController.State == TabPanelState.Open)
            {
                _tabPanel.SetPreview(preview);
            }
        }
        catch (OperationCanceledException)
        {
            // A newer hover superseded this preview.
        }
        catch (Exception ex)
        {
            _services.Logger.Warn("Window preview capture failed.", ex);
        }
    }

    private void OnItemMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not DockItemViewModel viewModel)
        {
            return;
        }

        _pressedItemId = viewModel.Id;
        _pressedElement = element;
        _pressOrigin = e.GetPosition(this);
        _dragStarted = false;

        element.CaptureMouse();
        e.Handled = true;
    }

    private void OnItemMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragStarted || _pressedItemId is null)
        {
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var distance = e.GetPosition(this) - _pressOrigin;
        var threshold = SystemParameters.MinimumHorizontalDragDistance;

        if (Math.Abs(distance.X) <= threshold && Math.Abs(distance.Y) <= threshold)
        {
            return;
        }

        _dragStarted = true;
        _pressedElement?.ReleaseMouseCapture();

        var data = new DataObject(ItemIdDataFormat, _pressedItemId);
        DragDrop.DoDragDrop(_pressedElement!, data, DragDropEffects.Move);

        // The drag loop has ended; a MouseLeftButtonUp will not arrive, so reset here.
        _dragStarted = false;
        _pressedItemId = null;
        _pressedElement = null;
        HideInsertionIndicator();
    }

    private void OnItemMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var element = sender as FrameworkElement;
        element?.ReleaseMouseCapture();

        if (_dragStarted)
        {
            return;
        }

        if (element is not null
            && element.DataContext is DockItemViewModel viewModel
            && viewModel.Id == _pressedItemId)
        {
            ActivateItem(viewModel);
        }

        _pressedItemId = null;
        _pressedElement = null;
        e.Handled = true;
    }

    private void OnItemMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not DockItemViewModel viewModel)
        {
            return;
        }

        _visibility.Show(NowMs());
        ShowItemMenu(element, viewModel);
        e.Handled = true;
    }

    private void ShowItemMenu(FrameworkElement target, DockItemViewModel viewModel)
    {
        var menu = new ContextMenu();

        if (viewModel.Item is GroupHeaderItem)
        {
            var rename = new MenuItem { Header = "Rename group…" };
            rename.Click += (_, _) => RenameGroup(viewModel);
            menu.Items.Add(rename);

            var removeGroup = new MenuItem { Header = "Remove group (items stay on the dock)" };
            removeGroup.Click += (_, _) => _services.Items.Remove(viewModel.Id);
            menu.Items.Add(removeGroup);
        }
        else if (viewModel.Item is SeparatorItem)
        {
            var removeSeparator = new MenuItem { Header = "Remove separator" };
            removeSeparator.Click += (_, _) => _services.Items.Remove(viewModel.Id);
            menu.Items.Add(removeSeparator);
        }
        else
        {
            BuildOpenableItemMenu(menu, viewModel);
        }

        menu.PlacementTarget = target;
        menu.IsOpen = true;
    }

    private void BuildOpenableItemMenu(ContextMenu menu, DockItemViewModel viewModel)
    {
        var id = viewModel.Id;

        var open = new MenuItem { Header = OpenLabelFor(viewModel) };
        open.Click += (_, _) => ActivateItem(viewModel);
        menu.Items.Add(open);

        // Quick actions the integration can run reliably for the running application.
        if (viewModel.Item is AppItem app)
        {
            var running = _running.FindByExecutable(app.RunningMatchKey);
            if (running is not null)
            {
                var integration = _services.Applications.Resolve(running, _services.DisabledIntegrationSet());
                foreach (var action in integration.GetQuickActions(running))
                {
                    var quick = new MenuItem { Header = action.Label };
                    var request = action.Request;
                    quick.Click += (_, _) => LaunchQuickAction(request);
                    menu.Items.Add(quick);
                }
            }
        }

        if (viewModel.Kind == PinnedItemKind.File)
        {
            var containing = new MenuItem { Header = "Open file location" };
            containing.Click += (_, _) => _services.Shell.OpenContainingFolder(viewModel.TargetPath);
            menu.Items.Add(containing);
        }

        menu.Items.Add(new Separator());

        var groups = _services.Items.GetGroups(viewModel.Section);
        var moveTo = new MenuItem { Header = "Move to group" };

        var none = new MenuItem { Header = "No group" };
        none.Click += (_, _) => _services.Items.MoveToGroup(id, null);
        moveTo.Items.Add(none);

        foreach (var group in groups)
        {
            var entry = new MenuItem { Header = group.EffectiveName };
            var groupId = group.Id;
            entry.Click += (_, _) => _services.Items.MoveToGroup(id, groupId);
            moveTo.Items.Add(entry);
        }

        moveTo.Items.Add(new Separator());
        var newGroup = new MenuItem { Header = "New group…" };
        newGroup.Click += (_, _) =>
        {
            var header = PromptNewGroup(viewModel.Section);
            if (header is not null)
            {
                _services.Items.MoveToGroup(id, header.Id);
            }
        };
        moveTo.Items.Add(newGroup);
        menu.Items.Add(moveTo);

        var up = new MenuItem { Header = "Move up" };
        up.Click += (_, _) => _services.Items.MoveUp(id);
        menu.Items.Add(up);

        var down = new MenuItem { Header = "Move down" };
        down.Click += (_, _) => _services.Items.MoveDown(id);
        menu.Items.Add(down);

        var separatorBelow = new MenuItem { Header = "Add separator below" };
        separatorBelow.Click += (_, _) => InsertSeparatorBelow(viewModel);
        menu.Items.Add(separatorBelow);

        menu.Items.Add(new Separator());

        var remove = new MenuItem { Header = "Remove from dock" };
        remove.Click += (_, _) =>
        {
            if (ReferenceEquals(_selected, viewModel))
            {
                SelectItem(null);
            }

            _services.Items.Remove(id);
        };
        menu.Items.Add(remove);
    }

    private void LaunchQuickAction(LaunchRequest request)
    {
        var result = _services.Shell.Launch(request);
        if (!result.Success)
        {
            _services.Logger.Warn($"Quick action failed: {result.Error}");
        }
    }

    private GroupHeaderItem? PromptNewGroup(DockSection section)
    {
        var name = Microsoft.VisualBasic.Interaction.InputBox(
            "Name the new group",
            "Dock Manager",
            section == DockSection.Applications ? "Apps" : "Files");

        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return _services.Items.AddGroup(section, name.Trim());
    }

    private void RenameGroup(DockItemViewModel viewModel)
    {
        if (viewModel.Item is not GroupHeaderItem header)
        {
            return;
        }

        var name = Microsoft.VisualBasic.Interaction.InputBox("Rename group", "Dock Manager", header.EffectiveName);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        header.DisplayName = name.Trim();

        // Rebuild the view models so the header label refreshes.
        _services.Items.ReplaceAll(_services.Items.Items);
    }

    private void InsertSeparatorBelow(DockItemViewModel viewModel)
    {
        var sectionItems = _services.Items.GetItems(viewModel.Section);
        var index = -1;
        for (var i = 0; i < sectionItems.Count; i++)
        {
            if (ReferenceEquals(sectionItems[i], viewModel.Item))
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            return;
        }

        var separator = _services.Items.AddSeparator(viewModel.Section);
        _services.Items.Move(separator.Id, index + 1);
    }

    private static string OpenLabelFor(DockItemViewModel viewModel) => viewModel.Kind switch
    {
        PinnedItemKind.Application => "Launch",
        PinnedItemKind.Folder => "Open in Explorer",
        _ => "Open",
    };

    private void ActivateItem(DockItemViewModel viewModel)
    {
        var item = viewModel.Item;

        if (!item.Kind.IsOpenable())
        {
            return;
        }

        if (!viewModel.IsAvailable)
        {
            _services.Logger.Warn($"'{item.EffectiveName}' is no longer available at {item.TargetPath}.");
            return;
        }

        RefreshRunningApps();

        if (item is AppItem app)
        {
            var running = _running.FindByExecutable(app.RunningMatchKey);
            if (running is not null && _services.Windows.Activate(running))
            {
                return;
            }
        }

        var request = item.CreateLaunchRequest(_services.FileSystem);
        if (request is null)
        {
            _services.Logger.Warn($"No launch request for '{item.EffectiveName}'.");
            return;
        }

        var result = _services.Shell.Launch(request);
        if (!result.Success)
        {
            ShowLaunchFailure(item, result.Error);
        }
    }

    private void ShowLaunchFailure(PinnedItem item, string? error)
    {
        MessageBox.Show(
            this,
            $"Could not open \"{item.EffectiveName}\".{(error is null ? string.Empty : $"\n{error}")}",
            "Dock Manager",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    // ----- footer buttons --------------------------------------------------------------------

    private void OnAddClicked(object sender, RoutedEventArgs e)
    {
        _visibility.Show(NowMs());
        ShowAddMenu(AddButton);
    }

    private void OnSettingsClicked(object sender, RoutedEventArgs e)
    {
        _visibility.Show(NowMs());
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ShowAddMenu(FrameworkElement target)
    {
        var menu = new ContextMenu();

        var application = new MenuItem { Header = "Application…" };
        application.Click += (_, _) => AddApplication();
        menu.Items.Add(application);

        var file = new MenuItem { Header = "File…" };
        file.Click += (_, _) => AddFile();
        menu.Items.Add(file);

        var folder = new MenuItem { Header = "Folder…" };
        folder.Click += (_, _) => AddFolder();
        menu.Items.Add(folder);

        menu.PlacementTarget = target;
        menu.IsOpen = true;
    }

    private void AddApplication()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Pin an application",
            Filter = "Applications (*.exe;*.lnk)|*.exe;*.lnk|All files (*.*)|*.*",
            Multiselect = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        };

        if (dialog.ShowDialog(this) == true)
        {
            PinPaths(dialog.FileNames);
        }
    }

    private void AddFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Pin a file",
            Filter = "All files (*.*)|*.*",
            Multiselect = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        };

        if (dialog.ShowDialog(this) == true)
        {
            PinPaths(dialog.FileNames);
        }
    }

    private void AddFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Pin a folder",
            Multiselect = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        };

        if (dialog.ShowDialog(this) == true)
        {
            PinPaths(dialog.FolderNames);
        }
    }

    private void PinPaths(IEnumerable<string> paths)
    {
        var created = PinnedItemFactory.CreateFromPaths(paths, _services.FileSystem, _services.Shortcuts);
        var added = _services.Items.AddRange(created);
        _services.Logger.Info($"Pinned {added} of {created.Count} dropped item(s).");
    }

    // ----- panel background menu -------------------------------------------------------------

    private void OnPanelMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        _visibility.Show(NowMs());

        var menu = new ContextMenu();

        var add = new MenuItem { Header = "Add item" };
        var addApp = new MenuItem { Header = "Application…" };
        addApp.Click += (_, _) => AddApplication();
        add.Items.Add(addApp);
        var addFile = new MenuItem { Header = "File…" };
        addFile.Click += (_, _) => AddFile();
        add.Items.Add(addFile);
        var addFolder = new MenuItem { Header = "Folder…" };
        addFolder.Click += (_, _) => AddFolder();
        add.Items.Add(addFolder);
        menu.Items.Add(add);

        var group = new MenuItem { Header = "New group…" };
        var groupApps = new MenuItem { Header = "For applications" };
        groupApps.Click += (_, _) => PromptNewGroup(DockSection.Applications);
        group.Items.Add(groupApps);
        var groupFiles = new MenuItem { Header = "For files and folders" };
        groupFiles.Click += (_, _) => PromptNewGroup(DockSection.Files);
        group.Items.Add(groupFiles);
        menu.Items.Add(group);

        var divider = new MenuItem { Header = "Add separator" };
        var dividerApps = new MenuItem { Header = "Between applications" };
        dividerApps.Click += (_, _) => _services.Items.AddSeparator(DockSection.Applications);
        divider.Items.Add(dividerApps);
        var dividerFiles = new MenuItem { Header = "Between files and folders" };
        dividerFiles.Click += (_, _) => _services.Items.AddSeparator(DockSection.Files);
        divider.Items.Add(dividerFiles);
        menu.Items.Add(divider);

        menu.Items.Add(new Separator());

        var settings = new MenuItem { Header = "Settings…" };
        settings.Click += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(settings);

        var quit = new MenuItem { Header = "Quit Dock Manager" };
        quit.Click += (_, _) => QuitRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(quit);

        menu.PlacementTarget = Panel;
        menu.IsOpen = true;
        e.Handled = true;
    }

    // ----- drag and drop ---------------------------------------------------------------------

    private void OnPanelDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnPanelDrop(object sender, DragEventArgs e)
    {
        HideInsertionIndicator();
        if (TryGetDroppedPaths(e, out var paths))
        {
            PinPaths(paths);
        }

        e.Handled = true;
    }

    private void OnItemDragOver(object sender, DragEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        if (e.Data.GetDataPresent(ItemIdDataFormat) && element.DataContext is DockItemViewModel)
        {
            var list = FindOwnerList(element);
            if (list is null)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            var index = GetInsertionIndex(list, e.GetPosition(list).Y);
            ShowInsertionIndicator(list, index);
            e.Effects = DragDropEffects.Move;
        }
        else if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void OnItemDrop(object sender, DragEventArgs e)
    {
        HideInsertionIndicator();

        if (e.Data.GetDataPresent(ItemIdDataFormat)
            && sender is FrameworkElement element
            && element.DataContext is DockItemViewModel
            && FindOwnerList(element) is { } list
            && e.Data.GetData(ItemIdDataFormat) is string id)
        {
            var insertion = GetInsertionIndex(list, e.GetPosition(list).Y);
            var draggedIndex = IndexOf(list, id);
            if (draggedIndex >= 0 && draggedIndex < insertion)
            {
                insertion--;
            }

            _services.Items.Move(id, insertion);
        }
        else if (TryGetDroppedPaths(e, out var paths))
        {
            PinPaths(paths);
        }

        e.Handled = true;
    }

    private static bool TryGetDroppedPaths(DragEventArgs e, out string[] paths)
    {
        paths = [];
        if (e.Data.GetData(DataFormats.FileDrop) is string[] dropped && dropped.Length > 0)
        {
            paths = dropped;
            return true;
        }

        return false;
    }

    private ItemsControl? FindOwnerList(DependencyObject element)
    {
        var current = element;
        while (current is not null)
        {
            if (ReferenceEquals(current, ApplicationsList) || ReferenceEquals(current, FilesList))
            {
                return (ItemsControl)current;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static int IndexOf(ItemsControl list, string id)
    {
        for (var i = 0; i < list.Items.Count; i++)
        {
            if (list.Items[i] is DockItemViewModel viewModel && viewModel.Id == id)
            {
                return i;
            }
        }

        return -1;
    }

    private static int GetInsertionIndex(ItemsControl list, double y)
    {
        for (var i = 0; i < list.Items.Count; i++)
        {
            if (list.ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement container)
            {
                var mid = container.TranslatePoint(new Point(0, container.ActualHeight / 2d), list).Y;
                if (y < mid)
                {
                    return i;
                }
            }
        }

        return list.Items.Count;
    }

    private void ShowInsertionIndicator(ItemsControl list, int index)
    {
        double y;
        if (index < list.Items.Count
            && list.ItemContainerGenerator.ContainerFromIndex(index) is FrameworkElement container)
        {
            y = container.TranslatePoint(new Point(0, 0), Overlay).Y - 2;
        }
        else if (list.ItemContainerGenerator.ContainerFromIndex(list.Items.Count - 1) is FrameworkElement last)
        {
            y = last.TranslatePoint(new Point(0, last.ActualHeight), Overlay).Y + 1;
        }
        else
        {
            return;
        }

        Canvas.SetLeft(InsertionIndicator, _look.DockPadding);
        Canvas.SetTop(InsertionIndicator, y);
        InsertionIndicator.Width = _look.ItemSize;
        InsertionIndicator.Visibility = Visibility.Visible;
    }

    private void HideInsertionIndicator() => InsertionIndicator.Visibility = Visibility.Collapsed;

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _cursorTimer.Stop();
        _runningTimer.Stop();
        _panelRefreshTimer.Stop();
        _contentLoadCts?.Cancel();
        _previewCts?.Cancel();
        _hotkeys?.UnregisterAll();
        _foregroundWatcher.Dispose();
        UnsubscribeRendering();
        _tabPanel?.Close();
        _tabPanel = null;
    }
}
