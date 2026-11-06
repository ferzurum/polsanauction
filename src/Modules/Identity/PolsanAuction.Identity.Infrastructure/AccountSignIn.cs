using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PolsanAuction.Identity.Application;
using PolsanAuction.Identity.Domain;

namespace PolsanAuction.Identity.Infrastructure;

/// <summary>
/// Sign-in in a fixed order — lockout, password, confirmation — so an unconfirmed account is revealed only to
/// someone who knows its password (Identity's PasswordSignInAsync would reveal it to anyone; spec 0002, AC-6).
/// </summary>
internal sealed class AccountSignIn(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, IdentityDbContext db)
    : IAccountSignIn
{
    // Same hasher settings for every instance; computing it twice under a race is harmless.
    private static string? _unknownAccountHash;

    public async Task<SignInOutcome> SignInAsync(string? email, string? password, CancellationToken cancellationToken)
    {
        var address = EmailAddress.TryCreate(email);
        var user = address is null ? null : await users.FindByEmailAsync(address.Value);
        if (user is null)
        {
            // Check the password against a throwaway hash anyway, so an unknown e-mail answers as slowly as a
            // wrong password and timing does not reveal who is registered (R-5; review 0002 round 1, F-8).
            _ = users.PasswordHasher.VerifyHashedPassword(new ApplicationUser(), UnknownAccountHash(), password ?? string.Empty);
            return new SignInOutcome(SignInStatus.InvalidCredentials);
        }

        if (await users.IsLockedOutAsync(user))
        {
            return Locked(user);
        }

        if (!await users.CheckPasswordAsync(user, password ?? string.Empty))
        {
            var lockedUntil = await RecordFailureAsync(user, cancellationToken);
            return lockedUntil is null
                ? new SignInOutcome(SignInStatus.InvalidCredentials)
                : new SignInOutcome(SignInStatus.LockedOut, lockedUntil);
        }

        await users.ResetAccessFailedCountAsync(user);
        if (!user.EmailConfirmed)
        {
            return new SignInOutcome(SignInStatus.EmailNotConfirmed);
        }

        await signIn.SignInAsync(user, isPersistent: false);
        return new SignInOutcome(SignInStatus.Succeeded);
    }

    /// <summary>A new security stamp invalidates every copy of the session cookie, not just the browser's one (F-3).</summary>
    public async Task SignOutAsync(ClaimsPrincipal principal)
    {
        if (await users.GetUserAsync(principal) is { } user)
        {
            await users.UpdateSecurityStampAsync(user);
        }

        await signIn.SignOutAsync();
    }

    public async Task<CurrentAccount?> GetCurrentAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null)
        {
            return null;
        }

        var roles = await users.GetRolesAsync(user);
        return new CurrentAccount(user.Id, user.FirstName, user.LastName, user.Email ?? string.Empty, [.. roles.Order(StringComparer.Ordinal)]);
    }

    /// <summary>
    /// The fifth consecutive failure locks the account for 15 minutes (IdentityModule, AC-7). One atomic UPDATE
    /// instead of AccessFailedAsync: Identity's read-modify-write loses increments under parallel requests and
    /// reports that as a ConcurrencyFailure result, not an exception (review 0002 round 1, F-1). The lock end is
    /// read from the real clock because Identity's IsLockedOutAsync compares against it (plan 0002, A-1).
    /// Returns the lock end when the account is locked after this failure.
    /// </summary>
    private async Task<DateTimeOffset?> RecordFailureAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var maxFailures = users.Options.Lockout.MaxFailedAccessAttempts;
        var lockEnd = TimeProvider.System.GetUtcNow().Add(users.Options.Lockout.DefaultLockoutTimeSpan);

        // A new concurrency stamp makes a stale tracked write of this user (e.g. a parallel successful sign-in
        // resetting the count) fail instead of overwriting the lock.
        await db.Users
            .Where(u => u.Id == user.Id && u.LockoutEnabled)
            .ExecuteUpdateAsync(
                set => set
                    .SetProperty(u => u.LockoutEnd, u => u.AccessFailedCount + 1 >= maxFailures ? lockEnd : u.LockoutEnd)
                    .SetProperty(u => u.AccessFailedCount, u => u.AccessFailedCount + 1 >= maxFailures ? 0 : u.AccessFailedCount + 1)
                    .SetProperty(u => u.ConcurrencyStamp, Guid.NewGuid().ToString()),
                cancellationToken);

        var current = await db.Users.AsNoTracking()
            .Where(u => u.Id == user.Id)
            .Select(u => u.LockoutEnd)
            .SingleAsync(cancellationToken);
        return current > TimeProvider.System.GetUtcNow() ? current : null;
    }

    private string UnknownAccountHash() =>
        _unknownAccountHash ??= users.PasswordHasher.HashPassword(new ApplicationUser(), Guid.NewGuid().ToString());

    private static SignInOutcome Locked(ApplicationUser user) => new(SignInStatus.LockedOut, user.LockoutEnd);
}
