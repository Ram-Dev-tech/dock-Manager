using DockManager.Core.Dock;

namespace DockManager.Core.Tests;

public class DockAnimatorTests
{
    [Fact]
    public void Animation_runs_from_start_to_target()
    {
        var animator = new DockAnimator();
        var frames = new List<double>();
        animator.Frame += value => frames.Add(value);
        var completed = 0;
        animator.Completed += () => completed++;

        animator.Start(0d, 1d, 100, nowMs: 0);
        Assert.True(animator.IsRunning);

        animator.Tick(50);
        Assert.InRange(animator.Progress, 0.5d, 1d);

        animator.Tick(100);
        Assert.False(animator.IsRunning);
        Assert.Equal(1d, animator.Progress);
        Assert.Equal(1, completed);
        Assert.NotEmpty(frames);
    }

    [Fact]
    public void Zero_duration_completes_immediately()
    {
        var animator = new DockAnimator();
        animator.Start(0d, 1d, 0, nowMs: 500);

        Assert.False(animator.IsRunning);
        Assert.Equal(1d, animator.Progress);
    }

    [Fact]
    public void SnapTo_jumps_without_animating()
    {
        var animator = new DockAnimator();
        animator.Start(0d, 1d, 200, nowMs: 0);

        animator.SnapTo(0.25);

        Assert.False(animator.IsRunning);
        Assert.Equal(0.25, animator.Progress);
        Assert.False(animator.Tick(10));
    }

    [Fact]
    public void Easing_is_monotonic_and_within_bounds()
    {
        var previous = -1d;
        for (var i = 0; i <= 10; i++)
        {
            var value = DockAnimator.EaseOutCubic(i / 10d);
            Assert.InRange(value, 0d, 1d);
            Assert.True(value >= previous);
            previous = value;
        }

        Assert.Equal(0d, DockAnimator.EaseOutCubic(0d));
        Assert.Equal(1d, DockAnimator.EaseOutCubic(1d));
        Assert.Equal(0d, DockAnimator.EaseOutCubic(-5d));
        Assert.Equal(1d, DockAnimator.EaseOutCubic(5d));
    }

    [Fact]
    public void Hide_animation_runs_backwards()
    {
        var animator = new DockAnimator();
        animator.SnapTo(1d);
        animator.Start(1d, 0d, 120, nowMs: 1000);

        animator.Tick(1060);
        Assert.InRange(animator.Progress, 0d, 0.5d);

        animator.Tick(1120);
        Assert.Equal(0d, animator.Progress);
    }
}
