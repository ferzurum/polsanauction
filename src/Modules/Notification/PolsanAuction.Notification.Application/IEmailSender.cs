namespace PolsanAuction.Notification.Application;

/// <summary>An outgoing e-mail. <see cref="To"/> is personal data: never log it (docs/security.md).</summary>
public sealed record EmailMessage(string To, string Subject, string Body);

/// <summary>Sends e-mail. Development prints to the console; real delivery is configured later (spec 0002, R-10).</summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
