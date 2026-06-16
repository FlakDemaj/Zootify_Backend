using Zootify_Backend.DTOs.Admin.Request;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Admin;

/// <summary>
/// Verifies that loan emails are accompanied by an in-app push notification to the borrower, that
/// both honor the borrower's notification settings (<c>SendEmail</c> / <c>SendPushNotification</c>),
/// and that the push message is localized to the borrower's language.
/// </summary>
public class AdminLoanControllerNotificationTests : IntegrationTestBase
{
    public AdminLoanControllerNotificationTests(IntegrationTestFixture fixture) : base(fixture) { }

    private void SetupBrevoReturnsTrue()
    {
        Fixture.BrevoMock
            .Setup(b => b.SendLoanConfirmationEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(true);
        Fixture.BrevoMock
            .Setup(b => b.SendLoanReturnConfirmationEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(true);
    }

    private async Task<(UserEntity admin, UserEntity borrower, SpecimenEntity specimen)> ArrangeAsync(
        bool sendEmail = true, bool sendPush = true, string language = "de")
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var borrower = await UserRepo.CreateUserAsync("borrower@test.de");
        await CreateUserSettingsInDbAsync(borrower.Guid, sendEmail: sendEmail, sendPush: sendPush, language: language);
        AuthenticateAs(admin);

        var geo = await SpecimenRepo.CreateGeographicLocationAsync();
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid);
        return (admin, borrower, specimen);
    }

    /// <summary>
    /// Tests Create Loan – Sends Push To Borrower In Borrower Language.
    /// </summary>
    [Theory]
    [InlineData("en", "created")]
    [InlineData("de", "erstellt")]
    public async Task CreateLoan_SendsPushToBorrowerInBorrowerLanguage(string language, string keyword)
    {
        SetupBrevoReturnsTrue();
        var (_, borrower, specimen) = await ArrangeAsync(language: language);

        var request = new CreateLoanRequestDto
        {
            SpecimenGuid = specimen.Guid,
            BorrowerGuid = borrower.Guid,
            DueDate = DateTime.UtcNow.AddDays(30),
        };

        var response = await Client.PostAsJsonAsync(Routings.AdminLoanRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        VerifyPushNotificationSent(keyword, Times.Once());
    }

    /// <summary>
    /// Tests Create Loan – Email Disabled – Sends Push But No Email.
    /// </summary>
    [Fact]
    public async Task CreateLoan_EmailDisabled_SendsPushButNoEmail()
    {
        SetupBrevoReturnsTrue();
        var (_, borrower, specimen) = await ArrangeAsync(sendEmail: false);

        var request = new CreateLoanRequestDto
        {
            SpecimenGuid = specimen.Guid,
            BorrowerGuid = borrower.Guid,
            DueDate = DateTime.UtcNow.AddDays(30),
        };

        var response = await Client.PostAsJsonAsync(Routings.AdminLoanRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        VerifyPushNotificationSent("erstellt", Times.Once());
        Fixture.BrevoMock.Verify(b => b.SendLoanConfirmationEmail(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
            It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    /// <summary>
    /// Tests Create Loan – Push Disabled – Sends Email But No Push.
    /// </summary>
    [Fact]
    public async Task CreateLoan_PushDisabled_SendsEmailButNoPush()
    {
        SetupBrevoReturnsTrue();
        var (_, borrower, specimen) = await ArrangeAsync(sendPush: false);

        var request = new CreateLoanRequestDto
        {
            SpecimenGuid = specimen.Guid,
            BorrowerGuid = borrower.Guid,
            DueDate = DateTime.UtcNow.AddDays(30),
        };

        var response = await Client.PostAsJsonAsync(Routings.AdminLoanRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        VerifyNoPushNotificationSent();
        Fixture.BrevoMock.Verify(b => b.SendLoanConfirmationEmail(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
            It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    /// <summary>
    /// Tests Return Loan – Sends Push To Borrower In Borrower Language.
    /// </summary>
    [Theory]
    [InlineData("en", "returned")]
    [InlineData("de", "zurückgegeben")]
    public async Task ReturnLoan_SendsPushToBorrowerInBorrowerLanguage(string language, string keyword)
    {
        SetupBrevoReturnsTrue();
        var (admin, borrower, specimen) = await ArrangeAsync(language: language);
        var loan = await LoanRepo.CreateLoanAsync(
            specimen.Guid, admin.Guid, borrower.Guid,
            dueDate: DateTime.UtcNow.AddDays(14));

        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminLoanRouting}/{loan.Guid}/return", new ReturnLoanRequestDto());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        VerifyPushNotificationSent(keyword, Times.Once());
    }
}
