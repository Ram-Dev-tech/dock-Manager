using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using DockManager.App.Composition;
using DockManager.App.Ui;
using DockManager.App.Windows;
using DockManager.Core.Dock;
using DockManager.Core.Items;
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

        _monitor = _monitors.GetFor(_hwnd);
        ApplyPlacement(0d);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        var message = (uint)msg;
        if (message is Win32.WmDisplayChange or Win32.WmDpiChanged or Win32.WmSettingChange)
        {
            Dispatcher.BeginInvoke(new Action(RefreshMonitorGeometry));
        }

        return IntPtr.Zero;
    }

    private void RefreshMonitorGeometry()
    {
        if (_hwnd == IntPtr.Zero)
        {
            return;
        }

        _monitor = _monitors.GetFor(_hwnd);
        ApplyPlacement(_animator.Progress);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _monitor = _monitors.GetFor(_hwnd);
        UpdateSeparatorVisibility();
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

        var inside = IsInsideDock(cursor);
        var atEdge = !inside && IsCursorAtEdge(cursor);
        _visibility.Update(inside, atEdge, NowMs());
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
                StartAnimation(0d, _visibility.Options.HideDurationMs);
                break;

            case DockVisibilityState.Hidden:
                _runningTimer.Stop();
                break;
        }
    }

    private void StartAnimation(double to, int durationMs)
    {
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
        Resources["PanelBackground"] = CreatePanelBrush(_look.PanelOpacity);

        ToolTipService.SetIsEnabled(Panel, settings.ShowTooltips);
    }

    private static Brush CreatePanelBrush(double opacity)
    {
        var alpha = (byte)Math.Clamp((int)Math.Round(opacity * 255), 89, 255);
        var brush = new SolidColorBrush(Color.FromArgb(alpha, 0x1C, 0x1C, 0x21));
        brush.Freeze();
        return brush;
    }

    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
    {
        ApplyLook(e.Settings);
        _visibility.ApplyOptions(e.Settings.CreateVisibilityOptions());
        _cursorTimer.Interval = TimeSpan.FromMilliseconds(e.Settings.CursorPollIntervalMs);
        RefreshMonitorGeometry();
    }

    private void OnContentChanged(object? sender, EventArgs e)
    {
        UpdateSeparatorVisibility();
        Dispatcher.BeginInvoke(new Action(() => ApplyPlacement(_animator.Progress)), DispatcherPriority.Loaded);
    }

    private void UpdateSeparatorVisibility()
    {
        var applications = _services.ViewModel.Applications.Count;
        var files = _services.ViewModel.Files.Count;

        ApplicationsSeparator.Visibility = applications > 0 && files > 0 ? Visibility.Visible : Visibility.Collapsed;
        FilesSeparator.Visibility = applications > 0 || files > 0 ? Visibility.Visible : Visibility.Collapsed;
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

        var open = new MenuItem { Header = OpenLabelFor(viewModel) };
        open.Click += (_, _) => ActivateItem(viewModel);
        menu.Items.Add(open);

        if (viewModel.Kind == PinnedItemKind.File)
        {
            var containing = new MenuItem { Header = "Open file location" };
            containing.Click += (_, _) => _services.Shell.OpenContainingFolder(viewModel.TargetPath);
            menu.Items.Add(containing);
        }

        menu.Items.Add(new Separator());

        var up = new MenuItem { Header = "Move up" };
        up.Click += (_, _) => _services.Items.MoveUp(viewModel.Id);
        menu.Items.Add(up);

        var down = new MenuItem { Header = "Move down" };
        down.Click += (_, _) => _services.Items.MoveDown(viewModel.Id);
        menu.Items.Add(down);

        menu.Items.Add(new Separator());

        var remove = new MenuItem { Header = "Remove from dock" };
        remove.Click += (_, _) => _services.Items.Remove(viewModel.Id);
        menu.Items.Add(remove);

        menu.PlacementTarget = target;
        menu.IsOpen = true;
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
        _foregroundWatcher.Dispose();
        UnsubscribeRendering();
    }
}
