using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Jobs;

/// <summary>
/// Tests for Collection Purge Service.
/// </summary>
public class CollectionPurgeServiceTests : IntegrationTestBase
{
    private static readonly DateTime OldEnough = DateTime.UtcNow.AddYears(-4);
    private static readonly DateTime TooRecent  = DateTime.UtcNow.AddYears(-1);

    public CollectionPurgeServiceTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    /// <summary>
    /// Tests Purge Batch – Deleted Collection Beyond Retention – Is Hard Deleted.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_DeletedCollectionBeyondRetention_IsHardDeleted()
    {
        var deleter    = await UserRepo.CreateUserAsync("deleter@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(deletedByUserGuid: deleter.Guid);
        await OverrideDeletionDateAsync(collection, OldEnough);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<ICollectionPurgeService>().PurgeBatchAsync();

        purged.Should().Be(1);
        var inDb = await Fixture.DbContext.Set<CollectionEntity>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Guid == collection.Guid);
        inDb.Should().BeNull();
    }

    /// <summary>
    /// Tests Purge Batch – Deleted Collection Within Retention – Is Not Purged.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_DeletedCollectionWithinRetention_IsNotPurged()
    {
        var deleter    = await UserRepo.CreateUserAsync("deleter@test.de");
        var collection = await CollectionRepo.CreateCollectionAsync(deletedByUserGuid: deleter.Guid);
        await OverrideDeletionDateAsync(collection, TooRecent);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<ICollectionPurgeService>().PurgeBatchAsync();

        purged.Should().Be(0);
        var inDb = await Fixture.DbContext.Set<CollectionEntity>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Guid == collection.Guid);
        inDb.Should().NotBeNull();
    }

    /// <summary>
    /// Tests Purge Batch – Active Collection – Is Not Purged.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_ActiveCollection_IsNotPurged()
    {
        await CollectionRepo.CreateCollectionAsync("Active Collection");

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<ICollectionPurgeService>().PurgeBatchAsync();

        purged.Should().Be(0);
    }

    /// <summary>
    /// Tests Purge Batch – No Eligible Collections – Returns Zero.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_NoEligibleCollections_ReturnsZero()
    {
        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<ICollectionPurgeService>().PurgeBatchAsync();

        purged.Should().Be(0);
    }

    /// <summary>
    /// Tests Purge Batch – Multiple Eligible – All Purged.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_MultipleEligible_AllPurged()
    {
        var deleter = await UserRepo.CreateUserAsync("deleter@test.de");
        var c1 = await CollectionRepo.CreateCollectionAsync("Old 1", deletedByUserGuid: deleter.Guid);
        var c2 = await CollectionRepo.CreateCollectionAsync("Old 2", deletedByUserGuid: deleter.Guid);
        var c3 = await CollectionRepo.CreateCollectionAsync("Active");
        await OverrideDeletionDateAsync(c1, OldEnough);
        await OverrideDeletionDateAsync(c2, OldEnough);

        using var scope = CreateScope();
        var purged = await scope.ServiceProvider.GetRequiredService<ICollectionPurgeService>().PurgeBatchAsync();

        purged.Should().Be(2);
        var c3InDb = await Fixture.DbContext.Set<CollectionEntity>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Guid == c3.Guid);
        c3InDb.Should().NotBeNull();
    }
}
