namespace PolsanAuction.Identity.Infrastructure;

/// <summary>A row of the allowed e-mail domain list (BR-12). The domain is stored lower-case.</summary>
public sealed class AllowedEmailDomainRecord
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public required string Domain { get; init; }
}
