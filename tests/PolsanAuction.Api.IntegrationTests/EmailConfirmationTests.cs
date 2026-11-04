using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace PolsanAuction.Api.IntegrationTests;

[Trait("Category", "Integration")]
[Collection(PostgresTestGroup.Name)]
public sealed class EmailConfirmationTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> ConfirmAsync(HttpClient client, Guid accountId, string token) =>
        client.PostAsJsonAsync("/api/auth/confirm-email", new { accountId, token }, Ct);

    private static Task<bool> IsConfirmedAsync(AuthApp app, string email) =>
        app.WithDbAsync(db => db.Users.Where(u => u.Email == email).Select(u => u.EmailConfirmed).SingleAsync(Ct));

    private static async Task AssertInvalidAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        Assert.Equal("confirmation.invalid", problem.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Confirm_ValidLink_ActivatesTheAccount_AC5()
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = app.CreateClient();
        var email = AuthApp.NewEmail();
        using var _ = await AuthApp.RegisterAsync(client, email);
        var (account, token) = app.LatestConfirmationLink(email);

        app.Clock.Advance(TimeSpan.FromHours(24) - TimeSpan.FromSeconds(1));
        using var response = await ConfirmAsync(client, account, token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(await IsConfirmedAsync(app, email));
    }

    [Fact]
    public async Task Confirm_LinkOlderThan24Hours_DoesNotActivate_AC5()
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = app.CreateClient();
        var email = AuthApp.NewEmail();
        using var _ = await AuthApp.RegisterAsync(client, email);
        var (account, token) = app.LatestConfirmationLink(email);

        app.Clock.Advance(TimeSpan.FromHours(24) + TimeSpan.FromSeconds(1));
        using var response = await ConfirmAsync(client, account, token);

        await AssertInvalidAsync(response);
        Assert.False(await IsConfirmedAsync(app, email));
    }

    [Fact]
    public async Task Confirm_LinkUsedBefore_IsRejected_AC5()
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = app.CreateClient();
        var email = AuthApp.NewEmail();
        using var _ = await AuthApp.RegisterAsync(client, email);
        var (account, token) = app.LatestConfirmationLink(email);

        using var first = await ConfirmAsync(client, account, token);
        using var second = await ConfirmAsync(client, account, token);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        await AssertInvalidAsync(second);
    }

    [Fact]
    public async Task Confirm_BrokenLink_IsRejected_AC5()
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = app.CreateClient();
        var email = AuthApp.NewEmail();
        using var _ = await AuthApp.RegisterAsync(client, email);
        var (account, token) = app.LatestConfirmationLink(email);

        using var tampered = await ConfirmAsync(client, account, token[..^2] + (token[^2] == 'A' ? "BB" : "AA"));
        using var otherAccount = await ConfirmAsync(client, Guid.CreateVersion7(), token);
        using var garbage = await ConfirmAsync(client, account, "bozuk-kod");
        using var empty = await ConfirmAsync(client, account, "");

        await AssertInvalidAsync(tampered);
        await AssertInvalidAsync(otherAccount);
        await AssertInvalidAsync(garbage);
        await AssertInvalidAsync(empty);
        Assert.False(await IsConfirmedAsync(app, email));
    }

    [Fact]
    public async Task Resend_UnconfirmedAccount_SendsANewWorkingLink_AC5()
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = app.CreateClient();
        var email = AuthApp.NewEmail();
        using var _ = await AuthApp.RegisterAsync(client, email);
        app.Clock.Advance(TimeSpan.FromHours(30));

        using var resend = await client.PostAsJsonAsync("/api/auth/resend-confirmation", new { email = email.ToUpperInvariant() }, Ct);
        var (account, token) = app.LatestConfirmationLink(email);
        using var confirm = await ConfirmAsync(client, account, token);

        Assert.Equal(HttpStatusCode.Accepted, resend.StatusCode);
        Assert.Equal(2, app.Emails.To(email).Count);
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
    }

    [Fact]
    public async Task Resend_UnknownOrConfirmedAddress_SendsNothingButAnswersTheSame_AC5()
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = app.CreateClient();
        var confirmed = AuthApp.NewEmail();
        using var _ = await AuthApp.RegisterAsync(client, confirmed);
        var (account, token) = app.LatestConfirmationLink(confirmed);
        using var __ = await ConfirmAsync(client, account, token);
        var unknown = AuthApp.NewEmail();

        using var forConfirmed = await client.PostAsJsonAsync("/api/auth/resend-confirmation", new { email = confirmed }, Ct);
        using var forUnknown = await client.PostAsJsonAsync("/api/auth/resend-confirmation", new { email = unknown }, Ct);

        Assert.Equal(HttpStatusCode.Accepted, forConfirmed.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, forUnknown.StatusCode);
        Assert.Single(app.Emails.To(confirmed));
        Assert.Empty(app.Emails.To(unknown));
    }
}
