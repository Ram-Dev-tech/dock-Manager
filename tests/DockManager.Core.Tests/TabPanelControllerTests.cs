using DockManager.Core.Panel;

namespace DockManager.Core.Tests;

public class TabPanelControllerTests
{
    private static TabPanelController Create(int openDelay = 200, int closeDelay = 250)
        => new(openDelay, closeDelay);

    [Fact]
    public void Starts_hidden()
    {
        var controller = Create();
        controller.Update(overSource: false, overPanel: false, nowMs: 1000);
        Assert.Equal(TabPanelState.Hidden, controller.State);
    }

    [Fact]
    public void Opens_only_after_the_hover_delay()
    {
        var controller = Create(openDelay: 200);

        controller.Update(true, false, 1000);
        Assert.Equal(TabPanelState.Hidden, controller.State);

        controller.Update(true, false, 1199);
        Assert.Equal(TabPanelState.Hidden, controller.State);

        controller.Update(true, false, 1200);
        Assert.Equal(TabPanelState.Open, controller.State);
    }

    [Fact]
    public void Travelling_across_the_dock_does_not_open_the_panel()
    {
        var controller = Create(openDelay: 200);

        controller.Update(true, false, 1000);
        controller.Update(false, false, 1050);
        controller.Update(true, false, 1100);
        // The cursor rested less than the delay on each pass.
        controller.Update(true, false, 1150);
        Assert.Equal(TabPanelState.Hidden, controller.State);
    }

    [Fact]
    public void Moving_into_the_panel_keeps_it_open()
    {
        var controller = Create();

        controller.Update(true, false, 1000);
        controller.Update(true, false, 1200);
        Assert.Equal(TabPanelState.Open, controller.State);

        // Cursor leaves the dock item and lands on the panel.
        controller.Update(false, true, 1250);
        controller.Update(false, true, 5000);
        Assert.Equal(TabPanelState.Open, controller.State);
    }

    [Fact]
    public void Closing_waits_for_the_grace_period()
    {
        var controller = Create(closeDelay: 250);
        controller.Update(true, false, 1000);
        controller.Update(true, false, 1200);

        controller.Update(false, false, 1300);
        Assert.Equal(TabPanelState.Open, controller.State);

        controller.Update(false, false, 1549);
        Assert.Equal(TabPanelState.Open, controller.State);

        controller.Update(false, false, 1550);
        Assert.Equal(TabPanelState.Hidden, controller.State);
    }

    [Fact]
    public void Returning_during_the_grace_period_cancels_the_close()
    {
        var controller = Create(closeDelay: 250);
        var transitions = new List<TabPanelState>();
        controller.StateChanged += (_, e) => transitions.Add(e.Current);

        controller.Update(true, false, 1000);
        controller.Update(true, false, 1200);
        controller.Update(false, false, 1300);
        controller.Update(false, true, 1400);
        controller.Update(false, true, 9000);

        Assert.Equal(TabPanelState.Open, controller.State);
        Assert.DoesNotContain(TabPanelState.Hidden, transitions);
    }

    [Fact]
    public void Show_and_hide_force_the_state()
    {
        var controller = Create();

        controller.Show(1000);
        Assert.Equal(TabPanelState.Open, controller.State);

        controller.Hide(1100);
        Assert.Equal(TabPanelState.Hidden, controller.State);
    }

    [Fact]
    public void Delays_are_clamped()
    {
        var controller = new TabPanelController(-10, 99999);
        Assert.Equal(0, controller.OpenDelayMs);
        Assert.Equal(2000, controller.CloseDelayMs);
    }
}
