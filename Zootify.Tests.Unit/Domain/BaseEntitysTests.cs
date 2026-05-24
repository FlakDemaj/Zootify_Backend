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
    /// Verifies that passing <c>null</c> clears the UpdatedByUserGuid and still sets the UpdatedDate.
    /// </summary>
    [Fact]
    public void SetUpdatedBy_NullGuid_SetsUpdatedByUserGuidToNull()
    {
        var user = UserTestData.CreateUser();

        user.SetUpdatedBy(null);

        user.UpdatedByUserGuid.Should().BeNull();
        user.UpdatedDate.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that passing a GUID stores it in UpdatedByUserGuid.
    /// </summary>
    [Fact]
    public void SetUpdatedBy_WithGuid_SetsUpdatedByUserGuid()
    {
        var user = UserTestData.CreateUser();
        var adminGuid = Guid.NewGuid();

        user.SetUpdatedBy(adminGuid);

        user.UpdatedByUserGuid.Should().Be(adminGuid);
    }

    #endregion
}