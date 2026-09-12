using DockManager.Core.Settings;

namespace DockManager.Core.Shortcuts;

/// <summary>
/// Guards against hijacking combinations the shell and the user depend on. Registration failures
/// still surface as warnings; this list only prevents the dock from even trying to take a
/// combination that belongs to Windows.
/// </summary>
public static class ShortcutValidator
{
    /// <summary>
    /// True when the combination is reserved by Windows or is a well known system shortcut that
    /// must never be silently overridden (Win key combos, Alt+Tab, Alt+Esc, Ctrl+Esc, Ctrl+Alt+Del).
    /// </summary>
    public static bool IsReservedBySystem(uint modifiers, uint key)
    {
        // Any Win key combination belongs to the shell (Win+L, Win+D, Win+E, snap layouts, …).
        if ((modifiers & ShortcutModifiers.Win) != 0)
        {
            return true;
        }

        var mods = modifiers & ShortcutModifiers.All;

        return (mods, key) switch
        {
            // Alt+Tab / Alt+Shift+Tab / Alt+Esc: window switching.
            (ShortcutModifiers.Alt, 0x09) => true,
            (ShortcutModifiers.Alt | ShortcutModifiers.Shift, 0x09) => true,
            (ShortcutModifiers.Alt, 0x1B) => true,

            // Ctrl+Esc: Start menu.
            (ShortcutModifiers.Control, 0x1B) => true,

            // Ctrl+Shift+Esc: Task Manager.
            (ShortcutModifiers.Control | ShortcutModifiers.Shift, 0x1B) => true,

            // Ctrl+Alt+Del is handled by the secure desktop and cannot be registered anyway.
            (ShortcutModifiers.Control | ShortcutModifiers.Alt, 0x2E) => true,

            // F1: system help.
            (0u, 0x70) => true,

            _ => false,
        };
    }

    /// <summary>Why a combination was refused, in user facing wording.</summary>
    public static string? RejectionReason(uint modifiers, uint key)
    {
        if (!ShortcutFormatter.IsCapturable(modifiers, key))
        {
            return "Use at least one modifier key (Ctrl, Alt, Shift or Win) together with another key.";
        }

        return IsReservedBySystem(modifiers, key)
            ? "This shortcut belongs to Windows and cannot be used."
            : null;
    }
}
