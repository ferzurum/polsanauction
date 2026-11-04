using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PolsanAuction.Identity.Infrastructure;

namespace PolsanAuction.Api.IntegrationTests;

/// <summary>
/// The API hosted against the shared test database with migrations applied on startup.
/// Later plan steps add the fake clock, the fake e-mail sender and the rate-limit overrides here.
/// </summary>
public sealed class AuthApp(string connectionString) : WebApplicationFactory<Program>
{
    public const string AllowedDomain = "polsanteknoloji.com.tr";

    /// <summary>A unique address on the allowed domain, so tests sharing the database never collide.</summary>
    public static string NewEmail(string prefix = "user") => $"{prefix}-{Guid.NewGuid():N}@{AllowedDomain}";

    public async Task<T> WithDbAsync<T>(Func<IdentityDbContext, Task<T>> query)
    {
        await using var scope = Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<IdentityDbContext>());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", connectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
    }
}
