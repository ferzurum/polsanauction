using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PolsanAuction.Api.Health;

namespace PolsanAuction.Api.UnitTests;

public sealed class HealthResponseTests
{
    [Fact]
    public void ToStatus_Healthy_ReturnsHealthy() =>
        Assert.Equal("Healthy", HealthResponse.ToStatus(HealthStatus.Healthy));

    [Theory]
    [InlineData(HealthStatus.Degraded)]
    [InlineData(HealthStatus.Unhealthy)]
    public void ToStatus_NotHealthy_ReturnsUnhealthy(HealthStatus status) =>
        Assert.Equal("Unhealthy", HealthResponse.ToStatus(status));

    [Fact]
    public async Task WriteAsync_UnhealthyWithDetails_WritesOnlyTheStatus()
    {
        var context = new DefaultHttpContext();
        using var body = new MemoryStream();
        context.Response.Body = body;
        var entry = new HealthReportEntry(
            HealthStatus.Unhealthy,
            "Host=db.internal;Password=secret",
            TimeSpan.FromSeconds(3),
            new InvalidOperationException("stack detail"),
            data: null);
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry> { ["database"] = entry },
            TimeSpan.FromSeconds(3));

        await HealthResponse.WriteAsync(context, report);

        Assert.Equal("""{"status":"Unhealthy"}""", Encoding.UTF8.GetString(body.ToArray()));
    }
}
