using System.Runtime.InteropServices;
using System.Text;

namespace DockManager.App.Windows;

/// <summary>
/// Resolves the executable image of a process. <c>QueryFullProcessImageName</c> is used instead of
/// <c>Process.MainModule</c> because it works for 64 bit callers inspecting 32 bit processes and for
/// processes we do not have full access to.
/// </summary>
public sealed class ProcessResolver
{
    private const int CacheLimit = 1024;

    private readonly Dictionary<uint, string> _cache = new();
    private readonly object _gate = new();

    public string GetExecutablePath(uint processId)
    {
        if (processId == 0)
        {
            return string.Empty;
        }

        lock (_gate)
        {
            if (_cache.TryGetValue(processId, out var cached))
            {
                return cached;
            }
        }

        var path = QueryImageName(processId);

        lock (_gate)
        {
            if (_cache.Count >= CacheLimit)
            {
                _cache.Clear();
            }

            _cache[processId] = path;
        }

        return path;
    }

    public void ClearCache()
    {
        lock (_gate)
        {
            _cache.Clear();
        }
    }

    private static string QueryImageName(uint processId)
    {
        var handle = Win32.OpenProcess(Win32.ProcessQueryLimitedInformation, false, processId);
        if (handle == IntPtr.Zero)
        {
            return string.Empty;
        }

        try
        {
            var builder = new StringBuilder(1024);
            var size = (uint)builder.Capacity;
            return Win32.QueryFullProcessImageName(handle, 0, builder, ref size)
                ? builder.ToString(0, (int)size)
                : string.Empty;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or ArgumentException)
        {
            return string.Empty;
        }
        finally
        {
            Win32.CloseHandle(handle);
        }
    }
}
