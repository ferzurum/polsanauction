using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PolsanAuction.Api.Health;

/// <summary>Writes the health result as a machine-readable status only — never descriptions or exceptions.</summary>
public static class HealthResponse
{
    public const string Healthy = "Healthy";
    public const string Unhealthy = "Unhealthy";

    public static string ToStatus(HealthStatus status) => status == HealthStatus.Healthy ? Healthy : Unhealthy;

    public static Task WriteAsync(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(new HealthStatusBody(ToStatus(report.Status)));

    private sealed record HealthStatusBody(string Status);
}
