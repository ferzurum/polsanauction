using System.Data.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PolsanAuction.Identity.Application;
using PolsanAuction.Identity.Domain;

namespace PolsanAuction.Identity.Infrastructure;

/// <summary>Composition of the Identity module for the API host.</summary>
public static class IdentityModule
{
    /// <summary>Confirmation links expire after 24 hours (spec 0002, R-2).</summary>
    public static readonly TimeSpan ConfirmationLinkLifetime = TimeSpan.FromHours(24);

    /// <summary>Sign-in lock after five failures (spec 0002, R-6).</summary>
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>The session ends after this much inactivity (spec 0002, R-7).</summary>
    public static readonly TimeSpan SessionIdleTimeout = TimeSpan.FromHours(8);

    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<IdentitySettings>().Bind(configuration.GetSection(IdentitySettings.Section));
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

                // AC-7: the fifth consecutive failure locks the account for 15 minutes.
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = LockoutDuration;
                options.Lockout.AllowedForNewUsers = true;
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddTokenProvider<ConfirmationTokenProvider>(ConfirmationTokenProvider.Name);

        // R-7: a session cookie that ends after 8 idle hours; no "remember me". The handler reads the time
        // from the registered TimeProvider, so tests drive it with a fake clock.
        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddCookie(IdentityConstants.ApplicationScheme, cookie =>
            {
                cookie.Cookie.Name = "polsan.auth";
                cookie.Cookie.HttpOnly = true;
                cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                cookie.Cookie.SameSite = SameSiteMode.Strict;
                cookie.ExpireTimeSpan = SessionIdleTimeout;
                cookie.SlidingExpiration = true;

                // An API answers with status codes; the web app decides where to navigate.
                cookie.Events.OnRedirectToLogin = context => Reject(context.Response, StatusCodes.Status401Unauthorized);
                cookie.Events.OnRedirectToAccessDenied = context => Reject(context.Response, StatusCodes.Status403Forbidden);
            });

        services.AddSingleton(provider => new ConfirmationTokenCodec(
            provider.GetRequiredService<IDataProtectionProvider>(),
            provider.GetRequiredService<TimeProvider>(),
            ConfirmationLinkLifetime));

        services.AddScoped<IUserAccounts, UserAccounts>();
        services.AddScoped<IAccountSignIn, AccountSignIn>();
        services.AddScoped<RegisterUser>();
        services.AddScoped<ConfirmEmail>();
        services.AddScoped<ResendConfirmation>();
        services.AddSingleton(provider => new ResendThrottle(
            provider.GetRequiredService<TimeProvider>(),
            provider.GetRequiredService<IOptions<IdentitySettings>>().Value.ResendInterval));
        return services;
    }

    /// <summary>
    /// When <c>Database:MigrateOnStartup</c> is true (local development via launchSettings, integration tests):
    /// applies pending migrations and seeds the roles and the allowed-domain list. Always: makes the configured
    /// initial admin an Admin (R-8) — a database that is down only logs a warning, the app still starts.
    /// </summary>
    public static async Task InitializeIdentityDatabaseAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<IdentitySettings>>().Value;

        if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            await MigrateAndSeedAsync(scope.ServiceProvider, settings, cancellationToken);
        }

        try
        {
            await InitialAdminSeeder.EnsureAsync(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(), settings);
        }
        catch (Exception exception) when (IsDatabaseUnreachable(exception))
        {
            IdentityLog.InitialAdminCheckSkipped(app.Logger, exception);
        }
    }

    private static async Task MigrateAndSeedAsync(IServiceProvider services, IdentitySettings settings, CancellationToken cancellationToken)
    {
        var db = services.GetRequiredService<IdentityDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        var roles = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in new[] { Roles.Admin, Roles.Bidder })
        {
            if (!await roles.RoleExistsAsync(role))
            {
                await roles.CreateAsync(new IdentityRole<Guid>(role) { Id = Guid.CreateVersion7() });
            }
        }

        await AllowedDomainSeeder.SeedAsync(db, settings.InitialAllowedDomains, cancellationToken);
    }

    /// <summary>EF Core wraps the provider's DbException (e.g. "transient failure"), so look through the chain.</summary>
    private static bool IsDatabaseUnreachable(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbException)
            {
                return true;
            }
        }

        return false;
    }

    private static Task Reject(HttpResponse response, int statusCode)
    {
        response.StatusCode = statusCode;
        return Task.CompletedTask;
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
