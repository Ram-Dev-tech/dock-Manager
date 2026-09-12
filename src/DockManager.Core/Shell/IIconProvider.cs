namespace DockManager.Core.Shell;

/// <summary>Provides an icon for a pinned item, or <c>null</c> when there is none.</summary>
/// <remarks>
/// Returns <see cref="object"/> so the core does not depend on a UI framework; the WPF layer stores
/// frozen <c>BitmapSource</c> instances here.
/// </remarks>
public interface IIconProvider
{
    object? GetIcon(string path, bool large);
}
