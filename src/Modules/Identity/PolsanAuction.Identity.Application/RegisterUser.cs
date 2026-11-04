using PolsanAuction.Identity.Domain;
using PolsanAuction.Notification.Application;

namespace PolsanAuction.Identity.Application;

/// <summary>The sign-up form as submitted.</summary>
public sealed record RegisterUserRequest(string? FirstName, string? LastName, string? Email, string? Password, string? PasswordConfirmation);

/// <summary>
/// Sign-up (spec 0002, R-1…R-3). A valid request always ends in the same outward result, whether or not the
/// address already has an account — only the e-mail sent differs, so the screen never reveals who is registered.
/// </summary>
public sealed class RegisterUser(IUserAccounts accounts, IEmailSender email, IAccountLinks links)
{
    /// <returns>Field errors; empty when the request was accepted ("check your e-mail").</returns>
    public async Task<IReadOnlyList<FieldError>> HandleAsync(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var allowedDomains = await accounts.GetAllowedDomainsAsync(cancellationToken);
        var (registration, errors) = Registration.Create(
            request.FirstName, request.LastName, request.Email, request.Password, request.PasswordConfirmation, allowedDomains);
        if (registration is null)
        {
            return errors;
        }

        var existing = await accounts.FindByEmailAsync(registration.Email, cancellationToken);
        var created = existing is null ? await accounts.CreatePendingAsync(registration, cancellationToken) : null;

        if (created is null)
        {
            // Already registered (or registered concurrently a moment ago): notify the owner only.
            var owner = existing ?? await accounts.FindByEmailAsync(registration.Email, cancellationToken);
            var name = owner?.FirstName ?? registration.FirstName;
            await email.SendAsync(IdentityEmails.AlreadyRegistered(registration.Email.Value, name, links.Login()), cancellationToken);
            return [];
        }

        var token = await accounts.CreateConfirmationTokenAsync(created.Id, cancellationToken);
        await email.SendAsync(
            IdentityEmails.Confirmation(created.Email.Value, created.FirstName, links.Confirmation(created.Id, token)),
            cancellationToken);
        return [];
    }
}
