namespace DockManager.Core.Panel;

public enum TabPanelState
{
    Hidden = 0,
    Open,
}

public sealed class TabPanelStateChangedEventArgs(TabPanelState previous, TabPanelState current) : EventArgs
{
    public TabPanelState Previous { get; } = previous;
    public TabPanelState Current { get; } = current;
}

/// <summary>
/// Visibility state machine for the secondary hover panel. The open delay prevents the panel from
/// popping up while the cursor merely travels along the dock; the close delay gives the cursor time
/// to move from the dock item into the panel without flicker. Clock driven, like the dock's own
/// visibility controller, so it is unit testable.
/// </summary>
public sealed class TabPanelController
{
    private int _openDelayMs;
    private int _closeDelayMs;

    private TabPanelState _state = TabPanelState.Hidden;
    private bool _over;
    private long _overSince;
    private bool _closeScheduled;
    private long _closeScheduledFor;

    public TabPanelController(int openDelayMs = 200, int closeDelayMs = 250)
    {
        ApplyDelays(openDelayMs, closeDelayMs);
    }

    public TabPanelState State => _state;

    public int OpenDelayMs => _openDelayMs;

    public int CloseDelayMs => _closeDelayMs;

    public event EventHandler<TabPanelStateChangedEventArgs>? StateChanged;

    public void ApplyDelays(int openDelayMs, int closeDelayMs)
    {
        _openDelayMs = Math.Clamp(openDelayMs, 0, 2000);
        _closeDelayMs = Math.Clamp(closeDelayMs, 0, 2000);
    }

    /// <summary>Advances the state machine.</summary>
    /// <param name="overSource">Cursor is over the dock item the panel belongs to.</param>
    /// <param name="overPanel">Cursor is over the panel itself.</param>
    /// <param name="nowMs">Monotonic clock in milliseconds.</param>
    public void Update(bool overSource, bool overPanel, long nowMs)
    {
        var over = overSource || overPanel;

        if (over)
        {
            if (!_over)
            {
                _overSince = nowMs;
            }

            _closeScheduled = false;

            if (_state == TabPanelState.Hidden && nowMs >= _overSince + _openDelayMs)
            {
                SetState(TabPanelState.Open, nowMs);
            }
        }
        else
        {
            _over = false;

            if (_state == TabPanelState.Open)
            {
                if (!_closeScheduled)
                {
                    _closeScheduled = true;
                    _closeScheduledFor = nowMs + _closeDelayMs;
                }
                else if (nowMs >= _closeScheduledFor)
                {
                    _closeScheduled = false;
                    SetState(TabPanelState.Hidden, nowMs);
                }
            }
        }

        if (over)
        {
            _over = true;
        }
    }

    /// <summary>Opens immediately, e.g. when the same application is hovered again right away.</summary>
    public void Show(long nowMs)
    {
        _closeScheduled = false;
        _over = true;
        _overSince = nowMs;
        if (_state == TabPanelState.Hidden)
        {
            SetState(TabPanelState.Open, nowMs);
        }
    }

    public void Hide(long nowMs)
    {
        _closeScheduled = false;
        _over = false;
        if (_state == TabPanelState.Open)
        {
            SetState(TabPanelState.Hidden, nowMs);
        }
    }

    private void SetState(TabPanelState next, long nowMs)
    {
        if (_state == next)
        {
            return;
        }

        var previous = _state;
        _state = next;
        StateChanged?.Invoke(this, new TabPanelStateChangedEventArgs(previous, next));
    }
}
