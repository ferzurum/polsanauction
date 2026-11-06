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

        // Identity names its tables and indexes explicitly ("AspNetUsers", "UserNameIndex"), which the snake_case
        // convention leaves alone — so name them here (docs/conventions.md; review 0002 round 1, F-11).
        builder.Entity<ApplicationUser>(user =>
        {
            user.ToTable("users");
            user.HasIndex(u => u.NormalizedUserName).HasDatabaseName("ix_users_normalized_user_name");
            user.HasIndex(u => u.NormalizedEmail).HasDatabaseName("ix_users_normalized_email");
            user.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            user.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        });
        builder.Entity<IdentityRole<Guid>>(role =>
        {
            role.ToTable("roles");
            role.HasIndex(r => r.NormalizedName).HasDatabaseName("ix_roles_normalized_name");
        });
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");

        builder.Entity<AllowedEmailDomainRecord>(domain =>
        {
            domain.Property(d => d.Domain).HasMaxLength(253).IsRequired();
            domain.HasIndex(d => d.Domain).IsUnique();
        });
    }
}
