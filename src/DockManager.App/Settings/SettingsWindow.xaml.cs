using System.Windows;
using System.Windows.Controls;
using DockManager.Core.Dock;
using DockManager.Core.Settings;

namespace DockManager.App.Settings;

/// <summary>
/// The minimal Phase 1 settings surface: position, size, auto-hide and startup. Every change is
/// routed through <see cref="SettingsStore.Update"/> so it is clamped, persisted and broadcast.
/// </summary>
public sealed partial class SettingsWindow : Window
{
    private readonly SettingsStore _settings;
    private readonly IStartupRegistration _startup;
    private bool _loading;

    public SettingsWindow(SettingsStore settings, IStartupRegistration startup)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _startup = startup ?? throw new ArgumentNullException(nameof(startup));

        InitializeComponent();

        _settings.Changed += OnSettingsChanged;
        Loaded += (_, _) => Refresh();

        Refresh();
    }

    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e) => Refresh();

    private void Refresh()
    {
        _loading = true;
        var settings = _settings.Current;

        EdgeLeft.IsChecked = settings.Edge == DockEdge.Left;
        EdgeRight.IsChecked = settings.Edge == DockEdge.Right;

        SizeCompact.IsChecked = settings.Size == DockSizeScale.Compact;
        SizeNormal.IsChecked = settings.Size == DockSizeScale.Normal;
        SizeLarge.IsChecked = settings.Size == DockSizeScale.Large;

        IconSlider.Value = settings.IconSize;
        IconValue.Text = settings.IconSize.ToString();

        AutoHideBox.IsChecked = settings.AutoHide;
        HideDelaySlider.Value = settings.HideDelayMs;
        HideDelaySlider.IsEnabled = settings.AutoHide;
        HideDelayLabel.IsEnabled = settings.AutoHide;

        StartupBox.IsChecked = settings.LaunchAtStartup;

        _loading = false;
    }

    private void OnEdgeChecked(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        var edge = Equals(sender, EdgeRight) ? DockEdge.Right : DockEdge.Left;
        _settings.Update(settings => settings.Edge = edge);
    }

    private void OnSizeChecked(object sender, RoutedEventArgs e)
    {
        if (_loading || sender is not RadioButton)
        {
            return;
        }

        var scale = Equals(sender, SizeCompact) ? DockSizeScale.Compact
            : Equals(sender, SizeLarge) ? DockSizeScale.Large
            : DockSizeScale.Normal;

        _settings.Update(settings =>
        {
            settings.Size = scale;
            settings.IconSize = scale.IconSizeFor();
        });
    }

    private void OnIconSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading)
        {
            return;
        }

        var value = (int)Math.Round(e.NewValue);
        _settings.Update(settings => settings.IconSize = value);
    }

    private void OnAutoHideChanged(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        _settings.Update(settings => settings.AutoHide = AutoHideBox.IsChecked == true);
    }

    private void OnHideDelayChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading)
        {
            return;
        }

        var value = (int)Math.Round(e.NewValue);
        _settings.Update(settings => settings.HideDelayMs = value);
    }

    private void OnStartupChanged(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        _settings.Update(settings => settings.LaunchAtStartup = StartupBox.IsChecked == true);
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        _settings.Changed -= OnSettingsChanged;
        base.OnClosed(e);
    }
}
