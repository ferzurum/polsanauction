using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PolsanAuction.Identity.Infrastructure;

/// <summary>Composition of the Identity module for the API host.</summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            Configure(options, configuration.GetConnectionString("Default") ?? string.Empty));

        // Confirmation tokens (and later the auth cookie) are protected with these keys. Where the keys are
        // persisted in production belongs to the deployment spec; the default key ring is fine for dev and tests.
        services.AddDataProtection();

        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }

    /// <summary>
    /// Applies pending migrations and seeds the allowed-domain list. Runs only when
    /// <c>Database:MigrateOnStartup</c> is true (local development via launchSettings, integration tests).
    /// </summary>
    public static async Task InitializeIdentityDatabaseAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        if (!app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        var initialDomains = app.Configuration.GetSection("Identity:InitialAllowedDomains").Get<string[]>() ?? [];
        await AllowedDomainSeeder.SeedAsync(db, initialDomains, cancellationToken);
    }

    internal static DbContextOptionsBuilder Configure(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", IdentityDbContext.Schema))
            .UseSnakeCaseNamingConvention();

    internal static DbContextOptionsBuilder<IdentityDbContext> Configure(DbContextOptionsBuilder<IdentityDbContext> options, string connectionString)
    {
        Configure((DbContextOptionsBuilder)options, connectionString);
        return options;
    }
}
