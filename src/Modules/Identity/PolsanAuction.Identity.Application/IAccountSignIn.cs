using System.Security.Claims;

namespace PolsanAuction.Identity.Application;

public enum SignInStatus
{
    Succeeded,

    /// <summary>Wrong password and unknown e-mail look the same (spec 0002, AC-6).</summary>
    InvalidCredentials,

    /// <summary>Correct password, but the e-mail is not confirmed yet — only revealed after the password matched.</summary>
    EmailNotConfirmed,

    LockedOut,
}

public sealed record SignInOutcome(SignInStatus Status, DateTimeOffset? LockedUntil = null);

/// <summary>The signed-in account as the web shows it.</summary>
public sealed record CurrentAccount(Guid Id, string FirstName, string LastName, string Email, IReadOnlyList<string> Roles);

/// <summary>Cookie sign-in for employees (spec 0002, R-5…R-7), implemented by Identity.Infrastructure.</summary>
public interface IAccountSignIn
{
    Task<SignInOutcome> SignInAsync(string? email, string? password, CancellationToken cancellationToken);

    Task SignOutAsync(ClaimsPrincipal principal);

    Task<CurrentAccount?> GetCurrentAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}
