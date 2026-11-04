using System.Net;

namespace PolsanAuction.Api.IntegrationTests;

/// <summary>AC-8: sign-out, the 8-hour idle limit (cookie handler on the fake clock) and anonymous access.</summary>
[Trait("Category", "Integration")]
[Collection(PostgresTestGroup.Name)]
public sealed class SessionTests(PostgresFixture postgres)
{
    private static readonly TimeSpan IdleLimit = TimeSpan.FromHours(8);

    private async Task<(AuthApp App, HttpClient Client)> SignedInAsync()
    {
        var app = new AuthApp(postgres.ConnectionString);
        var client = await app.CreateApiClientAsync();
        var email = await app.CreateConfirmedAccountAsync(client);
        using var login = await AuthApp.LoginAsync(client, email);
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        await AuthApp.RefreshAntiforgeryAsync(client);
        return (app, client);
    }

    [Fact]
    public async Task Logout_EndsTheSession_AC8()
    {
        var (app, client) = await SignedInAsync();
        await using var _ = app;
        using var __ = client;

        using var logout = await client.PostAsync(new Uri("/api/auth/logout", UriKind.Relative), null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await AuthApp.MeAsync(client)).StatusCode);
    }

    [Fact]
    public async Task Session_IdleForMoreThan8Hours_IsNoLongerValid_AC8()
    {
        var (app, client) = await SignedInAsync();
        await using var _ = app;
        using var __ = client;

        app.Clock.Advance(IdleLimit + TimeSpan.FromSeconds(1));

        Assert.Equal(HttpStatusCode.Unauthorized, (await AuthApp.MeAsync(client)).StatusCode);
    }

    [Fact]
    public async Task Session_WithActivity_StaysValidPast8Hours_ThenExpiresWhenIdle_AC8()
    {
        var (app, client) = await SignedInAsync();
        await using var _ = app;
        using var __ = client;

        app.Clock.Advance(TimeSpan.FromHours(5));
        Assert.Equal(HttpStatusCode.OK, (await AuthApp.MeAsync(client)).StatusCode);
        app.Clock.Advance(TimeSpan.FromHours(5));
        Assert.Equal(HttpStatusCode.OK, (await AuthApp.MeAsync(client)).StatusCode);
        app.Clock.Advance(IdleLimit + TimeSpan.FromSeconds(1));

        Assert.Equal(HttpStatusCode.Unauthorized, (await AuthApp.MeAsync(client)).StatusCode);
    }

    [Fact]
    public async Task Me_WithoutASession_IsUnauthorized_AC8()
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = await app.CreateApiClientAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await AuthApp.MeAsync(client)).StatusCode);
    }
}
