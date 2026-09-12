namespace DockManager.Core.Settings;

/// <summary>Actions a user can bind a global keyboard shortcut to.</summary>
public enum DockShortcutAction
{
    OpenDock = 0,
    NextItem = 1,
    PreviousItem = 2,
}

/// <summary>
/// One configurable global shortcut. Modifiers and key are stored as their Win32 values
/// (MOD_* and VK_*) because they are only ever used by RegisterHotKey; the user only ever sees
/// them formatted ("Ctrl + Shift + Space").
/// </summary>
public sealed class ShortcutRecord
{
    public DockShortcutAction Action { get; set; }

    public uint Modifiers { get; set; }

    public uint Key { get; set; }

    public ShortcutRecord Clone() => (ShortcutRecord)MemberwiseClone();

    /// <summary>A record with no key bound.</summary>
    public bool IsEmpty => Key == 0;

    public bool SameComboAs(ShortcutRecord? other)
        => other is not null && Modifiers == other.Modifiers && Key == other.Key;
}

/// <summary>Win32 MOD_* values, mirrored here so Core can reason about shortcuts without P/Invoke.</summary>
public static class ShortcutModifiers
{
    public const uint Alt = 0x0001;
    public const uint Control = 0x0002;
    public const uint Shift = 0x0004;
    public const uint Win = 0x0008;

    public const uint All = Alt | Control | Shift | Win;
}

/// <summary>The default bindings. Calm, unreserved combinations that never fight the shell.</summary>
public static class ShortcutDefaults
{
    /// <summary>Ctrl + Shift + Space.</summary>
    public const uint OpenDockKey = 0x20; // VK_SPACE

    /// <summary>Ctrl + Alt + Right.</summary>
    public const uint NextItemKey = 0x27; // VK_RIGHT

    /// <summary>Ctrl + Alt + Left.</summary>
    public const uint PreviousItemKey = 0x25; // VK_LEFT

    public static List<ShortcutRecord> Create() =>
    [
        new ShortcutRecord
        {
            Action = DockShortcutAction.OpenDock,
            Modifiers = ShortcutModifiers.Control | ShortcutModifiers.Shift,
            Key = OpenDockKey,
        },
        new ShortcutRecord
        {
            Action = DockShortcutAction.NextItem,
            Modifiers = ShortcutModifiers.Control | ShortcutModifiers.Alt,
            Key = NextItemKey,
        },
        new ShortcutRecord
        {
            Action = DockShortcutAction.PreviousItem,
            Modifiers = ShortcutModifiers.Control | ShortcutModifiers.Alt,
            Key = PreviousItemKey,
        },
    ];
}
