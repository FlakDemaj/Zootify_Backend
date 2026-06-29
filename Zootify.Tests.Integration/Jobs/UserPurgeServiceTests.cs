using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Jobs;

/// <summary>
/// Tests for User Purge Service.
/// </summary>
public class UserPurgeServiceTests : IntegrationTestBase
{
    // Default RetentionYears = 3; cutoff = now - 3 years
    private static readonly DateTime OldEnough   = DateTime.UtcNow.AddYears(-4); // beyond retention
    private static readonly DateTime TooRecent   = DateTime.UtcNow.AddYears(-1); // within retention

    public UserPurgeServiceTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    /// <summary>
    /// Tests Purge Batch – Deleted User Beyond Retention – Is Hard Deleted.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_DeletedUserBeyondRetention_IsHardDeleted()
    {
        var user = await UserRepo.CreateUserAsync("old@test.de", status: UserStatus.Deleted);
        await OverrideDeletionDateAsync(user, OldEnough);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<IUserPurgeService>().PurgeBatchAsync();

        purged.Should().Be(1);
        var inDb = await Fixture.DbContext.Set<UserEntity>().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Guid == user.Guid);
        inDb.Should().BeNull();
    }

    /// <summary>
    /// Tests Purge Batch – Deleted User Within Retention – Is Not Purged.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_DeletedUserWithinRetention_IsNotPurged()
    {
        var user = await UserRepo.CreateUserAsync("recent@test.de", status: UserStatus.Deleted);
        await OverrideDeletionDateAsync(user, TooRecent);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<IUserPurgeService>().PurgeBatchAsync();

        purged.Should().Be(0);
        var inDb = await Fixture.DbContext.Set<UserEntity>().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Guid == user.Guid);
        inDb.Should().NotBeNull();
    }

    /// <summary>
    /// Tests Purge Batch – Active User – Is Not Purged.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_ActiveUser_IsNotPurged()
    {
        var user = await UserRepo.CreateUserAsync("active@test.de", status: UserStatus.Active);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<IUserPurgeService>().PurgeBatchAsync();

        purged.Should().Be(0);
        var inDb = await Fixture.DbContext.Set<UserEntity>().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Guid == user.Guid);
        inDb.Should().NotBeNull();
    }

    /// <summary>
    /// Tests Purge Batch – No Eligible Users – Returns Zero.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_NoEligibleUsers_ReturnsZero()
    {
        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<IUserPurgeService>().PurgeBatchAsync();

        purged.Should().Be(0);
    }

    /// <summary>
    /// Tests Purge Batch – User With Specimen And Loans – Anonymizes Content And Purges.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_UserWithSpecimenAndLoans_AnonymizesContentAndPurges()
    {
        var purgedUser = await UserRepo.CreateUserAsync("authored@test.de", status: UserStatus.Deleted);
        await OverrideDeletionDateAsync(purgedUser, OldEnough);

        var otherUser = await UserRepo.CreateUserAsync("other@test.de", status: UserStatus.Active);

        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("Felidae", TaxonomyRank.Family);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        // A (non-deleted) specimen the purged user created — it must survive, only anonymized.
        var specimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, location.Guid, createdByUserGuid: purgedUser.Guid);

        // Loans where the purged user is lender / borrower — both must be removed.
        var lenderLoan = await LoanRepo.CreateLoanAsync(
            specimen.Guid, purgedUser.Guid, otherUser.Guid, dueDate: DateTime.UtcNow.AddDays(7));
        var borrowerLoan = await LoanRepo.CreateLoanAsync(
            specimen.Guid, otherUser.Guid, purgedUser.Guid, dueDate: DateTime.UtcNow.AddDays(7));

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<IUserPurgeService>().PurgeBatchAsync();

        purged.Should().Be(1);

        // The user is hard-deleted despite having authored a specimen and being in loans.
        (await Fixture.DbContext.Set<UserEntity>().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Guid == purgedUser.Guid)).Should().BeNull();

        // The specimen survives with its creator anonymized (set to null).
        var specimenInDb = await Fixture.DbContext.Set<SpecimenEntity>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.Guid == specimen.Guid);
        specimenInDb.Should().NotBeNull();
        specimenInDb!.CreatedByUserGuid.Should().BeNull();

        // Both loans referencing the purged user are gone.
        var remainingLoanGuids = await Fixture.DbContext.Set<LoanEntity>().AsNoTracking()
            .Select(l => l.Guid)
            .ToListAsync();
        remainingLoanGuids.Should().NotContain(lenderLoan.Guid);
        remainingLoanGuids.Should().NotContain(borrowerLoan.Guid);
    }

    /// <summary>
    /// Tests Purge Batch – User With Loan Requests – Removes Own Requests And Clears Approver References.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_UserWithLoanRequests_RemovesOwnAndClearsApprover()
    {
        var purgedUser = await UserRepo.CreateUserAsync("authored@test.de", status: UserStatus.Deleted);
        await OverrideDeletionDateAsync(purgedUser, OldEnough);

        var otherUser = await UserRepo.CreateUserAsync("other@test.de", status: UserStatus.Active);

        var species = await SpecimenRepo.CreateTaxonomyNodeAsync("Felidae", TaxonomyRank.Family);
        var location = await SpecimenRepo.CreateGeographicLocationAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);

        // A request made BY the purged user must be removed (requester_guid is non-nullable).
        var ownRequest = await LoanRequestRepo.CreateLoanRequestAsync(specimen.Guid, purgedUser.Guid);
        // A request by another user that the purged user has admin-approved must survive with the
        // approver reference cleared.
        var approvedRequest = await LoanRequestRepo.CreateLoanRequestAsync(
            specimen.Guid, otherUser.Guid, adminApproverGuid: purgedUser.Guid, adminApprovedAt: DateTime.UtcNow);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<IUserPurgeService>().PurgeBatchAsync();

        purged.Should().Be(1);

        (await Fixture.DbContext.Set<UserEntity>().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Guid == purgedUser.Guid)).Should().BeNull();

        var remainingRequestGuids = await Fixture.DbContext.Set<LoanRequestEntity>().AsNoTracking()
            .Select(r => r.Guid)
            .ToListAsync();
        remainingRequestGuids.Should().NotContain(ownRequest.Guid);
        remainingRequestGuids.Should().Contain(approvedRequest.Guid);

        var survivor = await Fixture.DbContext.Set<LoanRequestEntity>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.Guid == approvedRequest.Guid);
        survivor!.AdminApproverGuid.Should().BeNull();
    }

    /// <summary>
    /// Tests Purge Batch – Multiple Eligible – All Purged.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_MultipleEligible_AllPurged()
    {
        var u1 = await UserRepo.CreateUserAsync("old1@test.de", status: UserStatus.Deleted);
        var u2 = await UserRepo.CreateUserAsync("old2@test.de", status: UserStatus.Deleted);
        var u3 = await UserRepo.CreateUserAsync("active@test.de", status: UserStatus.Active);
        await OverrideDeletionDateAsync(u1, OldEnough);
        await OverrideDeletionDateAsync(u2, OldEnough);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<IUserPurgeService>().PurgeBatchAsync();

        purged.Should().Be(2);
        var u3InDb = await Fixture.DbContext.Set<UserEntity>().AsNoTracking()
            .FirstOrDefaultAsync(u => u.Guid == u3.Guid);
        u3InDb.Should().NotBeNull();
    }
}
