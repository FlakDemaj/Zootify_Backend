using Microsoft.Extensions.DependencyInjection;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Jobs;

/// <summary>
/// Verifies the language of the loan due-date reminder email.
///
/// The reminder is produced by a background job (<c>LoanReminderService</c>) which passes each
/// recipient's <c>UserSettings.Language</c> explicitly to <c>IBrevoProvider</c>, so the borrower and
/// lender each receive the reminder in their own preferred language. The tests verify the language
/// argument the provider receives for each recipient (distinguished by the <c>isBorrower</c> flag).
/// </summary>
public class LoanReminderEmailLanguageTests : IntegrationTestBase
{
    public LoanReminderEmailLanguageTests(IntegrationTestFixture fixture) : base(fixture) { }

    private void SetupBrevoMock()
    {
        Fixture.BrevoMock
            .Setup(b => b.SendLoanDueReminderEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<bool>(),
                It.IsAny<string?>()))
            .ReturnsAsync(true);
    }

    private async Task RunReminderBatchAsync()
    {
        using var scope = CreateScope();
        await scope.ServiceProvider
            .GetRequiredService<ILoanReminderService>()
            .SendReminderBatchAsync();
    }

    /// <summary>
    /// Tests Send Reminder Batch – Passes Each Recipients Own Language.
    /// </summary>
    [Fact]
    public async Task SendReminderBatch_PassesEachRecipientsOwnLanguage()
    {
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("Felidae", TaxonomyRank.Family);
        var geo = await SpecimenRepo.CreateGeographicLocationAsync();
        var lender = await UserRepo.CreateUserAsync($"{Guid.CreateVersion7()}lender@test.de", status: UserStatus.Active);
        var borrower = await UserRepo.CreateUserAsync($"{Guid.CreateVersion7()}borrower@test.de", status: UserStatus.Active);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid);

        // Borrower prefers English, lender prefers German.
        await CreateUserSettingsInDbAsync(borrower.Guid, language: "en");
        await CreateUserSettingsInDbAsync(lender.Guid, language: "de");

        await LoanRepo.CreateLoanAsync(
            specimen.Guid, lender.Guid, borrower.Guid,
            dueDate: DateTime.UtcNow.Date.AddDays(5));

        SetupBrevoMock();

        await RunReminderBatchAsync();

        // isBorrower: true → borrower's language; isBorrower: false → lender's language.
        Fixture.BrevoMock.Verify(b => b.SendLoanDueReminderEmail(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>(), true, "en"), Times.Once);
        Fixture.BrevoMock.Verify(b => b.SendLoanDueReminderEmail(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>(), false, "de"), Times.Once);
    }

    /// <summary>
    /// Tests Send Reminder Batch – Default Settings – Passes German Language.
    /// </summary>
    [Fact]
    public async Task SendReminderBatch_DefaultSettings_PassesGermanLanguage()
    {
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("Felidae", TaxonomyRank.Family);
        var geo = await SpecimenRepo.CreateGeographicLocationAsync();
        var lender = await UserRepo.CreateUserAsync($"{Guid.CreateVersion7()}lender@test.de", status: UserStatus.Active);
        var borrower = await UserRepo.CreateUserAsync($"{Guid.CreateVersion7()}borrower@test.de", status: UserStatus.Active);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid);

        await LoanRepo.CreateLoanAsync(
            specimen.Guid, lender.Guid, borrower.Guid,
            dueDate: DateTime.UtcNow.Date.AddDays(5));

        SetupBrevoMock();

        await RunReminderBatchAsync();

        // Users keep the default settings (German), so both reminders are sent with the "de" language.
        Fixture.BrevoMock.Verify(b => b.SendLoanDueReminderEmail(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<bool>(), "de"), Times.Exactly(2));
    }
}
