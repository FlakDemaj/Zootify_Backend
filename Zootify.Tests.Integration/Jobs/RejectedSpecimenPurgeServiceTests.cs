using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Jobs;

/// <summary>
/// Tests for Rejected Specimen Purge Service.
/// </summary>
public class RejectedSpecimenPurgeServiceTests : IntegrationTestBase
{
    private static readonly DateTime OldEnough = DateTime.UtcNow.AddYears(-4);
    private static readonly DateTime TooRecent  = DateTime.UtcNow.AddDays(-1);

    public RejectedSpecimenPurgeServiceTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    private async Task<(TaxonomyNodeEntity species, GeographicLocationEntity location)> CreatePrerequisitesAsync()
    {
        var species  = await SpecimenRepo.CreateTaxonomyNodeAsync("Felidae", TaxonomyRank.Family);
        var location = await SpecimenRepo.CreateGeographicLocationAsync("DE");
        return (species, location);
    }

    private async Task OverrideRejectedAtAsync(SpecimenEntity specimen, DateTime rejectedAt)
    {
        Fixture.DbContext.ChangeTracker.Clear();
        Fixture.DbContext.Attach(specimen);
        Fixture.DbContext.Entry(specimen).Property(s => s.RejectedAt).CurrentValue = rejectedAt;
        await Fixture.DbContext.SaveChangesAsync();
        Fixture.DbContext.ChangeTracker.Clear();
    }

    #region Happy Path

    /// <summary>
    /// Tests Purge Batch – Rejected Specimen Beyond Retention – Is Hard Deleted.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_RejectedSpecimenBeyondRetention_IsHardDeleted()
    {
        var (species, location) = await CreatePrerequisitesAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, location.Guid, status: SpecimenStatus.Rejected);
        await OverrideRejectedAtAsync(specimen, OldEnough);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider
            .GetRequiredService<IRejectedSpecimenPurgeService>().PurgeBatchAsync();

