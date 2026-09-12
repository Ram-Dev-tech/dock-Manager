namespace DockManager.Core.Settings;

/// <summary>Colour scheme of the dock. The default follows Windows.</summary>
public enum DockTheme
{
    System = 0,
    Light = 1,
    Dark = 2,
}

/// <summary>
/// How eagerly moving the cursor to the screen edge reveals the dock. Expressed as user facing
/// wording only; the pixel values are an implementation detail.
/// </summary>
public enum EdgeSensitivity
{
    LessSensitive = 0,
    Normal = 1,
    Sensitive = 2,
}

public static class EdgeSensitivityExtensions
{
    /// <summary>Maps the friendly choice onto the width of the invisible activation strip.</summary>
    public static int ActivationPixels(this EdgeSensitivity sensitivity) => sensitivity switch
    {
        EdgeSensitivity.LessSensitive => 1,
        EdgeSensitivity.Sensitive => 4,
        _ => 2,
    };

    public static EdgeSensitivity Nearest(int activationPixels) => activationPixels switch
    {
        <= 1 => EdgeSensitivity.LessSensitive,
        >= 4 => EdgeSensitivity.Sensitive,
        _ => EdgeSensitivity.Normal,
    };
}
