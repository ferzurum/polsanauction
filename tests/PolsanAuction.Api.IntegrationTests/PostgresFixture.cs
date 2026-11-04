using Testcontainers.PostgreSql;

namespace PolsanAuction.Api.IntegrationTests;

/// <summary>
/// A fresh PostgreSQL 18 container per test class, disposed when the class finishes — also on failure
/// (docs/testing.md, Resource hygiene rule 5). Ryuk removes it if the run is killed halfway.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
