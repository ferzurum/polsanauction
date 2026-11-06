using System.Collections.Concurrent;

namespace PolsanAuction.Identity.Application;

/// <summary>
/// At most one confirmation resend per address per interval (spec 0002, AC-6). Keyed by the normalized address
/// whether or not an account exists, so the limit itself reveals nothing. In-memory: one API instance.
/// </summary>
public sealed class ResendThrottle(TimeProvider clock, TimeSpan interval)
{
    /// <summary>Every this many calls, addresses whose interval has passed are forgotten.</summary>
    public const int PruneEvery = 256;

    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastAllowed = new(StringComparer.Ordinal);
    private int _calls;

    /// <summary>Addresses currently remembered; random addresses must not grow this without bound (review 0002 round 1, F-9).</summary>
    public int TrackedAddresses => _lastAllowed.Count;

    public bool TryAcquire(string normalizedEmail)
    {
        var now = clock.GetUtcNow();
        if (Interlocked.Increment(ref _calls) % PruneEvery == 0)
        {
            Prune(now);
        }

        var allowed = false;
        _lastAllowed.AddOrUpdate(
            normalizedEmail,
            _ =>
            {
                allowed = true;
                return now;
            },
            (_, last) =>
            {
                allowed = now - last >= interval;
                return allowed ? now : last;
            });
        return allowed;
    }

    private void Prune(DateTimeOffset now)
    {
        foreach (var entry in _lastAllowed)
        {
            if (now - entry.Value >= interval)
            {
                // Removes the entry only if no one renewed it meanwhile.
                ((ICollection<KeyValuePair<string, DateTimeOffset>>)_lastAllowed).Remove(entry);
            }
        }
    }
}
