using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DockManager.App.Ui;

/// <summary>Collapses elements bound to null or empty strings (e.g. the panel's subtitle line).</summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string { Length: > 0 } ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
