namespace PolsanAuction.Identity.Domain;

/// <summary>A validated sign-up request (spec 0002, R-1, AC-1/2/4). Built only through <see cref="Create"/>.</summary>
public sealed record Registration(string FirstName, string LastName, EmailAddress Email, string Password)
{
    /// <summary>Validates every field and returns either the registration or all field errors at once.</summary>
    public static (Registration? Registration, IReadOnlyList<FieldError> Errors) Create(
        string? firstName,
        string? lastName,
        string? email,
        string? password,
        string? passwordConfirmation,
        IEnumerable<string> allowedDomains)
    {
        var errors = new List<FieldError>();
        var first = PersonName.Normalize(firstName, Fields.FirstName, errors);
        var last = PersonName.Normalize(lastName, Fields.LastName, errors);

        var address = EmailAddress.TryCreate(email);
        if (address is null)
        {
            errors.Add(new FieldError(Fields.Email, string.IsNullOrWhiteSpace(email) ? ErrorCodes.Required : ErrorCodes.InvalidEmail));
        }
        else if (!address.IsOnAllowedDomain(allowedDomains))
        {
            errors.Add(new FieldError(Fields.Email, ErrorCodes.DomainNotAllowed));
        }

        PasswordPolicy.Validate(password, passwordConfirmation, errors);

        return errors.Count > 0
            ? (null, errors)
            : (new Registration(first!, last!, address!, password!), errors);
    }
}
