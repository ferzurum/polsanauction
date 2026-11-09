using PolsanAuction.Identity.Application;

namespace PolsanAuction.Api.Auth;

/// <summary>Links in e-mails point at the web application (<c>App:WebBaseUrl</c>), whose pages call the API.</summary>
internal sealed class AccountLinks(IConfiguration configuration) : IAccountLinks
{
    private Uri WebBase => new(configuration["App:WebBaseUrl"] ?? "http://localhost:5173");

    public Uri Confirmation(Guid accountId, string token) =>
        new(WebBase, $"/dogrula?hesap={accountId}&kod={Uri.EscapeDataString(token)}");

    public Uri Login() => new(WebBase, "/giris");
}
