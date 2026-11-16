using System.Collections.Concurrent;

namespace TravelPlanner.Service;

public class LoginAttemptTracker
{
    public const int MaxAttempts = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(1);

    private readonly ConcurrentDictionary<string, Entry> _entries = new();
    private readonly TimeProvider _time;
    private long _lastSweepTicks;

    private sealed record Entry(DateTimeOffset WindowStart, int Attempts);

    public LoginAttemptTracker(TimeProvider time)
    {
        _time = time;
    }

    // Reserves one attempt before the password is checked. Returns false when the limit is reached.
    public bool TryReserveAttempt(string key)
    {
        var now = _time.GetUtcNow();
        SweepIfNeeded(now);

        while (true)
        {
            if (!_entries.TryGetValue(key, out var current))
            {
                if (_entries.TryAdd(key, new Entry(now, 1)))
                {
                    return true;
                }

                continue;
            }

            if (now - current.WindowStart >= Window)
            {
                if (_entries.TryUpdate(key, new Entry(now, 1), current))
                {
                    return true;
                }

                continue;
            }

            if (current.Attempts >= MaxAttempts)
            {
                return false;
            }

            if (_entries.TryUpdate(key, current with { Attempts = current.Attempts + 1 }, current))
            {
                return true;
            }
        }
    }

    public void Reset(string key)
    {
        _entries.TryRemove(key, out _);
    }

    private void SweepIfNeeded(DateTimeOffset now)
    {
        var last = Interlocked.Read(ref _lastSweepTicks);
        if (now.UtcTicks - last < SweepInterval.Ticks)
        {
            return;
        }

        // Only one thread wins the swap and does the cleanup.
        if (Interlocked.CompareExchange(ref _lastSweepTicks, now.UtcTicks, last) != last)
        {
            return;
        }

        foreach (var pair in _entries)
        {
            if (now - pair.Value.WindowStart >= Window)
            {
                _entries.TryRemove(pair);
            }
        }
    }
}