using System.Windows;
using System.Windows.Controls;
using DockManager.Core.Dock;
using DockManager.Core.Integrations;
using DockManager.Core.Settings;

namespace DockManager.App.Settings;

/// <summary>
/// The settings surface: position, size, auto-hide, application intelligence and startup. Every
/// change is routed through <see cref="SettingsStore.Update"/> so it is clamped, persisted and
/// broadcast.
/// </summary>
public sealed partial class SettingsWindow : Window
{
    private readonly SettingsStore _settings;
    private readonly IStartupRegistration _startup;
    private readonly IReadOnlyList<IApplicationIntegration> _integrations;
    private bool _loading;

    public SettingsWindow(
        SettingsStore settings,
        IStartupRegistration startup,
        IReadOnlyList<IApplicationIntegration>? integrations = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _startup = startup ?? throw new ArgumentNullException(nameof(startup));
        _integrations = integrations ?? [];

        InitializeComponent();

        BuildIntegrationCheckboxes();

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

        HoverPanelBox.IsChecked = settings.ShowAppItemsOnHover;
        PreviewBox.IsChecked = settings.ShowWindowPreviews;
        HoverDelaySlider.Value = settings.HoverDelayMs;
        HoverDelayValue.Text = $"{settings.HoverDelayMs} ms";
        HoverDelaySlider.IsEnabled = settings.ShowAppItemsOnHover;
        PreviewBox.IsEnabled = settings.ShowAppItemsOnHover;
        IntegrationListHost.IsEnabled = settings.ShowAppItemsOnHover;

        var disabled = new HashSet<string>(settings.DisabledIntegrations, StringComparer.Ordinal);
        foreach (var child in IntegrationListHost.Children)
        {
            if (child is CheckBox { Tag: string id } box)
            {
                box.IsChecked = !disabled.Contains(id);
            }
        }

        _loading = false;
    }

    private void BuildIntegrationCheckboxes()
    {
        foreach (var integration in _integrations)
        {
            var box = new CheckBox
            {
                Content = integration.DisplayName,
                Foreground = System.Windows.Media.Brushes.White,
                Tag = integration.Id,
                Margin = new Thickness(0, 0, 0, 4),
            };
            box.Checked += OnIntegrationToggled;
            box.Unchecked += OnIntegrationToggled;
            IntegrationListHost.Children.Add(box);
        }
    }

    private void OnHoverPanelChanged(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        _settings.Update(settings => settings.ShowAppItemsOnHover = HoverPanelBox.IsChecked == true);
    }

    private void OnPreviewChanged(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        _settings.Update(settings => settings.ShowWindowPreviews = PreviewBox.IsChecked == true);
    }

    private void OnHoverDelayChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading)
        {
            return;
        }

        var value = (int)Math.Round(e.NewValue);
        _settings.Update(settings => settings.HoverDelayMs = value);
    }

    private void OnIntegrationToggled(object sender, RoutedEventArgs e)
    {
        if (_loading || sender is not CheckBox { Tag: string id })
        {
            return;
        }

        var enabled = ((CheckBox)sender).IsChecked == true;
        _settings.Update(settings =>
        {
            settings.DisabledIntegrations = settings.DisabledIntegrations
                .Where(existing => existing != id)
                .ToList();

            if (!enabled)
            {
                settings.DisabledIntegrations.Add(id);
            }
        });
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
