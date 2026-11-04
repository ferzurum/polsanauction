using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using PolsanAuction.Identity.Infrastructure;
using PolsanAuction.Notification.Application;

namespace PolsanAuction.Api.IntegrationTests;

/// <summary>
/// The API hosted against the shared test database: migrations on startup, a fixed fake clock
/// (docs/testing.md, Determinism) and a fake e-mail sender that records instead of sending.
/// </summary>
public sealed partial class AuthApp(string connectionString) : WebApplicationFactory<Program>
{
    public const string AllowedDomain = "polsanteknoloji.com.tr";
    public const string Password = "on-karakter-parola";

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero));

    public FakeEmailSender Emails { get; } = new();

    /// <summary>A unique address on the allowed domain, so tests sharing the database never collide.</summary>
    public static string NewEmail(string prefix = "user") => $"{prefix}-{Guid.NewGuid():N}@{AllowedDomain}";

    public static object SignUp(string email, string firstName = "Ayşe", string lastName = "Yılmaz", string password = Password) =>
        new { firstName, lastName, email, password, passwordConfirmation = password };

    public static async Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email) =>
        await client.PostAsJsonAsync("/api/auth/register", SignUp(email), TestContext.Current.CancellationToken);

    public async Task<T> WithDbAsync<T>(Func<IdentityDbContext, Task<T>> query)
    {
        await using var scope = Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<IdentityDbContext>());
    }

    /// <summary>Account id and token from the latest confirmation e-mail sent to <paramref name="email"/>.</summary>
    public (Guid AccountId, string Token) LatestConfirmationLink(string email)
    {
        var message = Emails.Sent.Last(m => m.To == email && m.Subject.Contains("doğrulayın", StringComparison.Ordinal));
        var match = ConfirmationLink().Match(message.Body);
        Assert.True(match.Success, "The confirmation e-mail carries no link.");
        return (Guid.Parse(match.Groups["account"].Value), Uri.UnescapeDataString(match.Groups["token"].Value));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", connectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
        });
    }

    [GeneratedRegex(@"/dogrula\?hesap=(?<account>[0-9a-f-]{36})&kod=(?<token>\S+)")]
    private static partial Regex ConfirmationLink();
}

/// <summary>Records e-mails instead of sending them (docs/testing.md: third-party services are faked).</summary>
public sealed class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public IReadOnlyList<EmailMessage> Sent => [.. _sent];

    public IReadOnlyList<EmailMessage> To(string email) => [.. _sent.Where(message => message.To == email)];

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }
}
