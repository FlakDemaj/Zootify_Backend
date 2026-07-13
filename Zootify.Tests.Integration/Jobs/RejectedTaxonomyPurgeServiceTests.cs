using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify.Tests.Integration.Utils;

namespace Zootify.Tests.Integration.Jobs;

/// <summary>
/// Tests for Rejected Taxonomy Purge Service.
/// </summary>
public class RejectedTaxonomyPurgeServiceTests : IntegrationTestBase
{
    private static readonly DateTime OldEnough = DateTime.UtcNow.AddDays(-20);
    private static readonly DateTime TooRecent = DateTime.UtcNow.AddDays(-1);

    public RejectedTaxonomyPurgeServiceTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    private async Task<TaxonomyNodeEntity> CreateRejectedNodeAsync(
        string name,
        TaxonomyRank rank,
        DateTime rejectedDate,
        Guid? parentGuid = null)
    {
        var node = await SpecimenRepo.CreateTaxonomyNodeAsync(
            name, rank, ApprovalStatus.Rejected, parentGuid: parentGuid);
        await OverrideRejectedDateAsync(node, rejectedDate);
        return node;
    }

    private async Task OverrideRejectedDateAsync(TaxonomyNodeEntity node, DateTime rejectedDate)
    {
        Fixture.DbContext.ChangeTracker.Clear();
        Fixture.DbContext.Attach(node);
        Fixture.DbContext.Entry(node).Property(n => n.RejectedDate).CurrentValue = rejectedDate;
        await Fixture.DbContext.SaveChangesAsync();
        Fixture.DbContext.ChangeTracker.Clear();
    }

    private async Task<int> PurgeAsync()
    {
        using var scope = CreateScope();
        return await scope.ServiceProvider
            .GetRequiredService<IRejectedTaxonomyPurgeService>().PurgeBatchAsync();
    }

    private async Task<TaxonomyNodeEntity?> NodeInDbAsync(Guid guid) =>
        await Fixture.DbContext.Set<TaxonomyNodeEntity>().AsNoTracking()
            .FirstOrDefaultAsync(n => n.Guid == guid);

    #region Happy Path

    /// <summary>
    /// Tests Purge Batch – Rejected Node Beyond Retention – Is Hard Deleted.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_RejectedNodeBeyondRetention_IsHardDeleted()
    {
        var node = await CreateRejectedNodeAsync("Chordata", TaxonomyRank.Phylum, OldEnough);

        var purged = await PurgeAsync();

        purged.Should().Be(1);
        (await NodeInDbAsync(node.Guid)).Should().BeNull();
    }

    /// <summary>
    /// Tests Purge Batch – No Eligible Nodes – Returns Zero.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_NoEligibleNodes_ReturnsZero()
    {
        (await PurgeAsync()).Should().Be(0);
    }

    #endregion

    #region Retention Window

    /// <summary>
    /// Tests Purge Batch – Rejected Node Within Retention – Is Not Purged.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_RejectedNodeWithinRetention_IsNotPurged()
    {
        var node = await CreateRejectedNodeAsync("Chordata", TaxonomyRank.Phylum, TooRecent);

        var purged = await PurgeAsync();

        purged.Should().Be(0);
        (await NodeInDbAsync(node.Guid)).Should().NotBeNull();
    }

    #endregion

    #region Guard: Wrong Status or Missing Date

    /// <summary>
    /// Tests Purge Batch – Pending Node – Is Not Purged.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_PendingNode_IsNotPurged()
    {
        var node = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "Chordata", TaxonomyRank.Phylum, ApprovalStatus.Pending);

        var purged = await PurgeAsync();

        purged.Should().Be(0);
        (await NodeInDbAsync(node.Guid)).Should().NotBeNull();
    }

    /// <summary>
    /// Tests Purge Batch – Rejected Node With Null Rejected Date – Is Not Purged.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_RejectedNodeWithNullRejectedDate_IsNotPurged()
    {
        // Status = Rejected but RejectedDate remains null — must not be purged.
        var node = await SpecimenRepo.CreateTaxonomyNodeAsync(
            "Chordata", TaxonomyRank.Phylum, ApprovalStatus.Rejected);

        var purged = await PurgeAsync();

        purged.Should().Be(0);
        (await NodeInDbAsync(node.Guid)).Should().NotBeNull();
    }

    #endregion

    #region Leaf-First Deletion

    /// <summary>
    /// Tests Purge Batch – Rejected Parent With Rejected Child – Deletes Leaf First, Parent In Next Batch.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_RejectedParentWithRejectedChild_DeletesLeafFirst()
    {
        var parent = await CreateRejectedNodeAsync("Chordata", TaxonomyRank.Phylum, OldEnough);
        var child = await CreateRejectedNodeAsync(
            "Mammalia", TaxonomyRank.Class, OldEnough, parentGuid: parent.Guid);

        // First batch: only the leaf (child) is eligible; the parent still has a child.
        var firstPurge = await PurgeAsync();
        firstPurge.Should().Be(1);
        (await NodeInDbAsync(child.Guid)).Should().BeNull();
        (await NodeInDbAsync(parent.Guid)).Should().NotBeNull();

        // Second batch: the parent is now a leaf and gets purged.
        var secondPurge = await PurgeAsync();
        secondPurge.Should().Be(1);
        (await NodeInDbAsync(parent.Guid)).Should().BeNull();
    }

    /// <summary>
    /// Tests Purge Batch – Rejected Parent With Approved Child – Parent Survives.
    /// </summary>
    [Fact]
    public async Task PurgeBatch_RejectedParentWithApprovedChild_ParentSurvives()
    {
        var parent = await CreateRejectedNodeAsync("Chordata", TaxonomyRank.Phylum, OldEnough);
        await SpecimenRepo.CreateTaxonomyNodeAsync(
            "Mammalia", TaxonomyRank.Class, ApprovalStatus.Approved, parentGuid: parent.Guid);

        var purged = await PurgeAsync();

        // The approved child blocks the parent's deletion; nothing is purged.
        purged.Should().Be(0);
        (await NodeInDbAsync(parent.Guid)).Should().NotBeNull();
    }

    #endregion
}
