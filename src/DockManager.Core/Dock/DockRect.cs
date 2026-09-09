namespace DockManager.Core.Dock;

/// <summary>
/// A framework independent rectangle in device independent pixels (DIPs).
/// The core library deliberately does not reference <c>System.Windows.Rect</c> so that it
/// stays unit testable off Windows.
/// </summary>
public readonly record struct DockRect(double X, double Y, double Width, double Height)
{
    public static DockRect Empty { get; } = new(0, 0, 0, 0);

    public double Left => X;
    public double Top => Y;
    public double Right => X + Width;
    public double Bottom => Y + Height;

    public bool IsEmpty => Width <= 0 || Height <= 0;

    public bool Contains(double x, double y)
        => x >= Left && x < Right && y >= Top && y < Bottom;

    public DockRect WithHeight(double height) => this with { Height = height };

    public DockRect WithWidth(double width) => this with { Width = width };

    public DockRect OffsetBy(double dx, double dy) => this with { X = X + dx, Y = Y + dy };

    /// <summary>Clamps this rectangle so that it fits inside <paramref name="bounds"/>.</summary>
    public DockRect ClampInside(DockRect bounds)
    {
        var width = Math.Min(Width, bounds.Width);
        var height = Math.Min(Height, bounds.Height);
        var x = Math.Max(bounds.Left, Math.Min(X, bounds.Right - width));
        var y = Math.Max(bounds.Top, Math.Min(Y, bounds.Bottom - height));
        return new DockRect(x, y, width, height);
    }

    public override string ToString()
        => FormattableString.Invariant($"{X:0.#},{Y:0.#} {Width:0.#}x{Height:0.#}");
}
