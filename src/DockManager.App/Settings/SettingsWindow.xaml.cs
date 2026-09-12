using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DockManager.App.Dock;
using DockManager.Core.Dock;
using DockManager.Core.Integrations;
using DockManager.Core.Settings;
using DockManager.Core.Shortcuts;

namespace DockManager.App.Settings;

/// <summary>
/// The settings surface, organized the way the product reads: Dock, Appearance, Items,
/// Applications, Shortcuts and General. Every change is routed through
/// <see cref="SettingsStore.Update"/> so it is clamped, persisted and broadcast. Global shortcuts
/// are captured here but never override Windows: reserved combinations are refused with an
/// explanation, and registrations another application already holds surface as warnings.
/// </summary>
public sealed partial class SettingsWindow : Window
{
    private readonly SettingsStore _settings;
    private readonly IStartupRegistration _startup;
    private readonly IReadOnlyList<IApplicationIntegration> _integrations;
    private readonly Func<IReadOnlyList<ShortcutRecord>> _conflictsProvider;
    private readonly Func<IReadOnlyList<MonitorSummary>> _monitorsProvider;
    private TextBox? _capturingBox;
    private bool _loading;

    public SettingsWindow(
        SettingsStore settings,
        IStartupRegistration startup,
        IReadOnlyList<IApplicationIntegration>? integrations = null,
        Func<IReadOnlyList<ShortcutRecord>>? conflictsProvider = null,
        Func<IReadOnlyList<MonitorSummary>>? monitorsProvider = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _startup = startup ?? throw new ArgumentNullException(nameof(startup));
        _integrations = integrations ?? [];
        _conflictsProvider = conflictsProvider ?? (() => []);
        _monitorsProvider = monitorsProvider ?? (() => []);

        InitializeComponent();

        BuildIntegrationCheckboxes();
        BuildMonitorCombo();

        _settings.Changed += OnSettingsChanged;
        Loaded += (_, _) => Refresh();

        Refresh();
    }

