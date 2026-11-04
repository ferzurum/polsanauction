using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using PolsanAuction.Identity.Infrastructure;

namespace PolsanAuction.Api.Auth;

/// <summary>Sign-up attempts per source per hour (spec 0002, AC-6). Over the limit: 429 with code <c>rate_limited</c>.</summary>
internal static class RateLimits
{
    public const string Register = "register";
    public const string Code = "rate_limited";

    public static IServiceCollection AddAuthRateLimits(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            // Behind a reverse proxy the source address needs ForwardedHeaders — part of the deployment spec.
            options.AddPolicy(Register, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = context.RequestServices.GetRequiredService<IOptions<IdentitySettings>>().Value.RegisterAttemptsPerHour,
                    Window = TimeSpan.FromHours(1),
                    QueueLimit = 0,
                }));
            options.OnRejected = (context, cancellationToken) =>
                new ValueTask(TooManyAttempts().ExecuteAsync(context.HttpContext));
        });

    public static IResult TooManyAttempts() =>
        Problems.Code(StatusCodes.Status429TooManyRequests, Code, "Too many attempts. Try again later.");
}
