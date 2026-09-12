namespace DockManager.Core.Integrations;

/// <summary>
/// The settings and context an integration may honour when producing its content. Passed per call
/// so integrations stay stateless and cheap to test.
/// </summary>
public sealed record IntegrationOptions(
    bool GroupMultipleWindows,
    bool ShowWindowPreviews,
    IntPtr ForegroundWindow)
{
    public static IntegrationOptions Default { get; } = new(false, true, IntPtr.Zero);
}
