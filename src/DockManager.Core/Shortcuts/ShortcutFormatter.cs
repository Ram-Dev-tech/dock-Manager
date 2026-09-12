using DockManager.Core.Settings;

namespace DockManager.Core.Shortcuts;

/// <summary>
/// Turns a stored shortcut into the text a user should read ("Ctrl + Shift + Space") and knows
/// whether a combination may be captured at all. Pure logic so it is unit tested on any platform.
/// </summary>
public static class ShortcutFormatter
{
    public static string Format(ShortcutRecord? record)
    {
        if (record is null || record.IsEmpty)
        {
            return "None";
        }

        var parts = new List<string>(4);
        if ((record.Modifiers & ShortcutModifiers.Control) != 0)
        {
            parts.Add("Ctrl");
        }

        if ((record.Modifiers & ShortcutModifiers.Alt) != 0)
        {
            parts.Add("Alt");
        }

        if ((record.Modifiers & ShortcutModifiers.Shift) != 0)
        {
            parts.Add("Shift");
        }

        if ((record.Modifiers & ShortcutModifiers.Win) != 0)
        {
            parts.Add("Win");
        }

        parts.Add(FormatKey(record.Key));
        return string.Join(" + ", parts);
    }

    /// <summary>Human readable key name for the common virtual keys the dock accepts.</summary>
    public static string FormatKey(uint virtualKey) => virtualKey switch
    {
        0x08 => "Backspace",
        0x09 => "Tab",
        0x0D => "Enter",
        0x13 => "Pause",
        0x14 => "Caps Lock",
        0x1B => "Esc",
        0x20 => "Space",
        0x21 => "Page Up",
        0x22 => "Page Down",
        0x23 => "End",
        0x24 => "Home",
        0x25 => "Left",
        0x26 => "Up",
        0x27 => "Right",
        0x28 => "Down",
        0x2D => "Insert",
        0x2E => "Delete",
        >= 0x30 and <= 0x39 => ((char)virtualKey).ToString(),
        >= 0x41 and <= 0x5A => ((char)virtualKey).ToString(),
        >= 0x70 and <= 0x87 => $"F{virtualKey - 0x6F}",
        0x90 => "Num Lock",
        0x91 => "Scroll Lock",
        0xBA => ";",
        0xBB => "=",
        0xBC => ",",
        0xBD => "-",
        0xBE => ".",
        0xBF => "/",
        0xC0 => "`",
        0xDB => "[",
        0xDC => "\\",
        0xDD => "]",
        0xDE => "'",
        _ => $"Key 0x{virtualKey:X2}",
    };

    /// <summary>
    /// Whether a pressed combination may be captured as a dock shortcut. Rejected: no modifier at
    /// all (that would swallow normal typing), and plain modifier keys with no real key.
    /// </summary>
    public static bool IsCapturable(uint modifiers, uint key)
        => (modifiers & ShortcutModifiers.All) != 0 && !IsModifierKey(key);

    private static bool IsModifierKey(uint key)
        => key is 0x10 or 0x11 or 0x12 // Shift, Ctrl, Alt
            or 0x5B or 0x5C           // Left/Right Win
            or 0xA0 or 0xA1           // Left/Right Shift
            or 0xA2 or 0xA3           // Left/Right Ctrl
            or 0xA4 or 0xA5;          // Left/Right Alt
}
