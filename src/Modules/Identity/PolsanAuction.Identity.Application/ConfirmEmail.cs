namespace PolsanAuction.Identity.Application;

/// <summary>E-mail confirmation from the link (spec 0002, R-2, AC-5).</summary>
public sealed class ConfirmEmail(IUserAccounts accounts)
{
    public Task<ConfirmationOutcome> HandleAsync(Guid accountId, string? token, CancellationToken cancellationToken) =>
        accountId == Guid.Empty || string.IsNullOrWhiteSpace(token)
            ? Task.FromResult(ConfirmationOutcome.Invalid)
            : accounts.ConfirmEmailAsync(accountId, token, cancellationToken);
}
