using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PolsanAuction.Identity.Infrastructure;

/// <summary>Lets <c>dotnet ef migrations add</c> build the model without a running app or database.</summary>
internal sealed class DesignTimeIdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args) =>
        new(IdentityModule.Configure(new DbContextOptionsBuilder<IdentityDbContext>(), "Host=design-time").Options);
}