    private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e) => Refresh();

    private void Refresh()
    {
        _loading = true;
        var settings = _settings.Current;

        // Dock
        EdgeLeft.IsChecked = settings.Edge == DockEdge.Left;
        EdgeRight.IsChecked = settings.Edge == DockEdge.Right;
        PreviewLeft.Visibility = settings.Edge == DockEdge.Left ? Visibility.Visible : Visibility.Collapsed;
        PreviewRight.Visibility = settings.Edge == DockEdge.Right ? Visibility.Visible : Visibility.Collapsed;

        SensitivityLess.IsChecked = settings.Sensitivity == EdgeSensitivity.LessSensitive;
        SensitivityNormal.IsChecked = settings.Sensitivity == EdgeSensitivity.Normal;
        SensitivityMore.IsChecked = settings.Sensitivity == EdgeSensitivity.Sensitive;

        AutoHideBox.IsChecked = settings.AutoHide;
        HideDelaySlider.Value = settings.HideDelayMs;
        HideDelaySlider.IsEnabled = settings.AutoHide;
        HideDelayLabel.IsEnabled = settings.AutoHide;

        // Appearance
        SizeCompact.IsChecked = settings.Size == DockSizeScale.Compact;
        SizeNormal.IsChecked = settings.Size == DockSizeScale.Normal;
        SizeLarge.IsChecked = settings.Size == DockSizeScale.Large;
        IconSlider.Value = settings.IconSize;
        IconValue.Text = settings.IconSize.ToString();

        ThemeSystem.IsChecked = settings.Theme == DockTheme.System;
        ThemeLight.IsChecked = settings.Theme == DockTheme.Light;
        ThemeDark.IsChecked = settings.Theme == DockTheme.Dark;
        AnimationsBox.IsChecked = settings.AnimationsEnabled;

        // Items
        SearchBoxSetting.IsChecked = settings.SearchEnabled;

        // Applications
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

        // Shortcuts
        if (_capturingBox is null)
        {
            OpenDockShortcut.Text = ShortcutFormatter.Format(settings.ShortcutFor(DockShortcutAction.OpenDock));
            NextItemShortcut.Text = ShortcutFormatter.Format(settings.ShortcutFor(DockShortcutAction.NextItem));
            PreviousItemShortcut.Text = ShortcutFormatter.Format(settings.ShortcutFor(DockShortcutAction.PreviousItem));
        }

        RefreshShortcutConflicts();

        // General
        StartupBox.IsChecked = settings.LaunchAtStartup;
        SelectMonitor(settings.MonitorName);

        _loading = false;
    }

    // ----- dock ----------------------------------------------------------------------------------

    private void OnEdgeChecked(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        var edge = Equals(sender, EdgeRight) ? DockEdge.Right : DockEdge.Left;
        _settings.Update(settings => settings.Edge = edge);
    }

    private void OnSensitivityChecked(object sender, RoutedEventArgs e)
    {
        if (_loading || sender is not RadioButton)
        {
            return;
        }

        var sensitivity = Equals(sender, SensitivityLess) ? EdgeSensitivity.LessSensitive
            : Equals(sender, SensitivityMore) ? EdgeSensitivity.Sensitive
            : EdgeSensitivity.Normal;

        _settings.Update(settings => settings.Sensitivity = sensitivity);
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

    // ----- appearance ------------------------------------------------------------------------------

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

    private void OnThemeChecked(object sender, RoutedEventArgs e)
    {
        if (_loading || sender is not RadioButton)
        {
            return;
        }

        var theme = Equals(sender, ThemeLight) ? DockTheme.Light
            : Equals(sender, ThemeDark) ? DockTheme.Dark
            : DockTheme.System;

        _settings.Update(settings => settings.Theme = theme);
    }

    private void OnAnimationsChanged(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        _settings.Update(settings => settings.AnimationsEnabled = AnimationsBox.IsChecked == true);
    }

    // ----- items ------------------------------------------------------------------------------------

    private void OnSearchChanged(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        _settings.Update(settings => settings.SearchEnabled = SearchBoxSetting.IsChecked == true);
    }

    // ----- applications -------------------------------------------------------------------------------

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

    // ----- shortcuts ---------------------------------------------------------------------------------

    private void OnShortcutBoxMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TextBox box)
        {
            return;
        }

        _capturingBox = box;
        box.Text = "Press keys…";
        ShortcutWarning.Visibility = Visibility.Collapsed;
        box.Focus();
        e.Handled = true;
    }

    private void OnShortcutBoxLostFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (!ReferenceEquals(_capturingBox, sender))
        {
            return;
        }

        _capturingBox = null;
        Refresh();
    }

    private void OnShortcutBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (!ReferenceEquals(_capturingBox, sender) || sender is not TextBox { Tag: string tag })
        {
            return;
        }

        e.Handled = true;

        if (e.Key == Key.Escape)
        {
            _capturingBox = null;
            Refresh();
            return;
        }

        var action = ParseAction(tag);
        var key = (uint)KeyInterop.VirtualKeyFromKey(e.Key);
        var modifiers = ToWin32Modifiers(Keyboard.Modifiers);

        var rejection = ShortcutValidator.RejectionReason(modifiers, key);
        if (rejection is not null)
        {
            ShowShortcutWarning(rejection);
            return;
        }

        var takenBy = _settings.Current.Shortcuts.FirstOrDefault(record =>
            record.Action != action && record.Modifiers == modifiers && record.Key == key);
        if (takenBy is not null)
        {
            ShowShortcutWarning($"That combination is already used for \"{NameOf(takenBy.Action)}\".");
            return;
        }

        _settings.Update(settings =>
        {
            var record = settings.Shortcuts.FirstOrDefault(candidate => candidate.Action == action);
            if (record is null)
            {
                record = new ShortcutRecord { Action = action };
                settings.Shortcuts.Add(record);
            }

            record.Modifiers = modifiers;
            record.Key = key;
        });

        _capturingBox = null;
        Refresh();
    }

    private void OnShortcutClearClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tag })
        {
            return;
        }

        var action = ParseAction(tag);
        _settings.Update(settings =>
        {
            var record = settings.Shortcuts.FirstOrDefault(candidate => candidate.Action == action);
            if (record is not null)
            {
                record.Modifiers = 0;
                record.Key = 0;
            }
        });

        _capturingBox = null;
        Refresh();
    }

    private void RefreshShortcutConflicts()
    {
        var conflicts = _conflictsProvider();
        if (conflicts.Count == 0)
        {
            ShortcutWarning.Visibility = Visibility.Collapsed;
            return;
        }

        var lines = conflicts.Select(record =>
            $"\"{ShortcutFormatter.Format(record)}\" ({NameOf(record.Action)}) could not be registered — another application may already use it.");

        ShortcutWarning.Text = string.Join(Environment.NewLine, lines);
        ShortcutWarning.Visibility = Visibility.Visible;
    }

    private void ShowShortcutWarning(string message)
    {
        ShortcutWarning.Text = message;
        ShortcutWarning.Visibility = Visibility.Visible;
        _capturingBox = null;
        Refresh();
    }

    private static DockShortcutAction ParseAction(string tag)
        => Enum.TryParse<DockShortcutAction>(tag, out var action) ? action : DockShortcutAction.OpenDock;

    private static string NameOf(DockShortcutAction action) => action switch
    {
        DockShortcutAction.NextItem => "Select next item",
        DockShortcutAction.PreviousItem => "Select previous item",
        _ => "Open dock",
    };

    private static uint ToWin32Modifiers(ModifierKeys modifiers)
    {
        uint result = 0;
        if ((modifiers & ModifierKeys.Control) != 0)
        {
            result |= ShortcutModifiers.Control;
        }

        if ((modifiers & ModifierKeys.Alt) != 0)
        {
            result |= ShortcutModifiers.Alt;
        }

        if ((modifiers & ModifierKeys.Shift) != 0)
        {
            result |= ShortcutModifiers.Shift;
        }

        if ((modifiers & ModifierKeys.Windows) != 0)
        {
            result |= ShortcutModifiers.Win;
        }

        return result;
    }

    // ----- general ------------------------------------------------------------------------------------

    private void BuildMonitorCombo()
    {
        MonitorCombo.Items.Add(new ComboBoxItem { Content = "Primary monitor", Tag = string.Empty });

        foreach (var monitor in _monitorsProvider())
        {
            MonitorCombo.Items.Add(new ComboBoxItem { Content = monitor.Description, Tag = monitor.DeviceName });
        }
    }

    private void SelectMonitor(string deviceName)
    {
        var index = 0;
        for (var i = 0; i < MonitorCombo.Items.Count; i++)
        {
            if (MonitorCombo.Items[i] is ComboBoxItem { Tag: string tag }
                && string.Equals(tag, deviceName, StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                break;
            }
        }

        MonitorCombo.SelectedIndex = index;
    }

    private void OnMonitorSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        if (MonitorCombo.SelectedItem is not ComboBoxItem { Tag: string deviceName })
        {
            return;
        }

        _settings.Update(settings => settings.MonitorName = deviceName);
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
