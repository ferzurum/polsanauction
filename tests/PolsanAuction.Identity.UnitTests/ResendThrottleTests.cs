using Microsoft.Extensions.Time.Testing;
using PolsanAuction.Identity.Application;

namespace PolsanAuction.Identity.UnitTests;

public sealed class ResendThrottleTests
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero));

    [Fact]
    public void TryAcquire_SameAddressWithinTheInterval_IsRefused_ThenAllowedAgain_AC6()
    {
        var throttle = new ResendThrottle(_clock, Interval);

        var first = throttle.TryAcquire("a@polsanteknoloji.com.tr");
        _clock.Advance(Interval - TimeSpan.FromSeconds(1));
        var second = throttle.TryAcquire("a@polsanteknoloji.com.tr");
        _clock.Advance(TimeSpan.FromSeconds(1));
        var third = throttle.TryAcquire("a@polsanteknoloji.com.tr");

        Assert.Equal([true, false, true], [first, second, third]);
    }

    [Fact]
    public void TryAcquire_ManyDifferentAddresses_ForgetsTheOnesWhoseIntervalHasPassed()
    {
        var throttle = new ResendThrottle(_clock, Interval);
        for (var i = 0; i < ResendThrottle.PruneEvery - 1; i++)
        {
            throttle.TryAcquire($"random-{i}@example.com");
        }

        _clock.Advance(Interval);
        throttle.TryAcquire("fresh@polsanteknoloji.com.tr");

        Assert.Equal(1, throttle.TrackedAddresses);
    }

    [Fact]
    public void TryAcquire_Pruning_KeepsAddressesStillWithinTheirInterval()
    {
        var throttle = new ResendThrottle(_clock, Interval);
        throttle.TryAcquire("recent@polsanteknoloji.com.tr");
        for (var i = 0; i < ResendThrottle.PruneEvery - 1; i++)
        {
            throttle.TryAcquire($"random-{i}@example.com");
        }

        Assert.False(throttle.TryAcquire("recent@polsanteknoloji.com.tr"));
    }
}
