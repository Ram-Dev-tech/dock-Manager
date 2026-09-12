using DockManager.Core.Integrations;

namespace DockManager.Core.Tests.Support;

public sealed class FakeWindowActivator : IWindowActivator
{
    private readonly HashSet<IntPtr> _fail = [];

    public List<IntPtr> Activated { get; } = [];

    public bool Result { get; set; } = true;

    public void FailFor(IntPtr handle) => _fail.Add(handle);

    public bool ActivateWindow(IntPtr handle)
    {
        Activated.Add(handle);
        return Result && !_fail.Contains(handle);
    }
}
