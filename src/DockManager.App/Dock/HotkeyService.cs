using DockManager.Core.Settings;
using DockManager.Core.Shortcuts;

namespace DockManager.App.Dock;

/// <summary>
/// Owns the dock's global shortcuts. Bindings are registered against the dock window; combinations
/// Windows reserves or another application already holds are never taken silently — they are
/// reported so the settings screen can warn the user.
/// </summary>
internal sealed class HotkeyService
{
    private const int BaseId = 0x444D; // "DM"

    private readonly IntPtr _hwnd;
    private readonly Dictionary<int, DockShortcutAction> _actionsById = [];
    private readonly List<int> _registeredIds = [];

    public HotkeyService(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    /// <summary>Raised on the UI thread when a registered shortcut fires.</summary>
    public event Action<DockShortcutAction>? Pressed;

    /// <summary>Shortcuts that could not be registered (reserved by Windows or held elsewhere).</summary>
    public IReadOnlyList<ShortcutRecord> Conflicts { get; private set; } = [];

    /// <summary>Replaces the whole binding set.</summary>
    public void Apply(IReadOnlyList<ShortcutRecord>? records)
    {
        UnregisterAll();

        var conflicts = new List<ShortcutRecord>();
        if (records is not null)
        {
            var nextId = BaseId;
            foreach (var record in records)
            {
                if (record.IsEmpty)
                {
                    continue;
                }

                if (ShortcutValidator.IsReservedBySystem(record.Modifiers, record.Key))
                {
                    conflicts.Add(record);
                    continue;
                }

                var id = nextId++;
                if (Win32.RegisterHotKey(_hwnd, id, record.Modifiers | Win32.ModNoRepeat, record.Key))
                {
                    _actionsById[id] = record.Action;
                    _registeredIds.Add(id);
                }
                else
                {
                    conflicts.Add(record);
                }
            }
        }

        Conflicts = conflicts;
    }

    /// <summary>Feeds a window message; returns true when it was a dock hotkey.</summary>
    public bool HandleMessage(uint message, IntPtr wParam)
    {
        if (message != Win32.WmHotKey)
        {
            return false;
        }

        if (_actionsById.TryGetValue(wParam.ToInt32(), out var action))
        {
            Pressed?.Invoke(action);
            return true;
        }

        return false;
    }

    public void UnregisterAll()
    {
        foreach (var id in _registeredIds)
        {
            Win32.UnregisterHotKey(_hwnd, id);
        }

        _registeredIds.Clear();
        _actionsById.Clear();
    }
}
