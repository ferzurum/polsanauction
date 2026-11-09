using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Time.Testing;
using PolsanAuction.Identity.Infrastructure;

namespace PolsanAuction.Identity.UnitTests;

/// <summary>The confirmation link rules of spec 0002, AC-5, on the token itself (plan 0002, A-1).</summary>
public sealed class ConfirmationTokenCodecTests
{
    private const string Purpose = "EmailConfirmation";
    private const string Stamp = "stamp-1";
    private static readonly Guid Account = Guid.CreateVersion7();
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero));
    private readonly ConfirmationTokenCodec _codec;

    public ConfirmationTokenCodecTests() =>
        _codec = new ConfirmationTokenCodec(new EphemeralDataProtectionProvider(), _clock, Lifetime);

    [Fact]
    public void IsValid_FreshToken_IsAccepted_AC5() =>
        Assert.True(_codec.IsValid(_codec.Create(Account, Purpose, Stamp), Account, Purpose, Stamp));

    [Fact]
    public void IsValid_AtExactly24Hours_IsStillAccepted_AC5()
    {
        var token = _codec.Create(Account, Purpose, Stamp);
        _clock.Advance(Lifetime);

        Assert.True(_codec.IsValid(token, Account, Purpose, Stamp));
    }

    [Fact]
    public void IsValid_OneSecondAfter24Hours_IsRejected_AC5()
    {
        var token = _codec.Create(Account, Purpose, Stamp);
        _clock.Advance(Lifetime + TimeSpan.FromSeconds(1));

        Assert.False(_codec.IsValid(token, Account, Purpose, Stamp));
    }

    [Fact]
    public void IsValid_TokenOfAnotherAccount_IsRejected_AC5() =>
        Assert.False(_codec.IsValid(_codec.Create(Account, Purpose, Stamp), Guid.CreateVersion7(), Purpose, Stamp));

    [Fact]
    public void IsValid_TokenForAnotherPurpose_IsRejected_AC5() =>
        Assert.False(_codec.IsValid(_codec.Create(Account, "ResetPassword", Stamp), Account, Purpose, Stamp));

    [Fact]
    public void IsValid_AfterSecurityStampChanged_IsRejected_AC5() =>
        Assert.False(_codec.IsValid(_codec.Create(Account, Purpose, Stamp), Account, Purpose, "stamp-2"));

    [Theory]
    [InlineData("")]
    [InlineData("not base64 !")]
    [InlineData("AAAA")]
    public void IsValid_MalformedToken_IsRejected_AC5(string token) =>
        Assert.False(_codec.IsValid(token, Account, Purpose, Stamp));

    [Fact]
    public void IsValid_TamperedToken_IsRejected_AC5()
    {
        var bytes = Convert.FromBase64String(_codec.Create(Account, Purpose, Stamp));
        bytes[^1] ^= 0x01;

        Assert.False(_codec.IsValid(Convert.ToBase64String(bytes), Account, Purpose, Stamp));
    }

    [Fact]
    public void IsValid_TokenFromAnotherKeyRing_IsRejected_AC5()
    {
        var foreign = new ConfirmationTokenCodec(new EphemeralDataProtectionProvider(), _clock, Lifetime);

        Assert.False(_codec.IsValid(foreign.Create(Account, Purpose, Stamp), Account, Purpose, Stamp));
    }
}
