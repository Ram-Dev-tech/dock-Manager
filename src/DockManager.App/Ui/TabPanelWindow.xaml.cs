using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using DockManager.App.Dock;
using DockManager.Core.Integrations;

namespace DockManager.App.Ui;

/// <summary>
/// The secondary hover panel: shows the entries (tabs, documents, folders, windows) of the hovered
/// application and switches to the clicked one. Like the dock it never takes activation; its
/// physical rectangle is reported to the dock so the cursor poll keeps it open.
/// </summary>
public sealed partial class TabPanelWindow : Window
{
    private IntPtr _hwnd;

    public TabPanelWindow()
    {
        InitializeComponent();
    }

    /// <summary>Raised when an entry is clicked.</summary>
    public event EventHandler<AppContentItem>? ItemActivated;

    /// <summary>Raised when the cursor enters an entry (drives the optional preview).</summary>
    public event EventHandler<AppContentItem>? ItemHovered;

    /// <summary>Raised when the cursor leaves the entry list.</summary>
    public event EventHandler? ItemHoverCleared;

    /// <summary>Raised when the cursor enters the panel surface.</summary>
    public event EventHandler? PanelCursorEntered;

    /// <summary>Raised when the cursor leaves the panel surface.</summary>
    public event EventHandler? PanelCursorLeft;

    public bool IsCreated => _hwnd != IntPtr.Zero;

    /// <summary>Whether the cursor (physical pixels) is inside the panel window.</summary>
    public bool ContainsPoint(Win32.POINT point)
        => _hwnd != IntPtr.Zero
           && Win32.GetWindowRect(_hwnd, out var rect)
           && point.X >= rect.Left && point.X < rect.Right
           && point.Y >= rect.Top && point.Y < rect.Bottom;

    /// <summary>The panel's physical rectangle, or an empty rect while it is off screen.</summary>
    public bool TryGetPhysicalRect(out Win32.RECT rect)
    {
        rect = default;
        return _hwnd != IntPtr.Zero && Win32.GetWindowRect(_hwnd, out rect) && rect.Left >= -16000;
    }

    /// <summary>Moves the already laid out panel into place without activating it.</summary>
    public void ApplyPhysicalPlacement(int x, int y)
    {
        if (_hwnd == IntPtr.Zero)
        {
            return;
        }

        Win32.SetWindowPos(
            _hwnd,
            IntPtr.Zero,
            x,
            y,
            0,
            0,
            Win32.SwpNoSize | Win32.SwpNoZOrder | Win32.SwpNoActivate);
    }

    public void SetApplication(string title, string? iconPath)
    {
        AppTitle.Text = title;
        AppIcon.Source = string.IsNullOrWhiteSpace(iconPath)
            ? null
            : IconConverter.Provider?.GetIcon(iconPath, large: false) as System.Windows.Media.ImageSource;
    }

    public void SetLoading(bool loading)
    {
        LoadingText.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
    }

    public void SetItems(IReadOnlyList<AppContentItem> items)
    {
        ItemsList.ItemsSource = items.Count == 0 ? null : new List<AppContentItem>(items);
    }

    public void SetPreview(BitmapSource? preview)
    {
        if (preview is null)
        {
            PreviewBox.Visibility = Visibility.Collapsed;
            PreviewImage.Source = null;
            return;
        }

        PreviewImage.Source = preview;
        PreviewBox.Visibility = Visibility.Visible;
    }

    /// <summary>Ensures the window is shown (it starts off screen) so layout can run.</summary>
    public void EnsureShown()
    {
        if (!IsVisible)
        {
            Show();
        }
    }

    /// <summary>Parks the panel off screen without closing it, ready for instant reuse.</summary>
    public void MoveOffScreen()
    {
        SetPreview(null);
        SetLoading(false);
        ItemsList.ItemsSource = null;
        ApplyPhysicalPlacement(-32000, 0);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        if (PresentationSource.FromVisual(this) is not HwndSource source)
        {
            return;
        }

        _hwnd = source.Handle;

        var exStyle = Win32.GetWindowLongPtrSafe(_hwnd, Win32.GwlExStyle).ToInt64();
        exStyle &= ~Win32.WsExAppWindow;
        exStyle |= Win32.WsExToolWindow | Win32.WsExNoActivate;
        Win32.SetWindowLongPtrSafe(_hwnd, Win32.GwlExStyle, new IntPtr(exStyle));
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _hwnd = IntPtr.Zero;
    }

    private void OnPanelMouseEnter(object sender, MouseEventArgs e) => PanelCursorEntered?.Invoke(this, EventArgs.Empty);

    private void OnPanelMouseLeave(object sender, MouseEventArgs e)
    {
        ItemHoverCleared?.Invoke(this, EventArgs.Empty);
        PanelCursorLeft?.Invoke(this, EventArgs.Empty);
    }

    private void OnItemMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: AppContentItem item })
        {
            ItemActivated?.Invoke(this, item);
        }

        e.Handled = true;
    }

    private void OnItemMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: AppContentItem item })
        {
            ItemHovered?.Invoke(this, item);
        }
    }

    private void OnItemMouseLeave(object sender, MouseEventArgs e) => ItemHoverCleared?.Invoke(this, EventArgs.Empty);
}
