using PolsanAuction.Identity.Domain;

namespace PolsanAuction.Identity.UnitTests;

public sealed class RegistrationTests
{
    private static readonly string[] Allowed = ["polsanteknoloji.com.tr"];
    private const string ValidPassword = "on-karakter";

    private static (Registration? Registration, IReadOnlyList<FieldError> Errors) Create(
        string? firstName = "Ayşe Nur",
        string? lastName = "Yılmaz",
        string? email = "ayse@polsanteknoloji.com.tr",
        string? password = ValidPassword,
        string? confirmation = ValidPassword) =>
        Registration.Create(firstName, lastName, email, password, confirmation, Allowed);

    [Fact]
    public void Create_ValidInput_ReturnsNormalizedRegistration_AC1()
    {
        var (registration, errors) = Create(firstName: "  Ayşe Nur  ", lastName: " Yılmaz ", email: " Ayse@PolsanTeknoloji.com.tr ");

        Assert.Empty(errors);
        Assert.NotNull(registration);
        Assert.Equal("Ayşe Nur", registration.FirstName);
        Assert.Equal("Yılmaz", registration.LastName);
        Assert.Equal("ayse@polsanteknoloji.com.tr", registration.Email.Value);
    }

    [Fact]
    public void Create_DomainNotAllowed_ReturnsDomainError_AC2()
    {
        var (registration, errors) = Create(email: "ayse@gmail.com");

        Assert.Null(registration);
        Assert.Equal([new FieldError(Fields.Email, ErrorCodes.DomainNotAllowed)], errors);
    }

    [Theory]
    [InlineData("", Fields.FirstName, ErrorCodes.Required)]
    [InlineData("   ", Fields.FirstName, ErrorCodes.Required)]
    [InlineData(null, Fields.FirstName, ErrorCodes.Required)]
    public void Create_MissingFirstName_IsRejected_AC4(string? firstName, string field, string code) =>
        Assert.Equal([new FieldError(field, code)], Create(firstName: firstName).Errors);

    [Fact]
    public void Create_NameBoundaries_100AcceptedAnd101Rejected_AC4()
    {
        Assert.Empty(Create(lastName: new string('ş', 100)).Errors);
        Assert.Equal([new FieldError(Fields.LastName, ErrorCodes.TooLong)], Create(lastName: new string('ş', 101)).Errors);
        Assert.Empty(Create(lastName: $"  {new string('ş', 100)}  ").Errors);
    }

    [Fact]
    public void Create_PasswordBoundaries_10To128Accepted_AC4()
    {
        string Repeat(int length) => new('x', length);

        Assert.Equal([new FieldError(Fields.Password, ErrorCodes.TooShort)], Create(password: Repeat(9), confirmation: Repeat(9)).Errors);
        Assert.Empty(Create(password: Repeat(10), confirmation: Repeat(10)).Errors);
        Assert.Empty(Create(password: Repeat(128), confirmation: Repeat(128)).Errors);
        Assert.Equal([new FieldError(Fields.Password, ErrorCodes.TooLong)], Create(password: Repeat(129), confirmation: Repeat(129)).Errors);
    }

    [Fact]
    public void Create_PasswordWithoutDigitsOrUppercase_IsAccepted_AC4() =>
        Assert.Empty(Create(password: "uzunbirparola", confirmation: "uzunbirparola").Errors);

    [Fact]
    public void Create_ConfirmationDiffers_IsRejected_AC4() =>
        Assert.Equal(
            [new FieldError(Fields.PasswordConfirmation, ErrorCodes.PasswordMismatch)],
            Create(confirmation: ValidPassword + "!").Errors);

    [Fact]
    public void Create_InvalidEmail_IsRejected_AC4() =>
        Assert.Equal([new FieldError(Fields.Email, ErrorCodes.InvalidEmail)], Create(email: "ayse@").Errors);

    [Fact]
    public void Create_EverythingWrong_ReportsEveryFieldAtOnce_AC4()
    {
        var (registration, errors) = Create(firstName: "", lastName: "", email: "", password: "kısa", confirmation: "başka");

        Assert.Null(registration);
        Assert.Equal(
            [Fields.FirstName, Fields.LastName, Fields.Email, Fields.Password, Fields.PasswordConfirmation],
            errors.Select(error => error.Field));
    }
}
