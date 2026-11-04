using Testcontainers.PostgreSql;

namespace PolsanAuction.Api.IntegrationTests;

/// <summary>
/// One fresh PostgreSQL 18 container per test run, shared by the <see cref="PostgresTestGroup"/> and disposed
/// when the run ends — also on failure (docs/testing.md, Resource hygiene rule 5). Ryuk removes it if the run
/// is killed halfway. Tests isolate their data with unique e-mail addresses (see <see cref="AuthApp.NewEmail"/>).
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}

/// <summary>Every test class that needs the database joins this collection, so they run one after another.</summary>
[CollectionDefinition(Name)]
public sealed class PostgresTestGroup : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
