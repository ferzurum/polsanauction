namespace PolsanAuction.Identity.Domain;

/// <summary>A rule violation on one input field; <see cref="Code"/> is the machine-readable ProblemDetails code.</summary>
public sealed record FieldError(string Field, string Code);

/// <summary>Field names shared by the API and the web form.</summary>
public static class Fields
{
    public const string FirstName = "firstName";
    public const string LastName = "lastName";
    public const string Email = "email";
    public const string Password = "password";
    public const string PasswordConfirmation = "passwordConfirmation";
}

/// <summary>Error codes (docs/conventions.md, Error handling); the web maps each to a Turkish message.</summary>
public static class ErrorCodes
{
    public const string Required = "required";
    public const string TooLong = "too_long";
    public const string TooShort = "too_short";
    public const string InvalidEmail = "email.invalid";
    public const string DomainNotAllowed = "email.domain_not_allowed";
    public const string PasswordMismatch = "password.mismatch";
}
