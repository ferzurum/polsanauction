using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace PolsanAuction.Api.Health;

/// <summary>Reports whether PostgreSQL answers a trivial query within a short timeout (spec 0001, R-2).</summary>
internal sealed class DatabaseHealthCheck(IConfiguration configuration) : IHealthCheck
{
    private const int TimeoutSeconds = 3;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy("Connection string 'Default' is not configured.");
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Timeout = TimeoutSeconds,
            CommandTimeout = TimeoutSeconds,
        };

        try
        {
            await using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (NpgsqlException exception)
        {
            // Description and exception stay in logs; HealthResponse never writes them (no internal details).
            return HealthCheckResult.Unhealthy("Database unreachable.", exception);
        }
    }
}
