namespace PolsanAuction.Api.Admin;

/// <summary>The admin area. Spec 0002 only guards it; its content arrives with spec 0003.</summary>
internal static class AdminEndpoints
{
    public const string Policy = "Admin";

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        // The role check runs on the server; hiding the link in the web is not authorization (docs/security.md).
        app.MapGroup("/api/admin")
            .RequireAuthorization(Policy)
            .MapGet("/", () => Results.Ok(new { area = "admin" }));
        return app;
    }
}
