using DockManager.Core.Shell;
using DockManager.Core.Windows;

namespace DockManager.Core.Integrations;

/// <summary>
/// Owns the registered integrations and picks the right one for a running application. Unknown or
/// disabled integrations fall back to <see cref="GenericIntegration"/>, so the manager itself can
/// never fail.
/// </summary>
public sealed class ApplicationManager
{
    private readonly List<IApplicationIntegration> _integrations = [];
    private readonly GenericIntegration _generic;

    public ApplicationManager(GenericIntegration generic)
    {
        _generic = generic ?? throw new ArgumentNullException(nameof(generic));
    }

    public GenericIntegration Generic => _generic;

    public IReadOnlyList<IApplicationIntegration> Registered => _integrations;

    public void Register(IApplicationIntegration integration)
    {
        ArgumentNullException.ThrowIfNull(integration);

        if (_integrations.Any(existing => string.Equals(existing.Id, integration.Id, StringComparison.Ordinal)))
        {
            return;
        }

        _integrations.Add(integration);
    }

    /// <summary>
    /// Resolves the integration for <paramref name="app"/>. <paramref name="disabled"/> holds the
    /// ids the user switched off in settings.
    /// </summary>
    public IApplicationIntegration Resolve(RunningApp app, IReadOnlySet<string>? disabled = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        var executableName = PathNormalizer.FileName(app.ExecutablePath).ToLowerInvariant();
        if (executableName.Length > 0)
        {
            foreach (var integration in _integrations)
            {
                if (disabled is not null && disabled.Contains(integration.Id))
                {
                    continue;
                }

                if (integration.ExecutableNames.Contains(executableName))
                {
                    return integration;
                }
            }
        }

        return _generic;
    }
}
