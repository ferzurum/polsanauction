using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace PolsanAuction.Api.IntegrationTests;

[Trait("Category", "Integration")]
[Collection(PostgresTestGroup.Name)]
public sealed class RegistrationTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Register_AllowedDomain_CreatesPendingBidderAndSendsConfirmation_AC1()
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = app.CreateClient();
        var email = AuthApp.NewEmail();

        using var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            AuthApp.SignUp($"  {email.ToUpperInvariant()}  ", firstName: "  Ayşe Nur ", lastName: " Yılmaz "),
            Ct);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var user = await app.WithDbAsync(db => db.Users.SingleAsync(u => u.Email == email, Ct));
        Assert.False(user.EmailConfirmed);
        Assert.Equal("Ayşe Nur", user.FirstName);
        Assert.Equal("Yılmaz", user.LastName);
        var roles = await app.WithDbAsync(db => db.UserRoles.Where(ur => ur.UserId == user.Id)
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name).ToListAsync(Ct));
        Assert.Equal(["Bidder"], roles);
        var sent = Assert.Single(app.Emails.To(email));
        Assert.Contains("/dogrula?hesap=", sent.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Register_PasswordIsNotStoredInPlainText_AC1()
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = app.CreateClient();
        var email = AuthApp.NewEmail();

        using var response = await AuthApp.RegisterAsync(client, email);

        var hash = await app.WithDbAsync(db => db.Users.Where(u => u.Email == email).Select(u => u.PasswordHash).SingleAsync(Ct));
        Assert.False(string.IsNullOrEmpty(hash));
        Assert.DoesNotContain(AuthApp.Password, hash, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ali@gmail.com")]
    [InlineData("ali@mail.polsanteknoloji.com.tr")]
    public async Task Register_DomainNotAllowed_IsRejectedWithoutAccountOrEmail_AC2(string email)
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = app.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/register", AuthApp.SignUp(email), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["email.domain_not_allowed"], await FieldErrorsAsync(response, "email"));
        Assert.False(await app.WithDbAsync(db => db.Users.AnyAsync(u => u.Email == email, Ct)));
        Assert.Empty(app.Emails.Sent);
    }

    [Fact]
    public async Task Register_ExistingEmail_GivesTheSameResultAndOnlyNotifiesTheOwner_AC3()
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = app.CreateClient();
        var email = AuthApp.NewEmail();

        using var first = await AuthApp.RegisterAsync(client, email);
        using var second = await client.PostAsJsonAsync("/api/auth/register", AuthApp.SignUp($" {email.ToUpperInvariant()} ", password: "baska-bir-parola"), Ct);

        Assert.Equal(first.StatusCode, second.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(Ct), await second.Content.ReadAsStringAsync(Ct));
        Assert.Equal(1, await app.WithDbAsync(db => db.Users.CountAsync(u => u.Email == email, Ct)));
        Assert.Collection(
            app.Emails.To(email),
            confirmation => Assert.Contains("doğrulayın", confirmation.Subject, StringComparison.Ordinal),
            notice => Assert.Contains("zaten bir hesabınız var", notice.Subject, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("", "Yılmaz", "on-karakter", "on-karakter", "firstName", "required")]
    [InlineData("Ayşe", "   ", "on-karakter", "on-karakter", "lastName", "required")]
    [InlineData("Ayşe", "Yılmaz", "dokuzkrkt", "dokuzkrkt", "password", "too_short")]
    [InlineData("Ayşe", "Yılmaz", "on-karakter", "on-karaktex", "passwordConfirmation", "password.mismatch")]
    public async Task Register_InvalidInput_ReturnsFieldErrorWithoutAccount_AC4(
        string firstName, string lastName, string password, string confirmation, string field, string code)
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = app.CreateClient();
        var email = AuthApp.NewEmail();

        using var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new { firstName, lastName, email, password, passwordConfirmation = confirmation },
            Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal([code], await FieldErrorsAsync(response, field));
        Assert.False(await app.WithDbAsync(db => db.Users.AnyAsync(u => u.Email == email, Ct)));
    }

    [Theory]
    [InlineData("ayse@")]
    [InlineData("")]
    public async Task Register_InvalidEmail_ReturnsEmailFieldError_AC4(string email)
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = app.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/register", AuthApp.SignUp(email), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Single(await FieldErrorsAsync(response, "email"));
    }

    private static async Task<string[]> FieldErrorsAsync(HttpResponseMessage response, string field)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        Assert.Equal("validation", problem.RootElement.GetProperty("code").GetString());
        return [.. problem.RootElement.GetProperty("errors").GetProperty(field).EnumerateArray().Select(e => e.GetString()!)];
    }
}
