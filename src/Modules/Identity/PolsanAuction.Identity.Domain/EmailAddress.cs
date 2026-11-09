namespace PolsanAuction.Identity.Domain;

/// <summary>
/// A normalized e-mail address: surrounding whitespace removed, compared case-insensitively
/// (stored lower-case, invariant culture) — spec 0002, AC-1.
/// </summary>
public sealed record EmailAddress
{
    private const int MaxLocalPartLength = 64;
    private const int MaxDomainLength = 253;

    private EmailAddress(string value, string domain)
    {
        Value = value;
        Domain = domain;
    }

    public string Value { get; }

    public string Domain { get; }

    public static EmailAddress? TryCreate(string? input)
    {
        var value = input?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(value) || value.Any(char.IsWhiteSpace) || value.Any(char.IsControl))
        {
            return null;
        }

        var at = value.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0 || at != value.LastIndexOf('@') || at > MaxLocalPartLength)
        {
            return null;
        }

        var domain = value[(at + 1)..];
        var labels = domain.Split('.');
        if (domain.Length > MaxDomainLength || labels.Length < 2 || labels.Any(label => label.Length == 0))
        {
            return null;
        }

        return new EmailAddress(value, domain);
    }

    /// <summary>The domain must equal an allowed domain exactly; sub-domains are not allowed (AC-2).</summary>
    public bool IsOnAllowedDomain(IEnumerable<string> allowedDomains) =>
        allowedDomains.Any(allowed => string.Equals(allowed.Trim(), Domain, StringComparison.OrdinalIgnoreCase));

    public override string ToString() => Value;
}
