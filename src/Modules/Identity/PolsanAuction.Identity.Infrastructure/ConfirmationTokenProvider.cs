using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

namespace PolsanAuction.Identity.Infrastructure;

/// <summary>
/// Creates and checks e-mail confirmation tokens: the creation time, account, purpose and security stamp,
/// encrypted and signed with Data Protection. Unlike Identity's built-in provider it reads the time from
/// <see cref="TimeProvider"/>, so the 24-hour expiry is testable without waiting (plan 0002, A-1).
/// </summary>
internal sealed class ConfirmationTokenCodec(IDataProtectionProvider dataProtection, TimeProvider clock, TimeSpan lifetime)
{
    private readonly IDataProtector _protector = dataProtection.CreateProtector("PolsanAuction.Identity.ConfirmationToken.v1");

    public string Create(Guid accountId, string purpose, string securityStamp)
    {
        using var buffer = new MemoryStream();
        using (var writer = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(clock.GetUtcNow().UtcTicks);
            writer.Write(accountId.ToString());
            writer.Write(purpose);
            writer.Write(securityStamp);
        }

        return Convert.ToBase64String(_protector.Protect(buffer.ToArray()));
    }

    /// <summary>True only for an intact token of this account and purpose, unexpired, with the current stamp.</summary>
    public bool IsValid(string token, Guid accountId, string purpose, string securityStamp)
    {
        try
        {
            using var reader = new BinaryReader(new MemoryStream(_protector.Unprotect(Convert.FromBase64String(token))), Encoding.UTF8);
            var created = new DateTimeOffset(reader.ReadInt64(), TimeSpan.Zero);
            var valid = clock.GetUtcNow() <= created + lifetime
                && string.Equals(reader.ReadString(), accountId.ToString(), StringComparison.Ordinal)
                && string.Equals(reader.ReadString(), purpose, StringComparison.Ordinal)
                && string.Equals(reader.ReadString(), securityStamp, StringComparison.Ordinal);
            return valid && reader.BaseStream.Position == reader.BaseStream.Length;
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException or EndOfStreamException)
        {
            return false;
        }
    }
}

/// <summary>Plugs <see cref="ConfirmationTokenCodec"/> into ASP.NET Core Identity as the e-mail confirmation provider.</summary>
internal sealed class ConfirmationTokenProvider(ConfirmationTokenCodec codec) : IUserTwoFactorTokenProvider<ApplicationUser>
{
    public const string Name = "PolsanConfirmation";

    public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<ApplicationUser> manager, ApplicationUser user) =>
        Task.FromResult(false);

    public async Task<string> GenerateAsync(string purpose, UserManager<ApplicationUser> manager, ApplicationUser user) =>
        codec.Create(user.Id, purpose, await manager.GetSecurityStampAsync(user));

    public async Task<bool> ValidateAsync(string purpose, string token, UserManager<ApplicationUser> manager, ApplicationUser user) =>
        codec.IsValid(token, user.Id, purpose, await manager.GetSecurityStampAsync(user));
}
