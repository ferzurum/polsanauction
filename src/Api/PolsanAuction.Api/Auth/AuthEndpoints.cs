using PolsanAuction.Identity.Application;

namespace PolsanAuction.Api.Auth;

/// <summary>Sign-up and e-mail confirmation (spec 0002). Anonymous by design (docs/security.md).</summary>
internal static class AuthEndpoints
{
    public const string ConfirmationInvalid = "confirmation.invalid";

    public sealed record ConfirmEmailRequest(Guid AccountId, string? Token);

    public sealed record ResendConfirmationRequest(string? Email);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").AllowAnonymous();

        // 202 for every accepted request, new or existing address alike (spec 0002, R-3).
        auth.MapPost("/register", async (RegisterUserRequest request, RegisterUser handler, CancellationToken cancellationToken) =>
        {
            var errors = await handler.HandleAsync(request, cancellationToken);
            return errors.Count == 0 ? Results.Accepted() : Problems.Validation(errors);
        });

        auth.MapPost("/confirm-email", async (ConfirmEmailRequest request, ConfirmEmail handler, CancellationToken cancellationToken) =>
            await handler.HandleAsync(request.AccountId, request.Token, cancellationToken) == ConfirmationOutcome.Confirmed
                ? Results.NoContent()
                : Problems.Code(StatusCodes.Status400BadRequest, ConfirmationInvalid, "The confirmation link is invalid, expired or already used."));

        // Same answer whether or not the address exists or is already confirmed (no account discovery).
        auth.MapPost("/resend-confirmation", async (ResendConfirmationRequest request, ResendConfirmation handler, CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(request.Email, cancellationToken);
            return Results.Accepted();
        });

        return app;
    }
}
