using PolsanAuction.Identity.Domain;

namespace PolsanAuction.Identity.UnitTests;

public sealed class EmailAddressTests
{
    private static readonly string[] Allowed = ["polsanteknoloji.com.tr"];

    [Theory]
    [InlineData("  ayse.yilmaz@polsanteknoloji.com.tr  ")]
    [InlineData("Ayse.Yilmaz@PolsanTeknoloji.com.tr")]
    [InlineData("AYSE.YILMAZ@POLSANTEKNOLOJI.COM.TR")]
    public void TryCreate_SurroundingSpacesAndCase_NormalizeToTheSameAddress_AC1(string input)
    {
        var address = EmailAddress.TryCreate(input);

        Assert.NotNull(address);
        Assert.Equal("ayse.yilmaz@polsanteknoloji.com.tr", address.Value);
        Assert.Equal("polsanteknoloji.com.tr", address.Domain);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-sign.polsanteknoloji.com.tr")]
    [InlineData("@polsanteknoloji.com.tr")]
    [InlineData("two@@polsanteknoloji.com.tr")]
    [InlineData("a@b@polsanteknoloji.com.tr")]
    [InlineData("in side@polsanteknoloji.com.tr")]
    [InlineData("user@localhost")]
    [InlineData("user@polsanteknoloji..com.tr")]
    [InlineData("user@")]
    public void TryCreate_MalformedInput_ReturnsNull_AC4(string? input) =>
        Assert.Null(EmailAddress.TryCreate(input));

    [Fact]
    public void TryCreate_LocalPartLongerThan64_ReturnsNull_AC4() =>
        Assert.Null(EmailAddress.TryCreate($"{new string('a', 65)}@polsanteknoloji.com.tr"));

    [Fact]
    public void IsOnAllowedDomain_ExactDomain_IsAllowed_AC2() =>
        Assert.True(EmailAddress.TryCreate("ali@polsanteknoloji.com.tr")!.IsOnAllowedDomain(Allowed));

    [Theory]
    [InlineData("ali@mail.polsanteknoloji.com.tr")]
    [InlineData("ali@polsanteknoloji.com")]
    [InlineData("ali@xpolsanteknoloji.com.tr")]
    [InlineData("ali@gmail.com")]
    public void IsOnAllowedDomain_OtherOrSubDomain_IsRejected_AC2(string input) =>
        Assert.False(EmailAddress.TryCreate(input)!.IsOnAllowedDomain(Allowed));

    [Fact]
    public void IsOnAllowedDomain_AllowedListInAnyCase_StillMatches_AC2() =>
        Assert.True(EmailAddress.TryCreate("ali@polsanteknoloji.com.tr")!.IsOnAllowedDomain([" PolsanTeknoloji.com.tr "]));
}
