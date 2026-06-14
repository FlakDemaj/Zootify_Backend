using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Jobs;

public class LoanReminderServiceTests : IntegrationTestBase
{
    public LoanReminderServiceTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }
    
    private void SetupBrevoMock()
    {
        Fixture.BrevoMock
            .Setup(b => b.SendLoanDueReminderEmail(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<string?>()))
            .ReturnsAsync(true);
    }

    private async Task<ILoanReminderService> ResolveLoanReminderServiceAsync()
    {
        var scope = CreateScope();
        return scope.ServiceProvider.GetRequiredService<ILoanReminderService>();
    }

    private async Task<(UserEntity lender, UserEntity borrower, SpecimenEntity specimen)>
        CreateLoanParticipantsAsync()
    {
        var species  = await SpecimenRepo.CreateTaxonomyNodeAsync("Felidae", TaxonomyRank.Family);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        // Create Random email addresses
        var lender   = await UserRepo.CreateUserAsync($"{Guid.CreateVersion7()}lender@test.de", firstName: "Max", lastName: "Mustermann", status: UserStatus.Active);
        var borrower = await UserRepo.CreateUserAsync($"{Guid.CreateVersion7()}borrower@test.de", firstName: "Anna", lastName: "Test", status: UserStatus.Active);
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);
        return (lender, borrower, specimen);
    }

    // Stage-1 (≤7 days) ------------------------------------------------------

    [Fact]
    public async Task Stage1_LoanDueIn6Days_SendsReminderToBorrowerAndLender()
    {
        SetupBrevoMock();
        var (lender, borrower, specimen) = await CreateLoanParticipantsAsync();
        await LoanRepo.CreateLoanAsync(
            specimen.Guid, lender.Guid, borrower.Guid,
            dueDate: DateTime.UtcNow.Date.AddDays(6));

        using var scope = CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ILoanReminderService>();
        await service.SendReminderBatchAsync();

        Fixture.BrevoMock.Verify(b => b.SendLoanDueReminderEmail(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<string?>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Stage1_SetsStage1ReminderSentAt()
    {
        SetupBrevoMock();
        var (lender, borrower, specimen) = await CreateLoanParticipantsAsync();
        var loan = await LoanRepo.CreateLoanAsync(
            specimen.Guid, lender.Guid, borrower.Guid,
            dueDate: DateTime.UtcNow.Date.AddDays(5));

        using var scope = CreateScope();
        await scope.ServiceProvider.GetRequiredService<ILoanReminderService>().SendReminderBatchAsync();

        var updated = await Fixture.DbContext.Set<LoanEntity>().AsNoTracking()
            .FirstAsync(l => l.Guid == loan.Guid);
        updated.Stage1ReminderSentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Stage1_AlreadySent_DoesNotSendAgain()
    {
        SetupBrevoMock();
        var (lender, borrower, specimen) = await CreateLoanParticipantsAsync();
        await LoanRepo.CreateLoanAsync(
            specimen.Guid, lender.Guid, borrower.Guid,
            dueDate: DateTime.UtcNow.Date.AddDays(5),
            stage1SentAt: DateTime.UtcNow.AddHours(-1));

        using var scope = CreateScope();
        await scope.ServiceProvider.GetRequiredService<ILoanReminderService>().SendReminderBatchAsync();

        Fixture.BrevoMock.Verify(b => b.SendLoanDueReminderEmail(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<string?>()),
            Times.Never);
    }

    // Stage-2 (≤4 days) ------------------------------------------------------

    [Fact]
    public async Task Stage2_LoanDueIn3Days_SendsStage2Reminder()
    {
        SetupBrevoMock();
        var (lender, borrower, specimen) = await CreateLoanParticipantsAsync();
        await LoanRepo.CreateLoanAsync(
            specimen.Guid, lender.Guid, borrower.Guid,
            dueDate: DateTime.UtcNow.Date.AddDays(3),
            stage1SentAt: DateTime.UtcNow.AddDays(-4));

        using var scope = CreateScope();
        await scope.ServiceProvider.GetRequiredService<ILoanReminderService>().SendReminderBatchAsync();

        Fixture.BrevoMock.Verify(b => b.SendLoanDueReminderEmail(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<string?>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Stage2_SetsStage2ReminderSentAt()
    {
        SetupBrevoMock();
        var (lender, borrower, specimen) = await CreateLoanParticipantsAsync();
        var loan = await LoanRepo.CreateLoanAsync(
            specimen.Guid, lender.Guid, borrower.Guid,
            dueDate: DateTime.UtcNow.Date.AddDays(3),
            stage1SentAt: DateTime.UtcNow.AddDays(-4));

        using var scope = CreateScope();
        await scope.ServiceProvider.GetRequiredService<ILoanReminderService>().SendReminderBatchAsync();

        var updated = await Fixture.DbContext.Set<LoanEntity>().AsNoTracking()
            .FirstAsync(l => l.Guid == loan.Guid);
        updated.Stage2ReminderSentAt.Should().NotBeNull();
    }

    // Stage-3 (≤1 day) -------------------------------------------------------

    [Fact]
    public async Task Stage3_LoanDueTomorrow_SendsStage3Reminder()
    {
        SetupBrevoMock();
        var (lender, borrower, specimen) = await CreateLoanParticipantsAsync();
        await LoanRepo.CreateLoanAsync(
            specimen.Guid, lender.Guid, borrower.Guid,
            dueDate: DateTime.UtcNow.Date.AddDays(1),
            stage1SentAt: DateTime.UtcNow.AddDays(-7),
            stage2SentAt: DateTime.UtcNow.AddDays(-4));

        using var scope = CreateScope();
        await scope.ServiceProvider.GetRequiredService<ILoanReminderService>().SendReminderBatchAsync();

        Fixture.BrevoMock.Verify(b => b.SendLoanDueReminderEmail(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<string?>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Stage3_SetsStage3ReminderSentAt()
    {
        SetupBrevoMock();
        var (lender, borrower, specimen) = await CreateLoanParticipantsAsync();
        var loan = await LoanRepo.CreateLoanAsync(
            specimen.Guid, lender.Guid, borrower.Guid,
            dueDate: DateTime.UtcNow.Date.AddDays(1),
            stage1SentAt: DateTime.UtcNow.AddDays(-7),
            stage2SentAt: DateTime.UtcNow.AddDays(-4));

        using var scope = CreateScope();
        await scope.ServiceProvider.GetRequiredService<ILoanReminderService>().SendReminderBatchAsync();

        var updated = await Fixture.DbContext.Set<LoanEntity>().AsNoTracking()
            .FirstAsync(l => l.Guid == loan.Guid);
        updated.Stage3ReminderSentAt.Should().NotBeNull();
    }

    // Already returned -------------------------------------------------------

    [Fact]
    public async Task ReturnedLoan_DoesNotReceiveAnyReminder()
    {
        SetupBrevoMock();
        var (lender, borrower, specimen) = await CreateLoanParticipantsAsync();
        await LoanRepo.CreateLoanAsync(
            specimen.Guid, lender.Guid, borrower.Guid,
            dueDate: DateTime.UtcNow.Date.AddDays(1),
            returnDate: DateTime.UtcNow.AddDays(-1));

        using var scope = CreateScope();
        await scope.ServiceProvider.GetRequiredService<ILoanReminderService>().SendReminderBatchAsync();

        Fixture.BrevoMock.Verify(b => b.SendLoanDueReminderEmail(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<string?>()),
            Times.Never);
    }

    // Multiple loans ---------------------------------------------------------

    [Fact]
    public async Task MultipleLoans_EachStageHandledIndependently()
    {
        SetupBrevoMock();
        var (lender, borrower, specimen) = await CreateLoanParticipantsAsync();
        var (lenderTwo, borrowerTwo, specimenTwo) = await CreateLoanParticipantsAsync();
        var (lenderThree, borrowerThree, specimenThree) = await CreateLoanParticipantsAsync();

        // Stage-1 only
        await LoanRepo.CreateLoanAsync(specimen.Guid, lender.Guid, borrower.Guid,
            dueDate: DateTime.UtcNow.Date.AddDays(7));
        // Stage-2 only (stage1 already sent)
        await LoanRepo.CreateLoanAsync(specimenTwo.Guid, lenderTwo.Guid, borrowerTwo.Guid,
            dueDate: DateTime.UtcNow.Date.AddDays(3),
            stage1SentAt: DateTime.UtcNow.AddDays(-4));
        // Stage-3 only (stage1+2 already sent)
        await LoanRepo.CreateLoanAsync(specimenThree.Guid, lenderThree.Guid, borrowerThree.Guid,
            dueDate: DateTime.UtcNow.Date.AddDays(1),
            stage1SentAt: DateTime.UtcNow.AddDays(-7),
            stage2SentAt: DateTime.UtcNow.AddDays(-4));

        using var scope = CreateScope();
        var sent = await scope.ServiceProvider.GetRequiredService<ILoanReminderService>().SendReminderBatchAsync();

        // 3 loans × 2 recipients (borrower + lender) = 6 emails
        Fixture.BrevoMock.Verify(b => b.SendLoanDueReminderEmail(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<string?>()),
            Times.Exactly(6));
        sent.Should().Be(3);
    }

    [Fact]
    public async Task NoLoans_ReturnsZeroAndSendsNoEmails()
    {
        SetupBrevoMock();

        using var scope = CreateScope();
        var sent = await scope.ServiceProvider.GetRequiredService<ILoanReminderService>().SendReminderBatchAsync();

        sent.Should().Be(0);
        Fixture.BrevoMock.Verify(b => b.SendLoanDueReminderEmail(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<string?>()),
            Times.Never);
    }
}
