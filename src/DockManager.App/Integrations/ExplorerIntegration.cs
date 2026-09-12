using System.Runtime.InteropServices;
using Microsoft.CSharp.RuntimeBinder;
using DockManager.Core.Diagnostics;
using DockManager.Core.Integrations;
using DockManager.Core.Shell;
using DockManager.Core.Windows;

namespace DockManager.App.Integrations;

/// <summary>
/// File Explorer integration. The open folder of each Explorer window is resolved through the shell
/// (<c>IShellWindows</c>), so entries keep their real location instead of the sometimes vague window
/// title. When the shell cannot be queried the window list is used as the fallback.
/// </summary>
public sealed class ExplorerIntegration : IApplicationIntegration
{
    private static readonly Guid ShellWindowsGuid = new("9BA05972-F6A8-11CF-A442-00A0C90A8F39");

    private readonly IntegrationWorker _worker;
    private readonly IWindowActivator _activator;
    private readonly IDockLogger? _logger;

    public ExplorerIntegration(IntegrationWorker worker, IWindowActivator activator, IDockLogger? logger = null)
    {
        _worker = worker ?? throw new ArgumentNullException(nameof(worker));
        _activator = activator ?? throw new ArgumentNullException(nameof(activator));
        _logger = logger;
    }

    public string Id => "explorer";

    public string DisplayName => "File Explorer";

    public IReadOnlyCollection<string> ExecutableNames => ["explorer.exe"];

    public bool SupportsItemLevelNavigation => true;

    public async Task<IReadOnlyList<AppContentItem>> GetContentAsync(
        RunningApp app,
        IntegrationOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(app);

        try
        {
            return await _worker.RunAsync(_ => ReadContent(app, options), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ObjectDisposedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.Warn("Explorer integration failed; falling back to windows.", ex);
            return FallbackWindows(app, options);
        }
    }

    public Task<bool> ActivateItemAsync(AppContentItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        return Task.FromResult(item.WindowHandle != IntPtr.Zero && _activator.ActivateWindow(item.WindowHandle));
    }

    private IReadOnlyList<AppContentItem> ReadContent(RunningApp app, IntegrationOptions options)
    {
        var folders = ReadShellFolders();
        var items = new List<AppContentItem>(app.Windows.Count);

        foreach (var window in app.Windows)
        {
            if (folders.TryGetValue(window.Handle, out var folder))
            {
                items.Add(new AppContentItem(
                    $"{app.Key}:{window.Handle}",
                    folder.Name,
                    AppContentKind.Folder,
                    window.Handle)
                {
                    Subtitle = folder.Path,
                    IsActive = window.Handle == options.ForegroundWindow,
                    Locator = folder.Path,
                    IconKey = folder.Path,
                });
            }
            else
            {
                items.Add(WindowItem(app, window, options));
            }
        }

        return items;
    }

    private static AppContentItem WindowItem(RunningApp app, WindowInfo window, IntegrationOptions options)
        => new(
            $"{app.Key}:{window.Handle}",
            string.IsNullOrWhiteSpace(window.Title) ? app.DisplayName : window.Title,
            AppContentKind.Window,
            window.Handle)
        {
            Subtitle = window.IsMinimized ? "Minimized" : null,
            IsActive = window.Handle == options.ForegroundWindow,
        };

    private static IReadOnlyList<AppContentItem> FallbackWindows(RunningApp app, IntegrationOptions options)
        => app.Windows.Select(window => WindowItem(app, window, options)).ToList();

    private sealed record ShellFolder(string Name, string Path);

    /// <summary>Maps Explorer window handles to the folder they currently show.</summary>
    private static Dictionary<IntPtr, ShellFolder> ReadShellFolders()
    {
        var map = new Dictionary<IntPtr, ShellFolder>();

        try
        {
            var type = Type.GetTypeFromCLSID(ShellWindowsGuid);
            if (type is null)
            {
                return map;
            }

            var shell = Activator.CreateInstance(type);
            if (shell is null)
            {
                return map;
            }

            try
            {
                if (shell is not System.Collections.IEnumerable enumerable)
                {
                    return map;
                }

                foreach (object? entry in enumerable)
                {
                    if (entry is null)
                    {
                        continue;
                    }

                    try
                    {
                        ReadEntry(entry, map);
                    }
                    catch (COMException)
                    {
                        // A shell window disappeared mid enumeration; skip it.
                    }
                    catch (InvalidCastException)
                    {
                        // Not an Explorer window (e.g. an old Internet Explorer host).
                    }
                    catch (RuntimeBinderException)
                    {
                        // The window exposes no folder document.
                    }
                    finally
                    {
                        Marshal.FinalReleaseComObject(entry);
                    }
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(shell);
            }
        }
        catch (Exception)
        {
            // Shell unavailable; the caller falls back to window titles.
        }

        return map;
    }

    private static void ReadEntry(object entry, Dictionary<IntPtr, ShellFolder> map)
    {
        dynamic window = entry;

        var handle = (IntPtr)window.HWND;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        object? document = window.Document;
        if (document is null)
        {
            return;
        }

        try
        {
            dynamic folder = ((dynamic)document).Folder;
            if (folder is null)
            {
                return;
            }

            string path = (string)((dynamic)folder).Self.Path;
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var name = PathNormalizer.FileName(path.TrimEnd('\\'));
            if (name.Length == 0)
            {
                name = path;
            }

            map.TryAdd(handle, new ShellFolder(name, path));
        }
        finally
        {
            if (document is not null)
            {
                Marshal.FinalReleaseComObject(document);
            }
        }
    }
}