        purged.Should().Be(1);
        var inDb = await Fixture.DbContext.Set<SpecimenEntity>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.Guid == specimen.Guid);
        inDb.Should().BeNull();
    }

    /// <summary>
    /// Tests Purge Batch – Rejected Specimen Beyond Retention – Returns Correct Count.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_RejectedSpecimenBeyondRetention_ReturnsCorrectCount()
    {
        var (species, location) = await CreatePrerequisitesAsync();
        var s1 = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, status: SpecimenStatus.Rejected);
        var s2 = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, status: SpecimenStatus.Rejected);
        var s3 = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);
        await OverrideRejectedAtAsync(s1, OldEnough);
        await OverrideRejectedAtAsync(s2, OldEnough);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider
            .GetRequiredService<IRejectedSpecimenPurgeService>().PurgeBatchAsync();

        purged.Should().Be(2);
        var s3InDb = await Fixture.DbContext.Set<SpecimenEntity>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.Guid == s3.Guid);
        s3InDb.Should().NotBeNull();
    }

    /// <summary>
    /// Tests Purge Batch – No Eligible Specimens – Returns Zero.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_NoEligibleSpecimens_ReturnsZero()
    {
        using var scope = CreateScope();
        var purged = await scope.ServiceProvider
            .GetRequiredService<IRejectedSpecimenPurgeService>().PurgeBatchAsync();

        purged.Should().Be(0);
    }

    #endregion

    #region Retention Window

    /// <summary>
    /// Tests Purge Batch – Rejected Specimen Within Retention – Is Not Purged.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_RejectedSpecimenWithinRetention_IsNotPurged()
    {
        var (species, location) = await CreatePrerequisitesAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, location.Guid, status: SpecimenStatus.Rejected);
        await OverrideRejectedAtAsync(specimen, TooRecent);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider
            .GetRequiredService<IRejectedSpecimenPurgeService>().PurgeBatchAsync();

        purged.Should().Be(0);
        var inDb = await Fixture.DbContext.Set<SpecimenEntity>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.Guid == specimen.Guid);
        inDb.Should().NotBeNull();
    }

    /// <summary>
    /// Tests Purge Batch – Only Old Rejected Purged – Recent Rejected Survives.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_OnlyOldRejectedPurged_RecentRejectedSurvives()
    {
        var (species, location) = await CreatePrerequisitesAsync();
        var old    = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, status: SpecimenStatus.Rejected);
        var recent = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, status: SpecimenStatus.Rejected);
        await OverrideRejectedAtAsync(old, OldEnough);
        await OverrideRejectedAtAsync(recent, TooRecent);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider
            .GetRequiredService<IRejectedSpecimenPurgeService>().PurgeBatchAsync();

        purged.Should().Be(1);
        var recentInDb = await Fixture.DbContext.Set<SpecimenEntity>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.Guid == recent.Guid);
        recentInDb.Should().NotBeNull();
    }

    #endregion

    #region Guard: Wrong Status or Missing Date

    /// <summary>
    /// Tests Purge Batch – Active Specimen – Is Not Purged.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_ActiveSpecimen_IsNotPurged()
    {
        var (species, location) = await CreatePrerequisitesAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider
            .GetRequiredService<IRejectedSpecimenPurgeService>().PurgeBatchAsync();

        purged.Should().Be(0);
        var inDb = await Fixture.DbContext.Set<SpecimenEntity>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.Guid == specimen.Guid);
        inDb.Should().NotBeNull();
    }

    /// <summary>
    /// Tests Purge Batch – Deleted Specimen – Is Not Purged.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_DeletedSpecimen_IsNotPurged()
    {
        var (species, location) = await CreatePrerequisitesAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, location.Guid, status: SpecimenStatus.Deleted);
        await OverrideDeletionDateAsync(specimen, OldEnough);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider
            .GetRequiredService<IRejectedSpecimenPurgeService>().PurgeBatchAsync();

        purged.Should().Be(0);
        var inDb = await Fixture.DbContext.Set<SpecimenEntity>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.Guid == specimen.Guid);
        inDb.Should().NotBeNull();
    }

    /// <summary>
    /// Tests Purge Batch – Rejected Specimen With Null Rejected At – Is Not Purged.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_RejectedSpecimenWithNullRejectedAt_IsNotPurged()
    {
        var (species, location) = await CreatePrerequisitesAsync();
        // Status = Rejected but RejectedAt remains null — must not be purged
        var specimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, location.Guid, status: SpecimenStatus.Rejected);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider
            .GetRequiredService<IRejectedSpecimenPurgeService>().PurgeBatchAsync();

        purged.Should().Be(0);
        var inDb = await Fixture.DbContext.Set<SpecimenEntity>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.Guid == specimen.Guid);
        inDb.Should().NotBeNull();
    }

    #endregion

    #region Cascade Deletes

    /// <summary>
    /// Tests Purge Batch – Specimen With Image – Image Is Also Deleted.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_SpecimenWithImage_ImageIsAlsoDeleted()
    {
        var (species, location) = await CreatePrerequisitesAsync();
        var specimen = await SpecimenRepo.CreateSpecimenWithImageAsync(
            species.Guid, location.Guid, status: SpecimenStatus.Rejected);
        await OverrideRejectedAtAsync(specimen, OldEnough);

        using var scope = CreateScope();
        await scope.ServiceProvider
            .GetRequiredService<IRejectedSpecimenPurgeService>().PurgeBatchAsync();

        var images = await GetImagesForSpecimenFromDbAsync(specimen.Guid);
        images.Should().BeEmpty();
    }

    /// <summary>
    /// Tests Purge Batch – Specimen With Tags – Tag Relations Are Also Deleted.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_SpecimenWithTags_TagRelationsAreAlsoDeleted()
    {
        var (species, location) = await CreatePrerequisitesAsync();
        var tag1 = await TagRepo.CreateTagAsync("reptile");
        var tag2 = await TagRepo.CreateTagAsync("endangered");
        var specimen = await SpecimenRepo.CreateSpecimenWithTagsAsync(
            species.Guid, location.Guid,
            tags: [tag1, tag2],
            status: SpecimenStatus.Rejected);
        await OverrideRejectedAtAsync(specimen, OldEnough);

        using var scope = CreateScope();
        await scope.ServiceProvider
            .GetRequiredService<IRejectedSpecimenPurgeService>().PurgeBatchAsync();

        var relations = await GetTagRelationsForSpecimenFromDbAsync(specimen.Guid);
        relations.Should().BeEmpty();
    }

    #endregion
}
