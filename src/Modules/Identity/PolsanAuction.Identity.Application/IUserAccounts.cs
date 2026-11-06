using PolsanAuction.Identity.Domain;

namespace PolsanAuction.Identity.Application;

/// <summary>An existing account as the use cases need to see it. Never carries the password hash.</summary>
public sealed record AccountSummary(Guid Id, string FirstName, EmailAddress Email, bool EmailConfirmed);

public enum ConfirmationOutcome
{
    Confirmed,

    /// <summary>Unknown account, malformed, expired or already used link — one answer for all (AC-5).</summary>
    Invalid,
}

/// <summary>Account storage and credential operations, implemented by Identity.Infrastructure.</summary>
public interface IUserAccounts
{
    Task<IReadOnlyList<string>> GetAllowedDomainsAsync(CancellationToken cancellationToken);

    Task<AccountSummary?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken);

    /// <summary>Creates an unconfirmed Bidder account; returns null when the e-mail was taken concurrently.</summary>
    Task<AccountSummary?> CreatePendingAsync(Registration registration, CancellationToken cancellationToken);

    /// <summary>
    /// Hashes the password and throws the hash away, so a sign-up for an existing address takes as long as one that
    /// creates an account and timing does not reveal who is registered (R-3; review 0002 round 1, F-8).
    /// </summary>
    void HashPasswordForTimingOnly(string password);

    /// <summary>A single-purpose, 24-hour confirmation token for the account.</summary>
    Task<string> CreateConfirmationTokenAsync(Guid accountId, CancellationToken cancellationToken);

    Task<ConfirmationOutcome> ConfirmEmailAsync(Guid accountId, string token, CancellationToken cancellationToken);
}

/// <summary>Links into the web application that e-mails point to; built by the host, which knows the web URL.</summary>
public interface IAccountLinks
{
    Uri Confirmation(Guid accountId, string token);

    Uri Login();
}
