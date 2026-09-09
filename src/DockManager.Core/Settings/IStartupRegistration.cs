namespace DockManager.Core.Settings;

/// <summary>Registers or removes the "start with Windows" entry.</summary>
public interface IStartupRegistration
{
    bool IsEnabled { get; }

    /// <summary>Whether the stored registration still points at the running executable.</summary>
    bool IsCurrent { get; }

    void Enable();

    void Disable();
}
