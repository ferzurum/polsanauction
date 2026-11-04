using Microsoft.EntityFrameworkCore;

namespace PolsanAuction.Identity.Infrastructure;

/// <summary>Fills the allowed-domain list from configuration when it is still empty (spec 0002, R-1).</summary>
internal static class AllowedDomainSeeder
{
    public static async Task SeedAsync(IdentityDbContext db, IEnumerable<string> initialDomains, CancellationToken cancellationToken)
    {
        if (await db.AllowedEmailDomains.AnyAsync(cancellationToken))
        {
            return;
        }

        var domains = initialDomains
            .Select(domain => domain.Trim().ToLowerInvariant())
            .Where(domain => domain.Length > 0)
            .Distinct();
        db.AllowedEmailDomains.AddRange(domains.Select(domain => new AllowedEmailDomainRecord { Domain = domain }));
        await db.SaveChangesAsync(cancellationToken);
    }
}
