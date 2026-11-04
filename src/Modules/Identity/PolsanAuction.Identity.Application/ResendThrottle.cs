using System.Collections.Concurrent;

namespace PolsanAuction.Identity.Application;

/// <summary>
/// At most one confirmation resend per address per interval (spec 0002, AC-6). Keyed by the normalized address
/// whether or not an account exists, so the limit itself reveals nothing. In-memory: one API instance.
/// </summary>
public sealed class ResendThrottle(TimeProvider clock, TimeSpan interval)
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastAllowed = new(StringComparer.Ordinal);

    public bool TryAcquire(string normalizedEmail)
    {
        var now = clock.GetUtcNow();
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
}
