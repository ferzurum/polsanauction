namespace PolsanAuction.Notification.Application;

/// <summary>Turkish texts of the e-mails the Identity module sends (spec 0002, R-2, R-3).</summary>
public static class IdentityEmails
{
    public static EmailMessage Confirmation(string to, string firstName, Uri confirmationLink) => new(
        to,
        "Polsan Auction — e-posta adresinizi doğrulayın",
        $"""
        Merhaba {firstName},

        Polsan Auction hesabınızı etkinleştirmek için aşağıdaki bağlantıyı açın:
        {confirmationLink}

        Bağlantı 24 saat geçerlidir ve yalnızca bir kez kullanılabilir.
        Bu kaydı siz yapmadıysanız bu e-postayı yok sayabilirsiniz.
        """);

    public static EmailMessage AlreadyRegistered(string to, string firstName, Uri loginLink) => new(
        to,
        "Polsan Auction — zaten bir hesabınız var",
        $"""
        Merhaba {firstName},

        Bu e-posta adresiyle yeniden kayıt olunmak istendi, ancak zaten bir hesabınız var.
        Giriş yapmak için: {loginLink}

        Bu isteği siz yapmadıysanız bu e-postayı yok sayabilirsiniz; hesabınızda bir değişiklik yapılmadı.
        """);
}
