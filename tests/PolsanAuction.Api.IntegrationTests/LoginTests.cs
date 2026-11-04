using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PolsanAuction.Api.IntegrationTests;

[Trait("Category", "Integration")]
[Collection(PostgresTestGroup.Name)]
public sealed class LoginTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Login_ConfirmedAccountWithRightPassword_StartsASession_AC6()
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = await app.CreateApiClientAsync();
        var email = await app.CreateConfirmedAccountAsync(client);

        using var login = await AuthApp.LoginAsync(client, $" {email.ToUpperInvariant()} ");
        using var me = await AuthApp.MeAsync(client);

        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        using var account = JsonDocument.Parse(await me.Content.ReadAsStringAsync(Ct));
        Assert.Equal(email, account.RootElement.GetProperty("email").GetString());
        Assert.Equal("Ayşe", account.RootElement.GetProperty("firstName").GetString());
        Assert.Equal("Yılmaz", account.RootElement.GetProperty("lastName").GetString());
    }

    [Fact]
    public async Task Login_WrongPasswordAndUnknownEmail_GetTheSameGenericError_AC6()
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = await app.CreateApiClientAsync();
        var email = await app.CreateConfirmedAccountAsync(client);

        using var wrongPassword = await AuthApp.LoginAsync(client, email, "yanlis-parola-1");
        using var unknownEmail = await AuthApp.LoginAsync(client, AuthApp.NewEmail(), "yanlis-parola-1");

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(wrongPassword.StatusCode, unknownEmail.StatusCode);
        Assert.Equal("auth.invalid_credentials", await AuthApp.ProblemCodeAsync(wrongPassword));
        Assert.Equal(await WithoutTraceIdAsync(wrongPassword), await WithoutTraceIdAsync(unknownEmail));
        Assert.Equal(HttpStatusCode.Unauthorized, (await AuthApp.MeAsync(client)).StatusCode);
    }

    /// <summary>The problem body minus <c>traceId</c>, which differs per request and says nothing about the account.</summary>
    private static async Task<string> WithoutTraceIdAsync(HttpResponseMessage response)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return string.Join(
            ";",
            problem.RootElement.EnumerateObject()
                .Where(property => property.Name != "traceId")
                .Select(property => $"{property.Name}={property.Value.GetRawText()}"));
    }

    [Fact]
    public async Task Login_UnconfirmedAccount_IsRefusedWithAResendHint_AC6()
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = await app.CreateApiClientAsync();
        var email = AuthApp.NewEmail();
        using var _ = await AuthApp.RegisterAsync(client, email);

        using var login = await AuthApp.LoginAsync(client, email);

        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
        Assert.Equal("auth.email_not_confirmed", await AuthApp.ProblemCodeAsync(login));
        Assert.Equal(HttpStatusCode.Unauthorized, (await AuthApp.MeAsync(client)).StatusCode);
    }

    [Fact]
    public async Task Login_UnconfirmedAccountWithWrongPassword_RevealsNothing_AC6()
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = await app.CreateApiClientAsync();
        var email = AuthApp.NewEmail();
        using var _ = await AuthApp.RegisterAsync(client, email);

        using var login = await AuthApp.LoginAsync(client, email, "yanlis-parola-1");

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal("auth.invalid_credentials", await AuthApp.ProblemCodeAsync(login));
    }

    [Fact]
    public async Task Login_WithoutAntiforgeryToken_IsRejected()
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = await app.CreateApiClientAsync();
        var email = await app.CreateConfirmedAccountAsync(client);
        client.DefaultRequestHeaders.Remove(AuthApp.AntiforgeryHeader);

        using var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = AuthApp.Password }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode);
        Assert.Equal("csrf.invalid", await AuthApp.ProblemCodeAsync(login));
        Assert.Equal(HttpStatusCode.Unauthorized, (await AuthApp.MeAsync(client)).StatusCode);
    }
}
