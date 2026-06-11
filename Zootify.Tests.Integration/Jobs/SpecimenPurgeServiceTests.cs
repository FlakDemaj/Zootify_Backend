using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Jobs;

public class SpecimenPurgeServiceTests : IntegrationTestBase
{
    private static readonly DateTime OldEnough = DateTime.UtcNow.AddYears(-4);
    private static readonly DateTime TooRecent  = DateTime.UtcNow.AddYears(-1);

    public SpecimenPurgeServiceTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    private async Task<(TaxonomyNodeEntity species, GeographicLocationEntity location)> CreatePrerequisitesAsync()
    {
        var species  = await SpecimenRepo.CreateTaxonomyNodeAsync("Felidae", TaxonomyRank.Family);
        var location = await SpecimenRepo.CreateGeographicLocationAsync("DE");
        return (species, location);
    }

    [Fact]
    public async Task PurgeBatch_DeletedSpecimenBeyondRetention_IsHardDeleted()
    {
        var (species, location) = await CreatePrerequisitesAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, location.Guid, status: SpecimenStatus.Deleted);
        await OverrideDeletionDateAsync(specimen, OldEnough);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<ISpecimenPurgeService>().PurgeBatchAsync();

        purged.Should().Be(1);
        var inDb = await Fixture.DbContext.Set<SpecimenEntity>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.Guid == specimen.Guid);
        inDb.Should().BeNull();
    }

    [Fact]
    public async Task PurgeBatch_DeletedSpecimenWithinRetention_IsNotPurged()
    {
        var (species, location) = await CreatePrerequisitesAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, location.Guid, status: SpecimenStatus.Deleted);
        await OverrideDeletionDateAsync(specimen, TooRecent);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<ISpecimenPurgeService>().PurgeBatchAsync();

        purged.Should().Be(0);
        var inDb = await Fixture.DbContext.Set<SpecimenEntity>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.Guid == specimen.Guid);
        inDb.Should().NotBeNull();
    }

    [Fact]
    public async Task PurgeBatch_ActiveSpecimen_IsNotPurged()
    {
        var (species, location) = await CreatePrerequisitesAsync();
        var specimen = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<ISpecimenPurgeService>().PurgeBatchAsync();

        purged.Should().Be(0);
        var inDb = await Fixture.DbContext.Set<SpecimenEntity>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.Guid == specimen.Guid);
        inDb.Should().NotBeNull();
    }

    [Fact]
    public async Task PurgeBatch_NoDeletionDate_IsNotPurged()
    {
        var (species, location) = await CreatePrerequisitesAsync();
        // Status = Deleted but DeletionDate is null → should not be purged
        var specimen = await SpecimenRepo.CreateSpecimenAsync(
            species.Guid, location.Guid, status: SpecimenStatus.Deleted);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<ISpecimenPurgeService>().PurgeBatchAsync();

        purged.Should().Be(0);
    }

    [Fact]
    public async Task PurgeBatch_NoEligibleSpecimens_ReturnsZero()
    {
        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<ISpecimenPurgeService>().PurgeBatchAsync();

        purged.Should().Be(0);
    }

    [Fact]
    public async Task PurgeBatch_MultipleEligible_AllPurged()
    {
        var (species, location) = await CreatePrerequisitesAsync();
        var s1 = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, status: SpecimenStatus.Deleted);
        var s2 = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid, status: SpecimenStatus.Deleted);
        var s3 = await SpecimenRepo.CreateSpecimenAsync(species.Guid, location.Guid);
        await OverrideDeletionDateAsync(s1, OldEnough);
        await OverrideDeletionDateAsync(s2, OldEnough);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<ISpecimenPurgeService>().PurgeBatchAsync();

        purged.Should().Be(2);
        var s3InDb = await Fixture.DbContext.Set<SpecimenEntity>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.Guid == s3.Guid);
        s3InDb.Should().NotBeNull();
    }
}
