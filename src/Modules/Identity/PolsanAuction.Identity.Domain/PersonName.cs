namespace PolsanAuction.Identity.Domain;

/// <summary>A first or last name: trimmed, 1–100 characters; Turkish letters and inner spaces are valid (AC-4).</summary>
public static class PersonName
{
    public const int MaxLength = 100;

    public static string? Normalize(string? input, string field, ICollection<FieldError> errors)
    {
        var value = input?.Trim() ?? string.Empty;
        if (value.Length == 0)
        {
            errors.Add(new FieldError(field, ErrorCodes.Required));
            return null;
        }

        if (value.Length > MaxLength)
        {
            errors.Add(new FieldError(field, ErrorCodes.TooLong));
            return null;
        }

        return value;
    }
}
