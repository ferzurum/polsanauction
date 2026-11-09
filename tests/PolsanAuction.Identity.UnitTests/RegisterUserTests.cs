using PolsanAuction.Identity.Application;
using PolsanAuction.Identity.Domain;
using PolsanAuction.Notification.Application;

namespace PolsanAuction.Identity.UnitTests;

public sealed class RegisterUserTests
{
    private const string Password = "on-karakter";
    private readonly FakeAccounts _accounts = new();
    private readonly RecordingEmailSender _email = new();
    private readonly RegisterUser _handler;

    public RegisterUserTests() => _handler = new RegisterUser(_accounts, _email, new FixedLinks());

    private static RegisterUserRequest Request(string email = "ayse@polsanteknoloji.com.tr") =>
        new("Ayşe", "Yılmaz", email, Password, Password);

    [Fact]
    public async Task HandleAsync_NewAddress_CreatesAccountAndSendsConfirmation_AC1()
    {
        var errors = await _handler.HandleAsync(Request(), CancellationToken.None);

        Assert.Empty(errors);
        Assert.Single(_accounts.Created);
        var sent = Assert.Single(_email.Sent);
        Assert.Contains("doğrulayın", sent.Subject, StringComparison.Ordinal);
        Assert.Contains("https://web.test/dogrula", sent.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_InvalidRequest_CreatesNothingAndSendsNothing_AC4()
    {
        var errors = await _handler.HandleAsync(Request("ayse@gmail.com"), CancellationToken.None);

        Assert.Equal([new FieldError(Fields.Email, ErrorCodes.DomainNotAllowed)], errors);
        Assert.Empty(_accounts.Created);
        Assert.Empty(_email.Sent);
    }

    [Fact]
    public async Task HandleAsync_ExistingAddress_OnlyNotifiesTheOwner_AC3()
    {
        _accounts.Existing = new AccountSummary(Guid.CreateVersion7(), "Ayşe", EmailAddress.TryCreate("ayse@polsanteknoloji.com.tr")!, true);

        var errors = await _handler.HandleAsync(Request(), CancellationToken.None);

        Assert.Empty(errors);
        Assert.Empty(_accounts.Created);
        Assert.Contains("zaten bir hesabınız var", Assert.Single(_email.Sent).Subject, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_ExistingAddress_StillSpendsAPasswordHash_SoTimingRevealsNothing()
    {
        _accounts.Existing = new AccountSummary(Guid.CreateVersion7(), "Ayşe", EmailAddress.TryCreate("ayse@polsanteknoloji.com.tr")!, true);

        await _handler.HandleAsync(Request(), CancellationToken.None);

        Assert.Equal([Password], _accounts.TimingOnlyHashes);
    }

    [Fact]
    public async Task HandleAsync_NewAddress_HashesOnlyOnce_WhileCreatingTheAccount()
    {
        await _handler.HandleAsync(Request(), CancellationToken.None);

        Assert.Empty(_accounts.TimingOnlyHashes);
    }

    [Fact]
    public async Task HandleAsync_AddressTakenConcurrently_AnswersAsForAnExistingAccount_AC3()
    {
        _accounts.LoseCreationRace = true;

        var errors = await _handler.HandleAsync(Request(), CancellationToken.None);

        Assert.Empty(errors);
        Assert.Contains("zaten bir hesabınız var", Assert.Single(_email.Sent).Subject, StringComparison.Ordinal);
    }

    private sealed class FakeAccounts : IUserAccounts
    {
        public AccountSummary? Existing { get; set; }

        public bool LoseCreationRace { get; set; }

        public List<Registration> Created { get; } = [];

        public List<string> TimingOnlyHashes { get; } = [];

        public void HashPasswordForTimingOnly(string password) => TimingOnlyHashes.Add(password);

        public Task<IReadOnlyList<string>> GetAllowedDomainsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>(["polsanteknoloji.com.tr"]);

        public Task<AccountSummary?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken) =>
            Task.FromResult(Existing);

        public Task<AccountSummary?> CreatePendingAsync(Registration registration, CancellationToken cancellationToken)
        {
            if (LoseCreationRace)
            {
                return Task.FromResult<AccountSummary?>(null);
            }

            Created.Add(registration);
            return Task.FromResult<AccountSummary?>(new AccountSummary(Guid.CreateVersion7(), registration.FirstName, registration.Email, false));
        }

        public Task<string> CreateConfirmationTokenAsync(Guid accountId, CancellationToken cancellationToken) =>
            Task.FromResult("kod");

        public Task<ConfirmationOutcome> ConfirmEmailAsync(Guid accountId, string token, CancellationToken cancellationToken) =>
            Task.FromResult(ConfirmationOutcome.Invalid);
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public List<EmailMessage> Sent { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedLinks : IAccountLinks
    {
        public Uri Confirmation(Guid accountId, string token) => new($"https://web.test/dogrula?hesap={accountId}&kod={token}");

        public Uri Login() => new("https://web.test/giris");
    }
}
