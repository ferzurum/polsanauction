using Microsoft.Extensions.DependencyInjection;
using PolsanAuction.Notification.Application;

namespace PolsanAuction.Notification.Infrastructure;

/// <summary>Composition of the Notification module for the API host.</summary>
public static class NotificationModule
{
    /// <summary>
    /// Registers console delivery. Real SMTP delivery is an open decision (spec 0002, Constraints) and will be
    /// switched on by configuration once the corporate SMTP relay is known.
    /// </summary>
    public static IServiceCollection AddNotificationModule(this IServiceCollection services) =>
        services.AddSingleton<IEmailSender>(_ => new ConsoleEmailSender(Console.Out));
}
