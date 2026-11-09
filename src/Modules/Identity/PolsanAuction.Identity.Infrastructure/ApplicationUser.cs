using Microsoft.AspNetCore.Identity;

namespace PolsanAuction.Identity.Infrastructure;

/// <summary>An employee account. Identity owns the credentials; names are kept for display only.</summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser()
    {
        Id = Guid.CreateVersion7();
        SecurityStamp = Guid.NewGuid().ToString();
    }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;
}
