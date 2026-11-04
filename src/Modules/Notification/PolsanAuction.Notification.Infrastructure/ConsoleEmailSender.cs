using PolsanAuction.Notification.Application;

namespace PolsanAuction.Notification.Infrastructure;

/// <summary>
/// Development delivery: prints the e-mail to the console instead of sending it (spec 0002, R-10).
/// Deliberately not an ILogger call — the recipient address must never reach structured logs (R-9).
/// </summary>
internal sealed class ConsoleEmailSender(TextWriter output) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        await output.WriteLineAsync(
            $"""
            ===== E-POSTA (geliştirme — gönderilmedi) =====
            Kime: {message.To}
            Konu: {message.Subject}

            {message.Body}
            ===============================================
            """.AsMemory(),
            cancellationToken);
    }
}
