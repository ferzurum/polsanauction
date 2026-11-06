using System.Net;
using System.Net.Http.Json;

namespace PolsanAuction.Api.IntegrationTests;

/// <summary>
/// AC-6: one resend per address per minute, ten sign-up attempts per source per hour; sign-in attempts per
/// source per minute (docs/security.md).
/// </summary>
[Trait("Category", "Integration")]
[Collection(PostgresTestGroup.Name)]
public sealed class RateLimitTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> ResendAsync(HttpClient client, string email) =>
        client.PostAsJsonAsync("/api/auth/resend-confirmation", new { email }, Ct);

    [Fact]
    public async Task Resend_TwiceWithinAMinute_IsLimited_ThenAllowedAgain_AC6()
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = await app.CreateApiClientAsync();
        var email = AuthApp.NewEmail();
        using var _ = await AuthApp.RegisterAsync(client, email);

        using var first = await ResendAsync(client, email);
        app.Clock.Advance(TimeSpan.FromSeconds(59));
        using var second = await ResendAsync(client, email.ToUpperInvariant());
        app.Clock.Advance(TimeSpan.FromSeconds(1));
        using var third = await ResendAsync(client, email);

        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.Equal("rate_limited", await AuthApp.ProblemCodeAsync(second));
        Assert.Equal(HttpStatusCode.Accepted, third.StatusCode);
        Assert.Equal(3, app.Emails.To(email).Count); // registration + two allowed resends
    }

    [Fact]
    public async Task Resend_LimitAppliesToUnknownAddressesToo_SoItRevealsNothing_AC6()
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = await app.CreateApiClientAsync();
        var unknown = AuthApp.NewEmail();

        using var first = await ResendAsync(client, unknown);
        using var second = await ResendAsync(client, unknown);

        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
    }

    [Fact]
    public async Task Register_EleventhAttemptWithinAnHour_IsLimited_AC6()
    {
        await using var app = new AuthApp(postgres.ConnectionString, new Dictionary<string, string> { ["Identity:RegisterAttemptsPerHour"] = "10" });
        using var client = await app.CreateApiClientAsync();

        for (var attempt = 1; attempt <= 10; attempt++)
        {
            using var allowed = await AuthApp.RegisterAsync(client, AuthApp.NewEmail());
            Assert.Equal(HttpStatusCode.Accepted, allowed.StatusCode);
        }

        using var eleventh = await AuthApp.RegisterAsync(client, AuthApp.NewEmail());

        Assert.Equal(HttpStatusCode.TooManyRequests, eleventh.StatusCode);
        Assert.Equal("rate_limited", await AuthApp.ProblemCodeAsync(eleventh));
    }

    [Fact]
    public async Task Resend_FromOneSourceOverTheLimit_IsLimitedEvenForNewAddresses()
    {
        await using var app = new AuthApp(postgres.ConnectionString, new Dictionary<string, string> { ["Identity:ResendAttemptsPerMinute"] = "3" });
        using var client = await app.CreateApiClientAsync();

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            using var allowed = await ResendAsync(client, AuthApp.NewEmail());
            Assert.Equal(HttpStatusCode.Accepted, allowed.StatusCode);
        }

        using var fourth = await ResendAsync(client, AuthApp.NewEmail());

        Assert.Equal(HttpStatusCode.TooManyRequests, fourth.StatusCode);
        Assert.Equal("rate_limited", await AuthApp.ProblemCodeAsync(fourth));
    }

    [Fact]
    public async Task Login_OverTheLimitWithinAMinute_IsLimited()
    {
        await using var app = new AuthApp(postgres.ConnectionString, new Dictionary<string, string> { ["Identity:LoginAttemptsPerMinute"] = "3" });
        using var client = await app.CreateApiClientAsync();
        var email = AuthApp.NewEmail();

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            using var allowed = await AuthApp.LoginAsync(client, email);
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        using var fourth = await AuthApp.LoginAsync(client, email);

        Assert.Equal(HttpStatusCode.TooManyRequests, fourth.StatusCode);
        Assert.Equal("rate_limited", await AuthApp.ProblemCodeAsync(fourth));
    }
}
