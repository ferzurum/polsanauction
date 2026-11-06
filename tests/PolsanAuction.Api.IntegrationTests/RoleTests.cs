using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PolsanAuction.Identity.Infrastructure;

namespace PolsanAuction.Api.IntegrationTests;

/// <summary>AC-9: Bidder by default, the configured initial admin, and the server-side admin check.</summary>
[Trait("Category", "Integration")]
[Collection(PostgresTestGroup.Name)]
public sealed class RoleTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Dictionary<string, string> AdminIs(string email) => new() { ["Identity:InitialAdminEmail"] = email };

    private static Task<HttpResponseMessage> AdminAreaAsync(HttpClient client) =>
        client.GetAsync(new Uri("/api/admin", UriKind.Relative), Ct);

    private static async Task<string[]> RolesAsync(HttpClient client)
    {
        using var me = await AuthApp.MeAsync(client);
        me.EnsureSuccessStatusCode();
        using var account = JsonDocument.Parse(await me.Content.ReadAsStringAsync(Ct));
        return [.. account.RootElement.GetProperty("roles").EnumerateArray().Select(role => role.GetString()!)];
    }

    [Fact]
    public async Task NewAccount_IsABidder_AndCannotOpenTheAdminArea_AC9()
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = await app.CreateApiClientAsync();
        var email = await app.CreateConfirmedAccountAsync(client);
        using var _ = await AuthApp.LoginAsync(client, email);

        Assert.Equal(["Bidder"], await RolesAsync(client));
        Assert.Equal(HttpStatusCode.Forbidden, (await AdminAreaAsync(client)).StatusCode);
    }

    [Fact]
    public async Task AdminArea_WithoutASession_IsUnauthorized_AC9()
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = await app.CreateApiClientAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await AdminAreaAsync(client)).StatusCode);
    }

    [Fact]
    public async Task InitialAdminAddress_BecomesAdminWhenConfirmed_AC9()
    {
        var adminEmail = AuthApp.NewEmail("admin");
        await using var app = new AuthApp(postgres.ConnectionString, AdminIs($" {adminEmail.ToUpperInvariant()} "));
        using var client = await app.CreateApiClientAsync();
        await app.CreateConfirmedAccountAsync(client, adminEmail);
        using var _ = await AuthApp.LoginAsync(client, adminEmail);

        Assert.Equal(["Admin", "Bidder"], await RolesAsync(client));
        Assert.Equal(HttpStatusCode.OK, (await AdminAreaAsync(client)).StatusCode);
    }

    [Fact]
    public async Task InitialAdminAddress_NotYetConfirmed_IsNotAdmin_AC9()
    {
        var adminEmail = AuthApp.NewEmail("admin");
        await using var app = new AuthApp(postgres.ConnectionString, AdminIs(adminEmail));
        using var client = await app.CreateApiClientAsync();
        using var _ = await AuthApp.RegisterAsync(client, adminEmail);

        var isAdmin = await app.WithDbAsync(async db =>
        {
            var user = db.Users.Single(u => u.Email == adminEmail);
            var adminRole = db.Roles.Single(r => r.Name == "Admin");
            return await Task.FromResult(db.UserRoles.Any(ur => ur.UserId == user.Id && ur.RoleId == adminRole.Id));
        });
        Assert.False(isAdmin);
    }

    [Fact]
    public async Task InitialAdminAddress_AlreadyConfirmedBeforeTheSetting_BecomesAdminAtStartup_AC9()
    {
        var adminEmail = AuthApp.NewEmail("admin");
        await using (var before = new AuthApp(postgres.ConnectionString))
        {
            using var client = await before.CreateApiClientAsync();
            await before.CreateConfirmedAccountAsync(client, adminEmail);
        }

        await using var after = new AuthApp(postgres.ConnectionString, AdminIs(adminEmail));
        using var adminClient = await after.CreateApiClientAsync();
        using var _ = await AuthApp.LoginAsync(adminClient, adminEmail);

        Assert.Contains("Admin", await RolesAsync(adminClient));
        Assert.Equal(HttpStatusCode.OK, (await AdminAreaAsync(adminClient)).StatusCode);
    }

    [Fact]
    public async Task RoleGrantedDuringASession_ReachesItWithinTheCheckInterval_WithoutSigningInAgain_AC9()
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = await app.CreateApiClientAsync();
        var email = await app.CreateConfirmedAccountAsync(client);
        using var _ = await AuthApp.LoginAsync(client, email);

        // The same call the initial-admin check makes at startup (InitialAdminSeeder).
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync(email);
            Assert.True((await users.AddToRoleAsync(user!, "Admin")).Succeeded);
        }

        var withinInterval = (await AdminAreaAsync(client)).StatusCode;
        app.Clock.Advance(IdentityModule.SecurityStampValidationInterval + TimeSpan.FromSeconds(1));

        Assert.Equal(HttpStatusCode.Forbidden, withinInterval); // the cookie still carries the old roles
        Assert.Equal(HttpStatusCode.OK, (await AdminAreaAsync(client)).StatusCode);
        Assert.Equal(["Admin", "Bidder"], await RolesAsync(client));
    }
}
