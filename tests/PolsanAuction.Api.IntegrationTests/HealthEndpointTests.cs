using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PolsanAuction.Api.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class HealthEndpointTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task Health_DatabaseReachable_ReturnsHealthy()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var factory = HealthApp.Create(postgres.ConnectionString);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/health", UriKind.Relative), cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("""{"status":"Healthy"}""", await response.Content.ReadAsStringAsync(cancellationToken));
    }
}

[Trait("Category", "Integration")]
public sealed class HealthEndpointUnreachableDatabaseTests
{
    [Fact]
    public async Task Health_DatabaseUnreachable_ReturnsUnhealthyWithoutDetails()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        // Port 1 on loopback: nothing listens, so the connection is refused.
        await using var factory = HealthApp.Create("Host=127.0.0.1;Port=1;Username=probe;Password=probe;Database=probe");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/health", UriKind.Relative), cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("""{"status":"Unhealthy"}""", body);
        Assert.DoesNotContain("127.0.0.1", body, StringComparison.Ordinal);
    }
}

internal static class HealthApp
{
    public static WebApplicationFactory<Program> Create(string connectionString) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:Default", connectionString));
}
