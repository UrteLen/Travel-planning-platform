using TravelPlanner.Service;

namespace TravelPlanner.UnitTests;

public class LoginAttemptTrackerTests
{
    private const string Key = "user@example.com|127.0.0.1";

    [Fact]
    public void TryReserveAttempt_WhenCalledConcurrently_AllowsExactlyTheLimit()
    {
        var tracker = new LoginAttemptTracker(TimeProvider.System);
        var allowed = 0;

        Parallel.For(0, 200, _ =>
        {
            if (tracker.TryReserveAttempt(Key))
            {
                Interlocked.Increment(ref allowed);
            }
        });

        Assert.Equal(LoginAttemptTracker.MaxAttempts, allowed);
    }

    [Fact]
    public void TryReserveAttempt_ForDifferentKeys_CountsThemSeparately()
    {
        var tracker = new LoginAttemptTracker(TimeProvider.System);

        for (var i = 0; i < LoginAttemptTracker.MaxAttempts; i++)
        {
            Assert.True(tracker.TryReserveAttempt(Key));
        }

        Assert.False(tracker.TryReserveAttempt(Key));
        Assert.True(tracker.TryReserveAttempt("other@example.com|127.0.0.1"));
    }

    [Fact]
    public void Reset_AfterLimitReached_AllowsAttemptsAgain()
    {
        var tracker = new LoginAttemptTracker(TimeProvider.System);

        for (var i = 0; i < LoginAttemptTracker.MaxAttempts; i++)
        {
            tracker.TryReserveAttempt(Key);
        }

        tracker.Reset(Key);

        Assert.True(tracker.TryReserveAttempt(Key));
    }

    [Fact]
    public void TryReserveAttempt_AfterWindowExpires_AllowsAttemptsAgain()
    {
        var time = new ManualTimeProvider();
        var tracker = new LoginAttemptTracker(time);

        for (var i = 0; i < LoginAttemptTracker.MaxAttempts; i++)
        {
            tracker.TryReserveAttempt(Key);
        }

        Assert.False(tracker.TryReserveAttempt(Key));

        time.Now = time.Now.AddMinutes(11);

        Assert.True(tracker.TryReserveAttempt(Key));
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }
}