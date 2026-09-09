using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using DockManager.Core.Diagnostics;
using DockManager.Core.Shell;

namespace DockManager.App.Shell;

/// <summary>
/// Extracts and caches shell icons. Icons are frozen <see cref="BitmapSource"/> instances so they can
/// be shared across every render of an item without further cost.
/// </summary>
public sealed class IconProvider : IIconProvider
{
    private const uint FileAttributeNormal = 0x80;
    private const uint FileAttributeDirectory = 0x10;

    private readonly ConcurrentDictionary<string, object?> _cache = new(StringComparer.Ordinal);
    private readonly IDockLogger? _logger;

    public IconProvider(IDockLogger? logger = null)
    {
        _logger = logger;
    }

    public int CacheSize => _cache.Count;

    public object? GetIcon(string path, bool large)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var key = PathNormalizer.NormalizeKey(path) + (large ? "|large" : "|small");
        return _cache.GetOrAdd(key, _ => Load(path, large));
    }

    public void ClearCache() => _cache.Clear();

    private BitmapSource? Load(string path, bool large)
    {
        var isDirectory = false;
        var exists = false;

        try
        {
            isDirectory = Directory.Exists(path);
            exists = isDirectory || File.Exists(path);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            exists = false;
        }

        var flags = Win32.ShgfiIcon | (large ? Win32.ShgfiLargeIcon : Win32.ShgfiSmallIcon);
        var attributes = 0u;

        if (!exists)
        {
            // Missing targets still get the icon of their file type, so the dock can show them as
            // unavailable instead of showing nothing at all.
            flags |= Win32.ShgfiUseFileAttributes;
            attributes = isDirectory ? FileAttributeDirectory : FileAttributeNormal;
        }

        var info = new Win32.SHFILEINFO();
        var size = (uint)Marshal.SizeOf<Win32.SHFILEINFO>();

        try
        {
            var result = Win32.SHGetFileInfo(path, attributes, ref info, size, flags);
            if (result == IntPtr.Zero || info.hIcon == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                var bitmap = Imaging.CreateBitmapSourceFromHIcon(info.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                bitmap.Freeze();
                return bitmap;
            }
            finally
            {
                Win32.DestroyIcon(info.hIcon);
            }
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or COMException or NotSupportedException)
        {
            _logger?.Warn($"Could not read the icon of '{path}'.", ex);
            return null;
        }
    }
}
