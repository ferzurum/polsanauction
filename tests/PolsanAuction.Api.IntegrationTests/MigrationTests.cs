using Microsoft.EntityFrameworkCore;

namespace PolsanAuction.Api.IntegrationTests;

[Trait("Category", "Integration")]
[Collection(PostgresTestGroup.Name)]
public sealed class MigrationTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Model_HasNoChangesMissingFromMigrations()
    {
        await using var app = new AuthApp(postgres.ConnectionString);

        var pending = await app.WithDbAsync(db => Task.FromResult(db.Database.HasPendingModelChanges()));

        Assert.False(pending, "The model changed without a migration: run dotnet ef migrations add.");
    }

    [Fact]
    public async Task Startup_AppliesMigrationsAndSeedsTheInitialAllowedDomain()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = app.CreateClient();

        var pendingMigrations = await app.WithDbAsync(async db => (await db.Database.GetPendingMigrationsAsync(cancellationToken)).Count());
        var domains = await app.WithDbAsync(db => db.AllowedEmailDomains.Select(d => d.Domain).ToListAsync(cancellationToken));

        Assert.Equal(0, pendingMigrations);
        Assert.Contains(AuthApp.AllowedDomain, domains);
    }
}
