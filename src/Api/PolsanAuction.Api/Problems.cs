using PolsanAuction.Identity.Domain;

namespace PolsanAuction.Api;

/// <summary>
/// RFC 9457 ProblemDetails with a machine-readable <c>code</c> (docs/conventions.md, Error handling).
/// The web maps codes to Turkish messages; nothing internal is ever written here.
/// </summary>
internal static class Problems
{
    public const string ValidationCode = "validation";

    public static IResult Validation(IReadOnlyList<FieldError> errors) => Results.Problem(
        statusCode: StatusCodes.Status400BadRequest,
        title: "Invalid input.",
        extensions: new Dictionary<string, object?>
        {
            ["code"] = ValidationCode,
            ["errors"] = errors
                .GroupBy(error => error.Field)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Code).ToArray()),
        });

    public static IResult Code(int statusCode, string code, string title) => Results.Problem(
        statusCode: statusCode,
        title: title,
        extensions: new Dictionary<string, object?> { ["code"] = code });
}
