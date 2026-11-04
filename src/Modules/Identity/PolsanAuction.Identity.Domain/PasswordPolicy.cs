namespace PolsanAuction.Identity.Domain;

/// <summary>10–128 characters, no character-class rules (docs/security.md; spec 0002, AC-4).</summary>
public static class PasswordPolicy
{
    public const int MinLength = 10;
    public const int MaxLength = 128;

    public static void Validate(string? password, string? confirmation, ICollection<FieldError> errors)
    {
        var length = password?.Length ?? 0;
        if (length == 0)
        {
            errors.Add(new FieldError(Fields.Password, ErrorCodes.Required));
        }
        else if (length < MinLength)
        {
            errors.Add(new FieldError(Fields.Password, ErrorCodes.TooShort));
        }
        else if (length > MaxLength)
        {
            errors.Add(new FieldError(Fields.Password, ErrorCodes.TooLong));
        }

        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            errors.Add(new FieldError(Fields.PasswordConfirmation, ErrorCodes.PasswordMismatch));
        }
    }
}
