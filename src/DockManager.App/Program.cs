using System.Threading;

namespace DockManager.App;

/// <summary>
/// Entry point. Enforces a single instance per session with a named mutex so launching the dock a
/// second time simply focuses the existing one instead of stacking icons.
/// </summary>
public static class Program
{
    private const string MutexName = @"Local\DockManager.SingleInstance";

    [STAThread]
    private static int Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, name: MutexName, out var createdNew);
        if (!createdNew)
        {
            // Another instance is already running.
            return 0;
        }

        var app = new App();
        app.InitializeComponent();
        return app.Run();
    }
}
