using DockManager.Core.Windows;

namespace DockManager.Core.Integrations;

/// <summary>
/// The contract every application integration implements. The dock asks an integration for the
/// things an application currently has open (tabs, documents, folders, windows) and how to switch
/// to one of them. Integrations may use any technology but must fail gracefully: when content
/// cannot be produced the dock falls back to the generic window level behaviour.
/// </summary>
public interface IApplicationIntegration
{
    /// <summary>Stable identifier, used by the settings screen to enable/disable the integration.</summary>
    string Id { get; }

    string DisplayName { get; }

    /// <summary>Lower cased executable file names this integration applies to, e.g. "chrome.exe".</summary>
    IReadOnlyCollection<string> ExecutableNames { get; }

    /// <summary>True when the integration can navigate below the window level (tabs, documents…).</summary>
    bool SupportsItemLevelNavigation { get; }

    /// <summary>
    /// Produces the currently open entries. Must be safe to call on a background thread and must
    /// never throw: on failure return the best available fallback (e.g. the window list).
    /// </summary>
    Task<IReadOnlyList<AppContentItem>> GetContentAsync(
        RunningApp app,
        IntegrationOptions options,
        CancellationToken cancellationToken);

    /// <summary>
    /// Switches to the entry. Returns false when the integration could not do it so the caller can
    /// fall back to plain window activation.
    /// </summary>
    Task<bool> ActivateItemAsync(AppContentItem item, CancellationToken cancellationToken);

    /// <summary>
    /// The extra context menu entries this integration can run reliably for the given application
    /// (e.g. "New window"). Empty for applications without known safe actions.
    /// </summary>
    IReadOnlyList<QuickAction> GetQuickActions(RunningApp app);
}
