using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify.Tests.Unit.Domain;

/// <summary>
/// Unit tests for the domain methods on
/// <see cref="BaseGuidEntity"/>,
/// <see cref="BaseCreationEntity"/>,
/// <see cref="BaseUpdateEntity"/>,
/// <see cref="BaseDeletionEntity"/>,
/// </summary>
public class BaseEntitysTests
{
    
    #region BaseUpdatedEntity

    /// <summary>
    /// Verifies that passing a GUID stores it in UpdatedByUserGuid.
    /// </summary>
    [Fact]
    public void SetUpdatedBy_WithGuid_SetsUpdatedByUserGuidAndUpdatedDate()
    {
        var user = UserTestData.CreateUser();
        var adminGuid = Guid.NewGuid();

        user.SetUpdatedBy(adminGuid);

        user.UpdatedByUserGuid.Should().Be(adminGuid);
        user.UpdatedDate.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    #endregion
    
    #region BaseDeletedEntity

    /// <summary>
    /// Verifies that passing a GUID stores it in DeletedByUserGuid.
    /// </summary>
    [Fact]
    public void SetDeletedBy_WithGuid_SetsDeletedByUSerGuidAndDeletionDate()
    {
        var user = UserTestData.CreateUser();
        var adminGuid = Guid.NewGuid();

        user.SetDeletedBy(adminGuid);

        user.DeletedByUserGuid.Should().Be(adminGuid);
        user.DeletionDate.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }
    
    /// <summary>
    /// Verifies that in removes the deleted reverence in DeletedByUserGuid.
    /// </summary>
    [Fact]
    public void RemoveDeletedByReference_SetsDeletedByUserGuidAndDeletionDateNull()
    {
        var user = UserTestData.CreateUser();

        user.RemoveDeletion();

        user.DeletedByUserGuid.Should().Be(null);
        user.DeletionDate.Should().Be(null);
    }
    
    #endregion
}