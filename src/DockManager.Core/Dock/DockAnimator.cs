namespace DockManager.Core.Dock;

/// <summary>
/// Time driven interpolation for the dock slide. The renderer only has to call
/// <see cref="Tick"/> and read <see cref="Progress"/>, which keeps the animation itself unit
/// testable and independent of any UI framework.
/// </summary>
public sealed class DockAnimator
{
    private double _from;
    private double _to;
    private long _startedAt;
    private int _durationMs;

    public DockAnimator()
    {
        Progress = 0d;
    }

    public double Progress { get; private set; }

    public bool IsRunning { get; private set; }

    /// <summary>Raised on every tick with the new progress.</summary>
    public event Action<double>? Frame;

    /// <summary>Raised once when the animation reaches its target.</summary>
    public event Action? Completed;

    public void Start(double from, double to, int durationMs, long nowMs)
    {
        _from = from;
        _to = to;
        _durationMs = Math.Max(0, durationMs);
        _startedAt = nowMs;
        Progress = from;
        IsRunning = true;

        if (_durationMs == 0)
        {
            Finish();
        }
    }

    /// <summary>Jumps straight to a value, cancelling any running animation.</summary>
    public void SnapTo(double value)
    {
        IsRunning = false;
        Progress = Math.Clamp(value, 0d, 1d);
        Frame?.Invoke(Progress);
    }

    public void Cancel()
    {
        IsRunning = false;
    }

    /// <summary>Advances the animation. Returns true while it is still running.</summary>
    public bool Tick(long nowMs)
    {
        if (!IsRunning)
        {
            return false;
        }

        var elapsed = nowMs - _startedAt;
        if (elapsed >= _durationMs)
        {
            Finish();
            return false;
        }

        var t = _durationMs == 0 ? 1d : (double)elapsed / _durationMs;
        Progress = _from + ((_to - _from) * EaseOutCubic(t));
        Frame?.Invoke(Progress);
        return true;
    }

    private void Finish()
    {
        IsRunning = false;
        Progress = Math.Clamp(_to, 0d, 1d);
        Frame?.Invoke(Progress);
        Completed?.Invoke();
    }

    /// <summary>Cubic ease out: fast at the start, settled at the end. Keeps the dock feeling instant.</summary>
    public static double EaseOutCubic(double t)
    {
        var clamped = Math.Clamp(t, 0d, 1d);
        var inv = 1d - clamped;
        return 1d - (inv * inv * inv);
    }
}
