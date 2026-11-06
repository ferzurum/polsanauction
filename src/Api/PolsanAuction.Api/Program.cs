using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PolsanAuction.Api.Admin;
using PolsanAuction.Api.Auth;
using PolsanAuction.Api.Health;
using PolsanAuction.Identity.Application;
using PolsanAuction.Identity.Domain;
using PolsanAuction.Identity.Infrastructure;
using PolsanAuction.Notification.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddNotificationModule(builder.Environment);
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<IAccountLinks, AccountLinks>();
builder.Services.AddAuthorization(options =>
    options.AddPolicy(AdminEndpoints.Policy, policy => policy.RequireRole(Roles.Admin)));
builder.Services.AddAuthRateLimits();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = AntiforgeryFilter.HeaderName;
    options.Cookie.Name = "polsan.csrf";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

var app = builder.Build();

await app.InitializeIdentityDatabaseAsync();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Anonymous by design (docs/security.md): reveals only Healthy / Unhealthy.
app.MapHealthChecks("/api/health", new HealthCheckOptions
{
    ResponseWriter = HealthResponse.WriteAsync,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
    },
}).AllowAnonymous();

app.MapAuthEndpoints();
app.MapAdminEndpoints();

app.Run();

/// <summary>Entry point; public so integration tests can host the app (WebApplicationFactory).</summary>
public partial class Program;
