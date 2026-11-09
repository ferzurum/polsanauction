using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
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
public sealed partial class AuthApp(string connectionString, IReadOnlyDictionary<string, string>? settings = null)
    : WebApplicationFactory<Program>
{
    public const string AllowedDomain = "polsanteknoloji.com.tr";
    public const string Password = "on-karakter-parola";
    public const string AntiforgeryHeader = "X-XSRF-TOKEN";

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero));

    public FakeEmailSender Emails { get; } = new();

    /// <summary>A unique address on the allowed domain, so tests sharing the database never collide.</summary>
    public static string NewEmail(string prefix = "user") => $"{prefix}-{Guid.NewGuid():N}@{AllowedDomain}";

    public static object SignUp(string email, string firstName = "Ayşe", string lastName = "Yılmaz", string password = Password) =>
        new { firstName, lastName, email, password, passwordConfirmation = password };

    public static async Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email) =>
        await client.PostAsJsonAsync("/api/auth/register", SignUp(email), TestContext.Current.CancellationToken);

    /// <summary>
    /// A cookie-keeping client on https (the auth cookies are Secure) that already carries the anti-forgery header.
    /// </summary>
    public async Task<HttpClient> CreateApiClientAsync()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        await RefreshAntiforgeryAsync(client);
        return client;
    }

    /// <summary>The anti-forgery token is bound to the user, so the SPA (and tests) fetch a new one after sign-in/out.</summary>
    public static async Task RefreshAntiforgeryAsync(HttpClient client)
    {
        using var response = await client.GetAsync(new Uri("/api/auth/antiforgery", UriKind.Relative), TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        client.DefaultRequestHeaders.Remove(AntiforgeryHeader);
        client.DefaultRequestHeaders.Add(AntiforgeryHeader, body.RootElement.GetProperty("token").GetString());
    }

    /// <summary>Registers and confirms an account; returns its e-mail.</summary>
    public async Task<string> CreateConfirmedAccountAsync(HttpClient client, string? email = null)
    {
        email ??= NewEmail();
        using var registered = await RegisterAsync(client, email);
        registered.EnsureSuccessStatusCode();
        var (account, token) = LatestConfirmationLink(email);
        using var confirmed = await client.PostAsJsonAsync("/api/auth/confirm-email", new { accountId = account, token }, TestContext.Current.CancellationToken);
        confirmed.EnsureSuccessStatusCode();
        return email;
    }

    public static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password = Password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password }, TestContext.Current.CancellationToken);

    public static Task<HttpResponseMessage> MeAsync(HttpClient client) =>
        client.GetAsync(new Uri("/api/auth/me", UriKind.Relative), TestContext.Current.CancellationToken);

    public static async Task<string?> ProblemCodeAsync(HttpResponseMessage response)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return problem.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

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

        // Every request in a test comes from the same source, so the per-source limits are high unless a test
        // sets the real one (plan 0002, risk 4); no test account matches the configured initial admin.
        builder.UseSetting("Identity:RegisterAttemptsPerHour", "1000");
        builder.UseSetting("Identity:LoginAttemptsPerMinute", "1000");
        builder.UseSetting("Identity:ResendAttemptsPerMinute", "1000");
        builder.UseSetting("Identity:InitialAdminEmail", string.Empty);
        foreach (var (key, value) in settings ?? new Dictionary<string, string>())
        {
            builder.UseSetting(key, value);
        }

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
