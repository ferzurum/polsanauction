using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PolsanAuction.Identity.Infrastructure;

/// <summary>Identity module storage; owns the <c>identity</c> schema only (docs/architecture.md).</summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public const string Schema = "identity";

    public DbSet<AllowedEmailDomainRecord> AllowedEmailDomains => Set<AllowedEmailDomainRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Schema);

        builder.Entity<ApplicationUser>(user =>
        {
            user.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            user.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        });

        builder.Entity<AllowedEmailDomainRecord>(domain =>
        {
            domain.Property(d => d.Domain).HasMaxLength(253).IsRequired();
            domain.HasIndex(d => d.Domain).IsUnique();
        });
    }
}
