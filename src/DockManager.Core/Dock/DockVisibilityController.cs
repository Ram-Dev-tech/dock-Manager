namespace DockManager.Core.Dock;

/// <summary>Timing configuration for the reveal/hide behaviour.</summary>
public sealed record DockVisibilityOptions
{
    /// <summary>Dwell time at the edge before the dock starts to appear. 0 keeps it instant.</summary>
    public int RevealDelayMs { get; init; } = 0;

    /// <summary>Grace period after the cursor leaves before the dock starts to hide.</summary>
    public int HideDelayMs { get; init; } = 380;

    /// <summary>Length of the slide in animation.</summary>
    public int RevealDurationMs { get; init; } = 140;

    /// <summary>Length of the slide out animation.</summary>
    public int HideDurationMs { get; init; } = 170;

    /// <summary>When false the dock never hides on its own.</summary>
    public bool AutoHide { get; init; } = true;

    public static DockVisibilityOptions Default { get; } = new();

    public DockVisibilityOptions Sanitized() => this with
    {
        RevealDelayMs = Math.Clamp(RevealDelayMs, 0, 2000),
        HideDelayMs = Math.Clamp(HideDelayMs, 0, 10000),
        RevealDurationMs = Math.Clamp(RevealDurationMs, 0, 1000),
        HideDurationMs = Math.Clamp(HideDurationMs, 0, 1000),
    };
}

public enum DockVisibilityState
{
    Hidden = 0,
    Revealing,
    Visible,
    Hiding,
}

public sealed class DockVisibilityChangedEventArgs(DockVisibilityState previous, DockVisibilityState current) : EventArgs
{
    public DockVisibilityState Previous { get; } = previous;
    public DockVisibilityState Current { get; } = current;
}

/// <summary>
/// The reveal/hide state machine. It is driven by <see cref="Update"/> with a monotonic clock so
/// that the whole interaction, including the hide delay, can be unit tested without a UI thread, a
/// timer or a real cursor.
/// </summary>
public sealed class DockVisibilityController
{
    private DockVisibilityOptions _options;
    private DockVisibilityState _state = DockVisibilityState.Hidden;
    private bool _wantsVisible;
    private long _wantsVisibleSince;
    private long _transitionStartedAt;
    private long _hideScheduledFor;
    private bool _hideScheduled;

    public DockVisibilityController(DockVisibilityOptions? options = null)
    {
        _options = (options ?? DockVisibilityOptions.Default).Sanitized();
    }

    public DockVisibilityOptions Options => _options;

    public DockVisibilityState State => _state;

    /// <summary>True while the grace period after the cursor left is running.</summary>
    public bool IsHideScheduled => _hideScheduled;

    public event EventHandler<DockVisibilityChangedEventArgs>? StateChanged;

    public void ApplyOptions(DockVisibilityOptions options)
    {
        _options = options.Sanitized();
        if (!_options.AutoHide && _state != DockVisibilityState.Visible)
        {
            SetState(DockVisibilityState.Visible);
        }
    }

    /// <summary>Advances the state machine.</summary>
    /// <param name="cursorInsideDock">The cursor is over the dock window itself.</param>
    /// <param name="cursorAtEdge">The cursor is inside the edge activation strip.</param>
    /// <param name="nowMs">Monotonic clock in milliseconds, e.g. <c>Environment.TickCount64</c>.</param>
    public void Update(bool cursorInsideDock, bool cursorAtEdge, long nowMs)
    {
        var wantsVisible = !_options.AutoHide || cursorInsideDock || cursorAtEdge;

        if (wantsVisible)
        {
            if (!_wantsVisible)
            {
                _wantsVisibleSince = nowMs;
            }

            _hideScheduled = false;
        }
        else
        {
            _wantsVisibleSince = nowMs;
            if (!_hideScheduled)
            {
                _hideScheduled = true;
                _hideScheduledFor = nowMs + _options.HideDelayMs;
            }
        }

        _wantsVisible = wantsVisible;

        switch (_state)
        {
            case DockVisibilityState.Hidden:
                if (wantsVisible && nowMs >= _wantsVisibleSince + _options.RevealDelayMs)
                {
                    StartTransition(DockVisibilityState.Revealing, nowMs);
                }

                break;

            case DockVisibilityState.Revealing:
                // A reveal is aborted mid animation when the cursor leaves straight away.
                if (IsHideDue(nowMs))
                {
                    StartHide(nowMs);
                }
                else if (nowMs >= _transitionStartedAt + _options.RevealDurationMs)
                {
                    SetState(DockVisibilityState.Visible);
                }

                break;

            case DockVisibilityState.Visible:
                if (IsHideDue(nowMs))
                {
                    StartHide(nowMs);
                }

                break;

            case DockVisibilityState.Hiding:
                if (wantsVisible)
                {
                    StartTransition(DockVisibilityState.Revealing, nowMs);
                }
                else if (nowMs >= _transitionStartedAt + _options.HideDurationMs)
                {
                    SetState(DockVisibilityState.Hidden);
                }

                break;
        }
    }

    private bool IsHideDue(long nowMs) => _hideScheduled && nowMs >= _hideScheduledFor;

    private void StartHide(long nowMs)
    {
        _hideScheduled = false;
        StartTransition(DockVisibilityState.Hiding, nowMs);
    }

    private void StartTransition(DockVisibilityState next, long nowMs)
    {
        _transitionStartedAt = nowMs;
        SetState(next);
    }

    /// <summary>Force the dock open, used when the context menu or settings open.</summary>
    public void Show(long nowMs)
    {
        _hideScheduled = false;
        _wantsVisible = true;
        _wantsVisibleSince = nowMs;
        _transitionStartedAt = nowMs;
        if (_state is DockVisibilityState.Hidden or DockVisibilityState.Hiding)
        {
            SetState(DockVisibilityState.Revealing);
        }
    }

    /// <summary>Force the dock closed.</summary>
    public void Hide(long nowMs)
    {
        _hideScheduled = false;
        _transitionStartedAt = nowMs;
        if (_state is not DockVisibilityState.Hidden)
        {
            SetState(DockVisibilityState.Hiding);
        }
    }

    private void SetState(DockVisibilityState next)
    {
        if (_state == next)
        {
            return;
        }

        var previous = _state;
        _state = next;
        StateChanged?.Invoke(this, new DockVisibilityChangedEventArgs(previous, next));
    }
}
