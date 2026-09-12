using System.Globalization;
using System.Windows.Data;
using DockManager.Core.Shell;

namespace DockManager.App.Ui;

/// <summary>
/// Turns a dock item's <c>IconKey</c> (an executable, file or folder path) into a shell icon. The
/// provider is a singleton service assigned once at startup so the converter can be created by XAML.
/// </summary>
public sealed class IconConverter : IValueConverter
{
    public static IIconProvider? Provider { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var path = value as string;
        return string.IsNullOrWhiteSpace(path) ? null : Provider?.GetIcon(path, large: true);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
