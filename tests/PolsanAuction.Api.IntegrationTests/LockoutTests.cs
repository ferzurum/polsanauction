using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace PolsanAuction.Api.IntegrationTests;

/// <summary>
/// AC-7. Identity's lockout reads the real clock, not TimeProvider, so the 15-minute expiry is proven by
/// checking the lock end and then moving it into the past in the database (plan 0002, A-1 b). That is why
/// this test deliberately reads the real clock (TimeProvider.System), unlike every other test.
/// </summary>
[Trait("Category", "Integration")]
[Collection(PostgresTestGroup.Name)]
public sealed class LockoutTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task FourFailures_DoNotLock_AndASuccessResetsTheCount_AC7()
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = await app.CreateApiClientAsync();
        var email = await app.CreateConfirmedAccountAsync(client);

        for (var attempt = 0; attempt < 4; attempt++)
        {
            using var failed = await AuthApp.LoginAsync(client, email, "yanlis-parola-1");
            Assert.Equal("auth.invalid_credentials", await AuthApp.ProblemCodeAsync(failed));
        }

        using var success = await AuthApp.LoginAsync(client, email);

        Assert.Equal(HttpStatusCode.NoContent, success.StatusCode);
        Assert.Equal(0, await app.WithDbAsync(db => db.Users.Where(u => u.Email == email).Select(u => u.AccessFailedCount).SingleAsync(Ct)));
    }

    [Fact]
    public async Task FiveFailures_LockFor15Minutes_ThenTheRightPasswordWorks_AC7()
    {
        await using var app = new AuthApp(postgres.ConnectionString);
        using var client = await app.CreateApiClientAsync();
        var email = await app.CreateConfirmedAccountAsync(client);

        for (var attempt = 0; attempt < 4; attempt++)
        {
            using var _ = await AuthApp.LoginAsync(client, email, "yanlis-parola-1");
        }

        var beforeLock = TimeProvider.System.GetUtcNow();
        using var fifth = await AuthApp.LoginAsync(client, email, "yanlis-parola-1");
        var afterLock = TimeProvider.System.GetUtcNow();
        using var rightPasswordWhileLocked = await AuthApp.LoginAsync(client, email);

        Assert.Equal(HttpStatusCode.Forbidden, fifth.StatusCode);
        Assert.Equal("auth.locked_out", await AuthApp.ProblemCodeAsync(fifth));
        var lockedUntil = await LockedUntilAsync(fifth);
        Assert.InRange(lockedUntil, beforeLock.AddMinutes(15), afterLock.AddMinutes(15));
        Assert.Equal("auth.locked_out", await AuthApp.ProblemCodeAsync(rightPasswordWhileLocked));
        Assert.Equal(HttpStatusCode.Unauthorized, (await AuthApp.MeAsync(client)).StatusCode);

        // 15 minutes later: the lock has ended.
        await app.WithDbAsync(db => db.Users.Where(u => u.Email == email)
            .ExecuteUpdateAsync(set => set.SetProperty(u => u.LockoutEnd, TimeProvider.System.GetUtcNow().AddSeconds(-1)), Ct));
        using var afterExpiry = await AuthApp.LoginAsync(client, email);

        Assert.Equal(HttpStatusCode.NoContent, afterExpiry.StatusCode);
    }

    private static async Task<DateTimeOffset> LockedUntilAsync(HttpResponseMessage response)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return problem.RootElement.GetProperty("lockedUntil").GetDateTimeOffset();
    }
}
