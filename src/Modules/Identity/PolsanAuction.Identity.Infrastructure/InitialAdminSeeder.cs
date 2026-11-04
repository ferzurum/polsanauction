using Microsoft.AspNetCore.Identity;
using PolsanAuction.Identity.Domain;

namespace PolsanAuction.Identity.Infrastructure;

/// <summary>
/// Makes the configured initial admin an Admin once the address is confirmed — at confirmation time and again at
/// start-up, so it does not matter whether the setting or the confirmation came first (spec 0002, R-8, AC-9).
/// </summary>
internal static class InitialAdminSeeder
{
    public static async Task EnsureAsync(UserManager<ApplicationUser> users, IdentitySettings settings, ApplicationUser? user = null)
    {
        var adminEmail = EmailAddress.TryCreate(settings.InitialAdminEmail);
        if (adminEmail is null)
        {
            return;
        }

        user ??= await users.FindByEmailAsync(adminEmail.Value);
        if (user is null
            || !user.EmailConfirmed
            || !string.Equals(user.Email, adminEmail.Value, StringComparison.OrdinalIgnoreCase)
            || await users.IsInRoleAsync(user, Roles.Admin))
        {
            return;
        }

        var result = await users.AddToRoleAsync(user, Roles.Admin);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Initial admin assignment failed: {string.Join(", ", result.Errors.Select(e => e.Code))}");
        }
    }
}
