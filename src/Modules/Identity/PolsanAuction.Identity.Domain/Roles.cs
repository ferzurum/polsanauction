namespace PolsanAuction.Identity.Domain;

/// <summary>Application roles (docs/domain.md: Admin, Personel). Every new account is a Bidder (spec 0002, R-8).</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Bidder = "Bidder";
}
