using DockManager.Core.Dock;

namespace DockManager.Core.Tests;

public class DockVisibilityControllerTests
{
    private static DockVisibilityController CreateController(Action<DockVisibilityOptionsBuilder>? configure = null)
    {
        var builder = new DockVisibilityOptionsBuilder();
        configure?.Invoke(builder);
        return new DockVisibilityController(builder.Build());
    }

    private sealed class DockVisibilityOptionsBuilder
    {
        public int RevealDelayMs { get; set; }
        public int HideDelayMs { get; set; } = 300;
        public int RevealDurationMs { get; set; } = 140;
        public int HideDurationMs { get; set; } = 170;
        public bool AutoHide { get; set; } = true;

        public DockVisibilityOptions Build() => new()
        {
            RevealDelayMs = RevealDelayMs,
            HideDelayMs = HideDelayMs,
            RevealDurationMs = RevealDurationMs,
            HideDurationMs = HideDurationMs,
            AutoHide = AutoHide,
        };
    }

    [Fact]
    public void Starts_hidden()
    {
        var controller = CreateController();
        controller.Update(cursorInsideDock: false, cursorAtEdge: false, nowMs: 1000);
        Assert.Equal(DockVisibilityState.Hidden, controller.State);
    }

    [Fact]
    public void Cursor_at_the_edge_reveals_the_dock()
    {
        var controller = CreateController();

        controller.Update(false, true, 1000);
        Assert.Equal(DockVisibilityState.Revealing, controller.State);

        controller.Update(false, true, 1000 + 140);
        Assert.Equal(DockVisibilityState.Visible, controller.State);
    }

    [Fact]
    public void Cursor_inside_the_dock_keeps_it_visible()
    {
        var controller = CreateController();
        controller.Update(false, true, 1000);
        controller.Update(true, false, 1140);
        controller.Update(true, false, 5000);

        Assert.Equal(DockVisibilityState.Visible, controller.State);
        Assert.False(controller.IsHideScheduled);
    }

    [Fact]
    public void Leaving_starts_the_hide_delay_and_only_then_hides()
    {
        var controller = CreateController();
        controller.Update(false, true, 1000);
        controller.Update(false, false, 1140);
        Assert.Equal(DockVisibilityState.Visible, controller.State);

        // Cursor is away but the grace period has not elapsed.
        controller.Update(false, false, 1140 + 299);
        Assert.Equal(DockVisibilityState.Visible, controller.State);
        Assert.True(controller.IsHideScheduled);

        controller.Update(false, false, 1140 + 300);
        Assert.Equal(DockVisibilityState.Hiding, controller.State);

        controller.Update(false, false, 1140 + 300 + 170);
        Assert.Equal(DockVisibilityState.Hidden, controller.State);
    }

    [Fact]
    public void Coming_back_during_the_hide_delay_does_not_flicker()
    {
        var controller = CreateController();
        var transitions = new List<DockVisibilityState>();
        controller.StateChanged += (_, e) => transitions.Add(e.Current);

        controller.Update(false, true, 1000);
        controller.Update(true, false, 1140);
        controller.Update(false, false, 1200);
        Assert.True(controller.IsHideScheduled);

        // The cursor is back before the delay elapsed.
        controller.Update(true, false, 1300);
        controller.Update(true, false, 3000);

        Assert.Equal(DockVisibilityState.Visible, controller.State);
        Assert.False(controller.IsHideScheduled);
        Assert.DoesNotContain(DockVisibilityState.Hiding, transitions);
    }

    [Fact]
    public void Reveal_delay_makes_the_cursor_dwell_before_the_dock_appears()
    {
        var controller = CreateController(builder => builder.RevealDelayMs = 120);

        controller.Update(false, true, 1000);
        Assert.Equal(DockVisibilityState.Hidden, controller.State);

        controller.Update(false, true, 1119);
        Assert.Equal(DockVisibilityState.Hidden, controller.State);

        controller.Update(false, true, 1120);
        Assert.Equal(DockVisibilityState.Revealing, controller.State);
    }

    [Fact]
    public void Leaving_immediately_aborts_a_running_reveal()
    {
        var controller = CreateController(builder => builder.HideDelayMs = 50);

        controller.Update(false, true, 1000);
        Assert.Equal(DockVisibilityState.Revealing, controller.State);

        controller.Update(false, false, 1010);
        controller.Update(false, false, 1060);

        Assert.Equal(DockVisibilityState.Hiding, controller.State);
    }

    [Fact]
    public void Returning_while_hiding_restarts_the_reveal()
    {
        var controller = CreateController(builder => builder.HideDelayMs = 0);

        controller.Update(false, true, 1000);
        controller.Update(false, false, 1140);
        Assert.Equal(DockVisibilityState.Hiding, controller.State);

        controller.Update(false, true, 1150);
        Assert.Equal(DockVisibilityState.Revealing, controller.State);

        controller.Update(false, true, 1150 + 140);
        Assert.Equal(DockVisibilityState.Visible, controller.State);
    }

    [Fact]
    public void Auto_hide_off_keeps_the_dock_pinned_open()
    {
        var controller = CreateController(builder => builder.AutoHide = false);

        controller.Update(false, false, 1000);
        controller.Update(false, false, 1140);
        Assert.Equal(DockVisibilityState.Visible, controller.State);

        controller.Update(false, false, 60000);
        Assert.Equal(DockVisibilityState.Visible, controller.State);
    }

    [Fact]
    public void Turning_auto_hide_on_while_pinned_open_lets_it_hide_again()
    {
        var controller = CreateController(builder => builder.AutoHide = false);
        controller.Update(false, false, 1000);
        Assert.Equal(DockVisibilityState.Visible, controller.State);

        controller.ApplyOptions(new DockVisibilityOptions { AutoHide = true, HideDelayMs = 100 });
        controller.Update(false, false, 1010);
        Assert.Equal(DockVisibilityState.Visible, controller.State);

        controller.Update(false, false, 1110);
        Assert.Equal(DockVisibilityState.Hiding, controller.State);
    }

    [Fact]
    public void Show_and_hide_force_the_state()
    {
        var controller = CreateController();

        controller.Show(1000);
        Assert.Equal(DockVisibilityState.Revealing, controller.State);

        controller.Update(false, false, 1140);
        Assert.Equal(DockVisibilityState.Visible, controller.State);

        controller.Hide(1200);
        Assert.Equal(DockVisibilityState.Hiding, controller.State);

        controller.Update(false, false, 1200 + 170);
        Assert.Equal(DockVisibilityState.Hidden, controller.State);
    }

    [Fact]
    public void Options_are_clamped_to_safe_values()
    {
        var controller = new DockVisibilityController(new DockVisibilityOptions
        {
            HideDelayMs = -50,
            RevealDurationMs = 99999,
        });

        Assert.Equal(0, controller.Options.HideDelayMs);
        Assert.Equal(1000, controller.Options.RevealDurationMs);
    }
}
