using Microsoft.Extensions.DependencyInjection;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Jobs;

/// <summary>
/// Verifies that loan due-date reminders are accompanied by an in-app push notification to each
/// recipient (borrower and lender), honoring each recipient's notification settings and language.
/// Push message keywords: German "fällig", English "due soon".
/// </summary>
public class LoanReminderNotificationTests : IntegrationTestBase
{
    public LoanReminderNotificationTests(IntegrationTestFixture fixture) : base(fixture) { }

    private void SetupBrevoReturnsTrue()
    {
        Fixture.BrevoMock
            .Setup(b => b.SendLoanDueReminderEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<bool>(),
                It.IsAny<string?>()))
            .ReturnsAsync(true);
    }

    private async Task<(UserEntity lender, UserEntity borrower, SpecimenEntity specimen)> ArrangeAsync()
    {
        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("Felidae", TaxonomyRank.Family);
        var geo = await SpecimenRepo.CreateGeographicLocationAsync();
        var lender = await UserRepo.CreateUserAsync($"{Guid.CreateVersion7()}lender@test.de", status: UserStatus.Active);
        var borrower = await UserRepo.CreateUserAsync($"{Guid.CreateVersion7()}borrower@test.de", status: UserStatus.Active);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, geo.Guid);
        return (lender, borrower, specimen);
    }

    private async Task RunReminderBatchAsync()
    {
        using var scope = CreateScope();
        await scope.ServiceProvider
            .GetRequiredService<ILoanReminderService>()
            .SendReminderBatchAsync();
    }

    /// <summary>
    /// Tests Send Reminder Batch – Sends Push To Both Recipients In Their Own Language.
    /// </summary>
    [Fact]
    public async Task SendReminderBatch_SendsPushToBothRecipientsInTheirOwnLanguage()
    {
        SetupBrevoReturnsTrue();
        var (lender, borrower, specimen) = await ArrangeAsync();

        await CreateUserSettingsInDbAsync(borrower.Guid, language: "en");
        await CreateUserSettingsInDbAsync(lender.Guid, language: "de");

        await LoanRepo.CreateLoanAsync(
            specimen.Guid, lender.Guid, borrower.Guid,
            dueDate: DateTime.UtcNow.Date.AddDays(5));

        await RunReminderBatchAsync();

        VerifyPushNotificationSent("due soon", Times.Once()); // borrower (English)
        VerifyPushNotificationSent("fällig", Times.Once());   // lender (German)
    }

    /// <summary>
    /// Tests Send Reminder Batch – Borrower Push Disabled – Only Lender Receives Push.
    /// </summary>
    [Fact]
    public async Task SendReminderBatch_BorrowerPushDisabled_OnlyLenderReceivesPush()
    {
        SetupBrevoReturnsTrue();
        var (lender, borrower, specimen) = await ArrangeAsync();

        await CreateUserSettingsInDbAsync(borrower.Guid, sendPush: false, language: "en");
        await CreateUserSettingsInDbAsync(lender.Guid, language: "de");

        await LoanRepo.CreateLoanAsync(
            specimen.Guid, lender.Guid, borrower.Guid,
            dueDate: DateTime.UtcNow.Date.AddDays(5));

        await RunReminderBatchAsync();

        VerifyPushNotificationSent("due soon", Times.Never()); // borrower opted out
        VerifyPushNotificationSent("fällig", Times.Once());    // lender still notified
    }
}
