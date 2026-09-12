using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using DockManager.App.Dock;
using DockManager.Core.Diagnostics;

namespace DockManager.App.Integrations;

/// <summary>
/// Captures a small one-shot preview of a window with <c>PrintWindow</c>. A single capture per hover
/// keeps this cheap; there is deliberately no live updating. Any failure simply yields no preview.
/// </summary>
public sealed class WindowPreviewService
{
    private const int MaxDimension = 4096;

    private readonly IDockLogger? _logger;

    public WindowPreviewService(IDockLogger? logger = null)
    {
        _logger = logger;
    }

    public Task<BitmapSource?> CaptureAsync(IntPtr hwnd, CancellationToken cancellationToken)
    {
        if (hwnd == IntPtr.Zero)
        {
            return Task.FromResult<BitmapSource?>(null);
        }

        return Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Capture(hwnd);
            },
            cancellationToken);
    }

    private BitmapSource? Capture(IntPtr hwnd)
    {
        try
        {
            if (!Win32.IsWindow(hwnd) || Win32.IsIconic(hwnd))
            {
                return null;
            }

            Win32.GetWindowRect(hwnd, out var rect);
            var width = rect.Width;
            var height = rect.Height;
            if (width <= 0 || height <= 0 || width > MaxDimension || height > MaxDimension)
            {
                return null;
            }

            var windowDc = Win32.GetDC(hwnd);
            if (windowDc == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                var memoryDc = Win32.CreateCompatibleDC(windowDc);
                if (memoryDc == IntPtr.Zero)
                {
                    return null;
                }

                try
                {
                    var bitmap = Win32.CreateCompatibleBitmap(windowDc, width, height);
                    if (bitmap == IntPtr.Zero)
                    {
                        return null;
                    }

                    try
                    {
                        var previous = Win32.SelectObject(memoryDc, bitmap);

                        var printed = Win32.PrintWindow(hwnd, memoryDc, Win32.PwRenderFullContent)
                            || Win32.PrintWindow(hwnd, memoryDc, 0);

                        if (previous != IntPtr.Zero)
                        {
                            Win32.SelectObject(memoryDc, previous);
                        }

                        if (!printed)
                        {
                            return null;
                        }

                        var source = Imaging.CreateBitmapSourceFromHBitmap(
                            bitmap,
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        source.Freeze();
                        return source;
                    }
                    finally
                    {
                        Win32.DeleteObject(bitmap);
                    }
                }
                finally
                {
                    Win32.DeleteDC(memoryDc);
                }
            }
            finally
            {
                Win32.ReleaseDC(hwnd, windowDc);
            }
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or NotSupportedException)
        {
            _logger?.Warn("Window preview capture failed.", ex);
            return null;
        }
    }
}
