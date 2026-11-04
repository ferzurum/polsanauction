using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PolsanAuction.Identity.Application;
using PolsanAuction.Identity.Domain;

namespace PolsanAuction.Identity.Infrastructure;

/// <summary><see cref="IUserAccounts"/> on ASP.NET Core Identity. Passwords are hashed by Identity, never stored as given.</summary>
internal sealed class UserAccounts(IdentityDbContext db, UserManager<ApplicationUser> users, IOptions<IdentitySettings> settings) : IUserAccounts
{
    public async Task<IReadOnlyList<string>> GetAllowedDomainsAsync(CancellationToken cancellationToken) =>
        await db.AllowedEmailDomains.Select(domain => domain.Domain).ToListAsync(cancellationToken);

    public async Task<AccountSummary?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(email.Value);
        return user is null ? null : Summarize(user);
    }

    public async Task<AccountSummary?> CreatePendingAsync(Registration registration, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = registration.Email.Value,
            Email = registration.Email.Value,
            FirstName = registration.FirstName,
            LastName = registration.LastName,
        };

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var created = await users.CreateAsync(user, registration.Password);
        if (!created.Succeeded)
        {
            if (created.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
            {
                return null;
            }

            // Registration already validated every rule; anything else here is a configuration defect.
            throw new InvalidOperationException($"Account creation failed: {string.Join(", ", created.Errors.Select(e => e.Code))}");
        }

        var role = await users.AddToRoleAsync(user, Roles.Bidder);
        if (!role.Succeeded)
        {
            throw new InvalidOperationException($"Role assignment failed: {string.Join(", ", role.Errors.Select(e => e.Code))}");
        }

        await transaction.CommitAsync(cancellationToken);
        return Summarize(user);
    }

    public async Task<string> CreateConfirmationTokenAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(accountId.ToString())
            ?? throw new InvalidOperationException("Confirmation token requested for a missing account.");
        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    }

    public async Task<ConfirmationOutcome> ConfirmEmailAsync(Guid accountId, string token, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(accountId.ToString());

        // An already confirmed account means the link was used before: single use (spec 0002, AC-5).
        if (user is null || user.EmailConfirmed || Decode(token) is not { } decoded)
        {
            return ConfirmationOutcome.Invalid;
        }

        var result = await users.ConfirmEmailAsync(user, decoded);
        if (!result.Succeeded)
        {
            return ConfirmationOutcome.Invalid;
        }

        await InitialAdminSeeder.EnsureAsync(users, settings.Value, user);
        return ConfirmationOutcome.Confirmed;
    }

    private static string? Decode(string token)
    {
        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static AccountSummary Summarize(ApplicationUser user) =>
        new(user.Id, user.FirstName, EmailAddress.TryCreate(user.Email)!, user.EmailConfirmed);
}
