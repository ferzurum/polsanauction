namespace PolsanAuction.Identity.Infrastructure;

/// <summary>The <c>Identity</c> configuration section (appsettings.json). Contains no secrets.</summary>
public sealed class IdentitySettings
{
    public const string Section = "Identity";

    /// <summary>Whoever confirms this address becomes Admin; no default account or password exists (spec 0002, R-8).</summary>
    public string? InitialAdminEmail { get; set; }

    public string[] InitialAllowedDomains { get; set; } = [];

    /// <summary>Sign-up attempts per source per hour (spec 0002, AC-6).</summary>
    public int RegisterAttemptsPerHour { get; set; } = 10;

    /// <summary>Sign-in attempts per source per minute (docs/security.md; review 0002 round 1, F-1).</summary>
    public int LoginAttemptsPerMinute { get; set; } = 30;

    /// <summary>Minimum time between two confirmation resends to one address (spec 0002, AC-6).</summary>
    public TimeSpan ResendInterval { get; set; } = TimeSpan.FromMinutes(1);
}
