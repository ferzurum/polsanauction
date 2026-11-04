using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PolsanAuction.Identity.Application;
using PolsanAuction.Identity.Domain;

namespace PolsanAuction.Identity.Infrastructure;

/// <summary>Composition of the Identity module for the API host.</summary>
public static class IdentityModule
{
    /// <summary>Confirmation links expire after 24 hours (spec 0002, R-2).</summary>
    public static readonly TimeSpan ConfirmationLinkLifetime = TimeSpan.FromHours(24);

    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddDbContext<IdentityDbContext>(options =>
            Configure(options, configuration.GetConnectionString("Default") ?? string.Empty));

        // Confirmation tokens (and later the auth cookie) are protected with these keys. Where the keys are
        // persisted in production belongs to the deployment spec; the default key ring is fine for dev and tests.
        services.AddDataProtection();

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                // The only password rule is the length (docs/security.md); Registration enforces 10–128.
                options.Password.RequiredLength = PasswordPolicy.MinLength;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 1;
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = string.Empty; // user name = the validated e-mail
                options.Tokens.EmailConfirmationTokenProvider = ConfirmationTokenProvider.Name;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<ConfirmationTokenProvider>(ConfirmationTokenProvider.Name);

        services.AddSingleton(provider => new ConfirmationTokenCodec(
            provider.GetRequiredService<IDataProtectionProvider>(),
            provider.GetRequiredService<TimeProvider>(),
            ConfirmationLinkLifetime));

        services.AddScoped<IUserAccounts, UserAccounts>();
        services.AddScoped<RegisterUser>();
        services.AddScoped<ConfirmEmail>();
        services.AddScoped<ResendConfirmation>();
        return services;
    }

    /// <summary>
    /// Applies pending migrations, then seeds the roles and the allowed-domain list. Runs only when
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

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in new[] { Roles.Admin, Roles.Bidder })
        {
            if (!await roles.RoleExistsAsync(role))
            {
                await roles.CreateAsync(new IdentityRole<Guid>(role) { Id = Guid.CreateVersion7() });
            }
        }

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
