using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using PolsanAuction.Identity.Application;
using PolsanAuction.Identity.Domain;

namespace PolsanAuction.Identity.Infrastructure;

/// <summary>
/// Sign-in in a fixed order — lockout, password, confirmation — so an unconfirmed account is revealed only to
/// someone who knows its password (Identity's PasswordSignInAsync would reveal it to anyone; spec 0002, AC-6).
/// </summary>
internal sealed class AccountSignIn(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn) : IAccountSignIn
{
    public async Task<SignInOutcome> SignInAsync(string? email, string? password, CancellationToken cancellationToken)
    {
        var address = EmailAddress.TryCreate(email);
        var user = address is null ? null : await users.FindByEmailAsync(address.Value);
        if (user is null)
        {
            return new SignInOutcome(SignInStatus.InvalidCredentials);
        }

        if (await users.IsLockedOutAsync(user))
        {
            return Locked(user);
        }

        if (!await users.CheckPasswordAsync(user, password ?? string.Empty))
        {
            // The fifth consecutive failure locks the account for 15 minutes (IdentityModule, AC-7).
            await users.AccessFailedAsync(user);
            return await users.IsLockedOutAsync(user) ? Locked(user) : new SignInOutcome(SignInStatus.InvalidCredentials);
        }

        await users.ResetAccessFailedCountAsync(user);
        if (!user.EmailConfirmed)
        {
            return new SignInOutcome(SignInStatus.EmailNotConfirmed);
        }

        await signIn.SignInAsync(user, isPersistent: false);
        return new SignInOutcome(SignInStatus.Succeeded);
    }

    public Task SignOutAsync() => signIn.SignOutAsync();

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

    private static SignInOutcome Locked(ApplicationUser user) => new(SignInStatus.LockedOut, user.LockoutEnd);
}
