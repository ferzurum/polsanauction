using Microsoft.AspNetCore.Antiforgery;

namespace PolsanAuction.Api.Auth;

/// <summary>
/// CSRF protection for the cookie-authenticated SPA (docs/security.md): every state-changing request must carry
/// the token from <c>GET /api/auth/antiforgery</c> in the <c>X-XSRF-TOKEN</c> header.
/// </summary>
internal sealed class AntiforgeryFilter(IAntiforgery antiforgery) : IEndpointFilter
{
    public const string HeaderName = "X-XSRF-TOKEN";
    public const string InvalidCode = "csrf.invalid";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var method = context.HttpContext.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method))
        {
            return await next(context);
        }

        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Problems.Code(StatusCodes.Status400BadRequest, InvalidCode, "Missing or invalid anti-forgery token.");
        }

        return await next(context);
    }
}
