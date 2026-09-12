using DockManager.Core.Shell;

namespace DockManager.Core.Integrations;

/// <summary>
/// One entry in an item's context menu that the dock knows how to run reliably, e.g. "New window"
/// for a browser. Only actions that can be implemented with plain shell launches belong here.
/// </summary>
public sealed record QuickAction(string Label, LaunchRequest Request);
