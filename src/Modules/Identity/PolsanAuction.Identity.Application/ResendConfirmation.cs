using PolsanAuction.Identity.Domain;
using PolsanAuction.Notification.Application;

namespace PolsanAuction.Identity.Application;

/// <summary>
/// Sends a fresh confirmation link to an unconfirmed account (spec 0002, R-4, AC-5/6). The caller always sees
/// the same result, so the endpoint cannot be used to find out which addresses are registered.
/// </summary>
public sealed class ResendConfirmation(IUserAccounts accounts, IEmailSender email, IAccountLinks links)
{
    public async Task HandleAsync(string? emailAddress, CancellationToken cancellationToken)
    {
        var address = EmailAddress.TryCreate(emailAddress);
        if (address is null)
        {
            return;
        }

        var account = await accounts.FindByEmailAsync(address, cancellationToken);
        if (account is null || account.EmailConfirmed)
        {
            return;
        }

        var token = await accounts.CreateConfirmationTokenAsync(account.Id, cancellationToken);
        await email.SendAsync(
            IdentityEmails.Confirmation(account.Email.Value, account.FirstName, links.Confirmation(account.Id, token)),
            cancellationToken);
    }
}
