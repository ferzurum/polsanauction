using Microsoft.AspNetCore.Antiforgery;
using PolsanAuction.Identity.Application;

namespace PolsanAuction.Api.Auth;

/// <summary>Sign-up, e-mail confirmation and session endpoints (spec 0002). Every POST is CSRF-protected.</summary>
internal static class AuthEndpoints
{
    public const string ConfirmationInvalid = "confirmation.invalid";
    public const string InvalidCredentials = "auth.invalid_credentials";
    public const string EmailNotConfirmed = "auth.email_not_confirmed";
    public const string LockedOut = "auth.locked_out";

    public sealed record ConfirmEmailRequest(Guid AccountId, string? Token);

    public sealed record EmailRequest(string? Email);

    public sealed record LoginRequest(string? Email, string? Password);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").AddEndpointFilter<AntiforgeryFilter>();

        // The SPA fetches a fresh token at start-up and again after every sign-in or sign-out.
        auth.MapGet("/antiforgery", (HttpContext context, IAntiforgery antiforgery) =>
            Results.Ok(new { token = antiforgery.GetAndStoreTokens(context).RequestToken }))
            .AllowAnonymous();

        // 202 for every accepted request, new or existing address alike (spec 0002, R-3).
        auth.MapPost("/register", async (RegisterUserRequest request, RegisterUser handler, CancellationToken cancellationToken) =>
        {
            var errors = await handler.HandleAsync(request, cancellationToken);
            return errors.Count == 0 ? Results.Accepted() : Problems.Validation(errors);
        }).AllowAnonymous();

        auth.MapPost("/confirm-email", async (ConfirmEmailRequest request, ConfirmEmail handler, CancellationToken cancellationToken) =>
            await handler.HandleAsync(request.AccountId, request.Token, cancellationToken) == ConfirmationOutcome.Confirmed
                ? Results.NoContent()
                : Problems.Code(StatusCodes.Status400BadRequest, ConfirmationInvalid, "The confirmation link is invalid, expired or already used."))
            .AllowAnonymous();

        // Same answer whether or not the address exists or is already confirmed (no account discovery).
        auth.MapPost("/resend-confirmation", async (EmailRequest request, ResendConfirmation handler, CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(request.Email, cancellationToken);
            return Results.Accepted();
        }).AllowAnonymous();

        auth.MapPost("/login", async (LoginRequest request, IAccountSignIn signIn, CancellationToken cancellationToken) =>
        {
            var outcome = await signIn.SignInAsync(request.Email, request.Password, cancellationToken);
            return outcome.Status switch
            {
                SignInStatus.Succeeded => Results.NoContent(),
                SignInStatus.EmailNotConfirmed => Problems.Code(StatusCodes.Status403Forbidden, EmailNotConfirmed, "The e-mail address is not confirmed yet."),
                SignInStatus.LockedOut => Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "The account is temporarily locked.",
                    extensions: new Dictionary<string, object?> { ["code"] = LockedOut, ["lockedUntil"] = outcome.LockedUntil }),
                _ => Problems.Code(StatusCodes.Status401Unauthorized, InvalidCredentials, "E-mail or password is incorrect."),
            };
        }).AllowAnonymous();

        auth.MapPost("/logout", async (IAccountSignIn signIn) =>
        {
            await signIn.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        auth.MapGet("/me", async (HttpContext context, IAccountSignIn signIn, CancellationToken cancellationToken) =>
            await signIn.GetCurrentAsync(context.User, cancellationToken) is { } account
                ? Results.Ok(account)
                : Results.Unauthorized())
            .RequireAuthorization();

        return app;
    }
}
