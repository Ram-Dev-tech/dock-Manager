using System.Runtime.InteropServices;
using System.Text;
using DockManager.Core.Diagnostics;
using DockManager.Core.Shell;

namespace DockManager.App.Shell;

/// <summary>
/// Reads the target of a <c>.lnk</c> shortcut through <c>IShellLink</c> so a pinned shortcut can be
/// matched against the executable that is actually running.
/// </summary>
public sealed class ShellLinkResolver : IShortcutResolver
{
    private const uint SlrNoUi = 0x0001;
    private const uint SlrNoSearch = 0x0010;
    private const uint SlrNoTrack = 0x0020;

    private readonly IDockLogger? _logger;

    public ShellLinkResolver(IDockLogger? logger = null)
    {
        _logger = logger;
    }

    public bool TryResolve(string shortcutPath, out ShortcutTarget? target)
    {
        target = null;

        if (string.IsNullOrWhiteSpace(shortcutPath))
        {
            return false;
        }

        if (ComLink is null)
        {
            return false;
        }

        var link = (IShellLinkW?)Activator.CreateInstance(ComLink);
        if (link is null)
        {
            return false;
        }

        try
        {
            var persist = (IPersistFile)link;
            persist.Load(shortcutPath, 0 /* STGM_READ */);
            link.Resolve(IntPtr.Zero, SlrNoUi | SlrNoSearch | SlrNoTrack);

            var path = new StringBuilder(260);
            link.GetPath(path, path.Capacity, IntPtr.Zero, 0);

            var resolved = path.ToString();
            if (string.IsNullOrWhiteSpace(resolved))
            {
                return false;
            }

            var arguments = new StringBuilder(1024);
            link.GetArguments(arguments, arguments.Capacity);

            var workingDirectory = new StringBuilder(260);
            link.GetWorkingDirectory(workingDirectory, workingDirectory.Capacity);

            var description = new StringBuilder(260);
            link.GetDescription(description, description.Capacity);

            target = new ShortcutTarget(resolved)
            {
                Arguments = NullIfEmpty(arguments.ToString()),
                WorkingDirectory = NullIfEmpty(workingDirectory.ToString()),
                Description = NullIfEmpty(description.ToString()),
            };

            return true;
        }
        catch (COMException ex)
        {
            _logger?.Warn($"Could not resolve shortcut '{shortcutPath}'.", ex);
            return false;
        }
        finally
        {
            Marshal.ReleaseComObject(link);
        }
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static Type? ComLink { get; } = Type.GetTypeFromCLSID(new Guid("00021401-0000-0000-C000-000000000046"));

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);

        void GetIDList(out IntPtr ppidl);

        void SetIDList(IntPtr pidl);

        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);

        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);

        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);

        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

        void GetHotkey(out short pwHotkey);

        void SetHotkey(short wHotkey);

        void GetShowCmd(out int piShowCmd);

        void SetShowCmd(int iShowCmd);

        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);

        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);

        void Resolve(IntPtr hwnd, uint fFlags);

        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0000010B-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassId);

        [PreserveSig]
        int IsDirty();

        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);

        void Save([MarshalAs(UnmanagedType.LPWStr)] string? pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);

        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string? pszFileName);

        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }
}
