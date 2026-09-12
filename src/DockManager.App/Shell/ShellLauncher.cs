using System.ComponentModel;
using System.Diagnostics;
using DockManager.Core.Diagnostics;
using DockManager.Core.Shell;

namespace DockManager.App.Shell;

/// <summary>
/// Opens apps, files and folders through the shell. <c>UseShellExecute</c> is what makes files open
/// in their associated application, shortcuts behave like the Start menu and folders open in
/// Explorer.
/// </summary>
public sealed class ShellLauncher : IShellLauncher
{
    private readonly IDockLogger? _logger;

    public ShellLauncher(IDockLogger? logger = null)
    {
        _logger = logger;
    }

    public LaunchResult Launch(LaunchRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.FilePath))
        {
            return LaunchResult.Failed("There is nothing to open.");
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = request.FilePath,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal,
            };

            if (!string.IsNullOrWhiteSpace(request.Arguments))
            {
                startInfo.Arguments = request.Arguments;
            }

            if (!string.IsNullOrWhiteSpace(request.WorkingDirectory))
            {
                startInfo.WorkingDirectory = request.WorkingDirectory;
            }

            using var process = Process.Start(startInfo);
            return process is not null
                ? LaunchResult.Ok
                : LaunchResult.Failed("Windows did not start the application.");
        }
        catch (Win32Exception ex)
        {
            _logger?.Error($"Could not open '{request.FilePath}'.", ex);
            return LaunchResult.Failed(ex.Message);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
        {
            _logger?.Error($"Could not open '{request.FilePath}'.", ex);
            return LaunchResult.Failed(ex.Message);
        }
    }

    public LaunchResult OpenContainingFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return LaunchResult.Failed("There is nothing to show.");
        }

        return Launch(new LaunchRequest("explorer.exe", LaunchVerb.Explore)
        {
            Arguments = $"/select,\"{path}\"",
        });
    }
}
