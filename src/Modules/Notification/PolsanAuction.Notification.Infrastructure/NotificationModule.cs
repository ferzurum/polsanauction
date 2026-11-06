using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PolsanAuction.Notification.Application;

namespace PolsanAuction.Notification.Infrastructure;

/// <summary>Composition of the Notification module for the API host.</summary>
public static class NotificationModule
{
    /// <summary>
    /// Registers console delivery in Development only: the e-mail content, confirmation links included, may reach
    /// only the development console (spec 0002, R-9/R-10). Any other environment refuses to start until real SMTP
    /// delivery — an open decision (spec 0002, Constraints) — is configured (review 0002 round 1, F-2).
    /// </summary>
    public static IServiceCollection AddNotificationModule(this IServiceCollection services, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                $"No e-mail delivery is configured for the '{environment.EnvironmentName}' environment; console delivery is for Development only.");
        }

        return services.AddSingleton<IEmailSender>(_ => new ConsoleEmailSender(Console.Out));
    }
}
