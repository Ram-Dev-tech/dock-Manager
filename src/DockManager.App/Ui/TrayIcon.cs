using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using DockManager.Core.Diagnostics;

namespace DockManager.App.Ui;

/// <summary>
/// A system tray icon with a small menu, implemented directly against <c>Shell_NotifyIcon</c> so the
/// dock does not need to load Windows Forms. A hidden message window receives the tray callbacks.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private const uint WmNull = 0x0000;
    private const uint WmCommand = 0x0111;
    private const uint WmTrayIcon = 0x8000 + 1;

    private const uint WmLeftButtonUp = 0x0202;
    private const uint WmLeftButtonDoubleClick = 0x0203;
    private const uint WmRightButtonUp = 0x0205;

    private const uint NimAdd = 0x00;
    private const uint NimModify = 0x01;
    private const uint NimDelete = 0x02;

    private const uint NifMessage = 0x0001;
    private const uint NifIcon = 0x0002;
    private const uint NifTip = 0x0004;
    private const uint NifShowTip = 0x0080;

    private const uint MenuItemString = 0x00000000;
    private const uint MenuSeparator = 0x00000800;

    private const uint TrackPopupMenuLeftAlign = 0x0000;
    private const uint TrackPopupMenuBottomAlign = 0x0020;

    private const int IdShowDock = 1001;
    private const int IdSettings = 1002;
    private const int IdQuit = 1003;

    private readonly IDockLogger? _logger;
    private HwndSource? _source;
    private IntPtr _icon = IntPtr.Zero;
    private bool _disposed;

    public TrayIcon(IDockLogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>Left click or double click on the tray icon: reveal the dock.</summary>
    public event EventHandler? ShowDockRequested;

    public event EventHandler? SettingsRequested;

    public event EventHandler? QuitRequested;

    public void Install()
    {
        if (_disposed || _source is not null)
        {
            return;
        }

        var parameters = new HwndSourceParameters("DockManagerTrayIcon")
        {
            PositionX = 0,
            PositionY = 0,
            Width = 1,
            Height = 1,
            WindowStyle = 0,
            ParentWindow = new IntPtr(-3), // HWND_MESSAGE: the window is never visible.
        };

        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);

        _icon = LoadIconFromEmbeddedResource();

        var data = CreateData();
        data.hWnd = _source.Handle;
        data.uCallbackMessage = WmTrayIcon;
        data.uFlags = NifMessage | NifIcon | NifTip | NifShowTip;
        data.hIcon = _icon;
        data.szTip = "Dock Manager";

        if (!ShellNotifyIcon(NimAdd, ref data))
        {
            _logger?.Warn("The tray icon could not be installed.");
        }
    }

    public void SetTip(string tip)
    {
        if (_source is null)
        {
            return;
        }

        var data = CreateData();
        data.hWnd = _source.Handle;
        data.uFlags = NifTip;
        data.szTip = tip;
        ShellNotifyIcon(NimModify, ref data);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_source is not null)
        {
            var data = CreateData();
            data.hWnd = _source.Handle;
            ShellNotifyIcon(NimDelete, ref data);
            _source.Dispose();
            _source = null;
        }

        if (_icon != IntPtr.Zero)
        {
            DestroyIcon(_icon);
            _icon = IntPtr.Zero;
        }
    }

    private static NOTIFYICONDATA CreateData() => new()
    {
        cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
    };

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch ((uint)msg)
        {
            case WmTrayIcon:
                switch ((int)(lParam.ToInt64() & 0xFFFF))
                {
                    case (int)WmLeftButtonUp:
                    case (int)WmLeftButtonDoubleClick:
                        ShowDockRequested?.Invoke(this, EventArgs.Empty);
                        break;

                    case (int)WmRightButtonUp:
                        ShowContextMenu();
                        break;
                }

                handled = true;
                break;

            case WmCommand:
                switch ((int)(wParam.ToInt64() & 0xFFFF))
                {
                    case IdShowDock:
                        ShowDockRequested?.Invoke(this, EventArgs.Empty);
                        break;

                    case IdSettings:
                        SettingsRequested?.Invoke(this, EventArgs.Empty);
                        break;

                    case IdQuit:
                        QuitRequested?.Invoke(this, EventArgs.Empty);
                        break;
                }

                handled = true;
                break;
        }

        return IntPtr.Zero;
    }

    private void ShowContextMenu()
    {
        if (_source is null)
        {
            return;
        }

        // Windows only routes the menu's selection message to the window that was foreground when
        // TrackPopupMenu was called, so take foreground first.
        SetForegroundWindow(_source.Handle);

        var menu = CreatePopupMenu();
        AppendMenu(menu, MenuItemString, IdShowDock, "Show dock");
        AppendMenu(menu, MenuItemString, IdSettings, "Settings…");
        AppendMenu(menu, MenuSeparator, 0, string.Empty);
        AppendMenu(menu, MenuItemString, IdQuit, "Quit Dock Manager");

        GetCursorPos(out var point);
        TrackPopupMenu(menu, TrackPopupMenuLeftAlign | TrackPopupMenuBottomAlign, point.X, point.Y, 0, _source.Handle, IntPtr.Zero);
        DestroyMenu(menu);
        PostMessage(_source.Handle, WmNull, IntPtr.Zero, IntPtr.Zero);
    }

    private IntPtr LoadIconFromEmbeddedResource()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/Assets/DockManager.ico", UriKind.Absolute);
            var stream = Application.GetResourceStream(uri);
            if (stream is null)
            {
                return IntPtr.Zero;
            }

            using var reader = new BinaryReader(stream.Stream);
            var bytes = reader.ReadBytes((int)stream.Stream.Length);
            var entry = ExtractIconImage(bytes);
            if (entry is null)
            {
                return IntPtr.Zero;
            }

            return CreateIconFromResourceEx(entry, (uint)entry.Length, true, 0x00030000, 0, 0, 0x8000);
        }
        catch (Exception ex) when (ex is IOException or UriFormatException or ApplicationException)
        {
            _logger?.Warn("Could not load the tray icon.", ex);
            return IntPtr.Zero;
        }
    }

    /// <summary>Returns the raw image of the smallest entry in an .ico file.</summary>
    private static byte[]? ExtractIconImage(byte[] file)
    {
        if (file.Length < 6)
        {
            return null;
        }

        var count = BitConverter.ToUInt16(file, 4);
        if (count == 0 || file.Length < 6 + (16 * count))
        {
            return null;
        }

        var best = -1;
        var bestSize = int.MaxValue;

        for (var i = 0; i < count; i++)
        {
            var offset = 6 + (16 * i);
            var width = file[offset] == 0 ? 256 : file[offset];
            var height = file[offset + 1] == 0 ? 256 : file[offset + 1];
            var size = Math.Max(width, height);

            if (size >= 16 && size < bestSize)
            {
                bestSize = size;
                best = i;
            }
        }

        if (best < 0)
        {
            return null;
        }

        var entryOffset = 6 + (16 * best);
        var imageOffset = BitConverter.ToInt32(file, entryOffset + 12);
        var imageSize = BitConverter.ToInt32(file, entryOffset + 8);
        if (imageOffset < 0 || imageSize <= 0 || imageOffset + imageSize > file.Length)
        {
            return null;
        }

        var image = new byte[imageSize];
        Array.Copy(file, imageOffset, image, 0, imageSize);
        return image;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uVersionOrTimeout;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    private static bool ShellNotifyIcon(uint message, ref NOTIFYICONDATA data)
    {
        try
        {
            return Shell_NotifyIcon(message, ref data);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            return false;
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, int uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    private static extern bool TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Win32.POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr CreateIconFromResourceEx(
        byte[] pbIconBits,
        uint cbIconBits,
        [MarshalAs(UnmanagedType.Bool)] bool fIcon,
        uint dwVersion,
        int cxDesired,
        int cyDesired,
        uint uFlags);
}
