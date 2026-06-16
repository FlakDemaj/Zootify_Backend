using Zootify_Backend.DTOs.Admin.Request;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Admin;

/// <summary>
/// Verifies that loan confirmation and loan return emails are sent in the borrower's stored language
/// preference (<c>UserSettings.Language</c>). The service passes that language explicitly to
/// <c>IBrevoProvider</c>, so the test simply verifies the language argument the provider receives —
/// it no longer depends on the request's <c>Accept-Language</c> header at all.
/// </summary>
public class AdminLoanControllerEmailLanguageTests : IntegrationTestBase
{
    public AdminLoanControllerEmailLanguageTests(IntegrationTestFixture fixture) : base(fixture) { }

    /// <summary>
    /// Tests Create Loan – Passes Borrower Language To Email.
    /// </summary>
    [Theory]
    [InlineData("en")]
    [InlineData("de")]
    public async Task CreateLoan_PassesBorrowerLanguageToEmail(string borrowerLanguage)
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var borrower = await UserRepo.CreateUserAsync("borrower@test.de");
        await CreateUserSettingsInDbAsync(borrower.Guid, language: borrowerLanguage);
        AuthenticateAs(admin);

        var geo = await SpecimenRepo.CreateGeographicLocationAsync();
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid);

        Fixture.BrevoMock
            .Setup(b => b.SendLoanConfirmationEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(true);

        var request = new CreateLoanRequestDto
        {
            SpecimenGuid = specimen.Guid,
            BorrowerGuid = borrower.Guid,
            DueDate = DateTime.UtcNow.AddDays(30),
            Notes = "Handle with care.",
        };

        var response = await Client.PostAsJsonAsync(Routings.AdminLoanRouting, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Fixture.BrevoMock.Verify(b => b.SendLoanConfirmationEmail(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
            It.IsAny<string?>(), borrowerLanguage), Times.Once);
    }

    /// <summary>
    /// Tests Return Loan – Passes Borrower Language To Email.
    /// </summary>
    [Theory]
    [InlineData("en")]
    [InlineData("de")]
    public async Task ReturnLoan_PassesBorrowerLanguageToEmail(string borrowerLanguage)
    {
        var admin = await UserRepo.CreateUserAsync("admin@test.de", role: UserRole.Admin);
        var borrower = await UserRepo.CreateUserAsync("borrower@test.de");
        await CreateUserSettingsInDbAsync(borrower.Guid, language: borrowerLanguage);
        AuthenticateAs(admin);

        var geo = await SpecimenRepo.CreateGeographicLocationAsync();
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("leo", TaxonomyRank.Species);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid);
        var loan = await LoanRepo.CreateLoanAsync(
            specimen.Guid, admin.Guid, borrower.Guid,
            dueDate: DateTime.UtcNow.AddDays(14));

        Fixture.BrevoMock
            .Setup(b => b.SendLoanReturnConfirmationEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(true);

        var request = new ReturnLoanRequestDto();

        var response = await Client.PostAsJsonAsync(
            $"{Routings.AdminLoanRouting}/{loan.Guid}/return", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Fixture.BrevoMock.Verify(b => b.SendLoanReturnConfirmationEmail(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string?>(), borrowerLanguage), Times.Once);
    }
}
