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

    /// <summary>Minimum time between two confirmation resends to one address (spec 0002, AC-6).</summary>
    public TimeSpan ResendInterval { get; set; } = TimeSpan.FromMinutes(1);
}
