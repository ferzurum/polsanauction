using Microsoft.Extensions.Logging;

namespace PolsanAuction.Identity.Infrastructure;

/// <summary>Identity log messages. They never contain names or e-mail addresses (spec 0002, R-9).</summary>
internal static partial class IdentityLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Initial admin check skipped: the database is not reachable.")]
    public static partial void InitialAdminCheckSkipped(ILogger logger, Exception exception);
}
