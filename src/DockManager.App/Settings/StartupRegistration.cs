using System.Runtime.InteropServices;
using System.Text;
using DockManager.Core.Diagnostics;
using DockManager.Core.Settings;

namespace DockManager.App.Settings;

/// <summary>
/// Registers the dock under <c>HKCU\...\Run</c> so it starts with Windows. The registry is called
/// directly through advapi32 to avoid taking a dependency on a Windows only assembly.
/// </summary>
public sealed class StartupRegistration : IStartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DockManager";

    private const uint Hkcu = 0x80000001;
    private const uint KeyRead = 0x20019;
    private const uint KeyWrite = 0x20006;
    private const uint RegSz = 1;

    private readonly string _command;
    private readonly IDockLogger? _logger;

    public StartupRegistration(string? executablePath = null, IDockLogger? logger = null)
    {
        _logger = logger;
        var path = string.IsNullOrWhiteSpace(executablePath) ? Environment.ProcessPath : executablePath;
        _command = string.IsNullOrWhiteSpace(path) ? string.Empty : $"\"{path}\"";
    }

    public bool IsEnabled => ReadValue() is not null;

    public bool IsCurrent
    {
        get
        {
            var value = ReadValue();
            return value is not null && string.Equals(value, _command, StringComparison.OrdinalIgnoreCase);
        }
    }

    public void Enable()
    {
        if (_command.Length == 0)
        {
            _logger?.Warn("Startup registration skipped: the executable path is unknown.");
            return;
        }

        WriteValue(_command);
    }

    public void Disable() => DeleteValue();

    private string? ReadValue()
    {
        if (RegOpenKeyEx(Hkcu, RunKeyPath, 0, KeyRead, out var key) != 0)
        {
            return null;
        }

        try
        {
            var size = 0u;
            if (RegQueryValueEx(key, ValueName, IntPtr.Zero, out _, IntPtr.Zero, ref size) != 0)
            {
                return null;
            }

            var buffer = Marshal.AllocHGlobal((int)Math.Max(size, 2));
            try
            {
                if (RegQueryValueEx(key, ValueName, IntPtr.Zero, out _, buffer, ref size) != 0)
                {
                    return null;
                }

                return Marshal.PtrToStringUni(buffer);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        finally
        {
            RegCloseKey(key);
        }
    }

    private void WriteValue(string value)
    {
        if (RegOpenKeyEx(Hkcu, RunKeyPath, 0, KeyWrite, out var key) != 0)
        {
            _logger?.Warn("Could not open the Run key; startup registration was not changed.");
            return;
        }

        try
        {
            var bytes = Encoding.Unicode.GetBytes(value + "\0");
            var pointer = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, pointer, bytes.Length);
                if (RegSetValueEx(key, ValueName, 0, RegSz, pointer, (uint)bytes.Length) != 0)
                {
                    _logger?.Warn("Could not write the Run value.");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pointer);
            }
        }
        finally
        {
            RegCloseKey(key);
        }
    }

    private void DeleteValue()
    {
        if (RegOpenKeyEx(Hkcu, RunKeyPath, 0, KeyWrite, out var key) != 0)
        {
            return;
        }

        try
        {
            RegDeleteValue(key, ValueName);
        }
        finally
        {
            RegCloseKey(key);
        }
    }

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegOpenKeyEx(uint hKey, string lpSubKey, uint ulOptions, uint samDesired, out IntPtr phkResult);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegQueryValueEx(
        IntPtr hKey,
        string lpValueName,
        IntPtr lpReserved,
        out uint lpType,
        IntPtr lpData,
        ref uint lpcbData);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegSetValueEx(IntPtr hKey, string lpValueName, uint reserved, uint dwType, IntPtr lpData, uint cbData);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegDeleteValue(IntPtr hKey, string lpValueName);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern int RegCloseKey(IntPtr hKey);
}
