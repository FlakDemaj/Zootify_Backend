using Zootify_Backend;

namespace Zootify.Tests.Unit.Domain;

/// <summary>
/// Tests for Collection Entity.
/// </summary>
public class CollectionEntityTests
{
    private static CollectionEntity CreateCollection(
        string name = "My Collection",
        string? description = null,
        bool isPrivate = false,
        CollectionStatus status = CollectionStatus.Active)
    {
        return new CollectionEntity
        {
            Name = name,
            Description = description,
            IsPrivate = isPrivate,
            Status = status
        };
    }

    #region DeleteCollection

    /// <summary>
    /// Tests Delete Collection – Sets Status To Deleted.
    /// </summary>
    [Fact]
    public void DeleteCollection_SetsStatusToDeleted()
    {
        var collection = CreateCollection();

        collection.DeleteCollection(Guid.CreateVersion7());

        collection.Status.Should().Be(CollectionStatus.Deleted);
    }

    /// <summary>
    /// Tests Delete Collection – Sets Deleted By User Guid.
    /// </summary>
    [Fact]
    public void DeleteCollection_SetsDeletedByUserGuid()
    {
        var collection = CreateCollection();
        var requester = Guid.CreateVersion7();

        collection.DeleteCollection(requester);

        collection.DeletedByUserGuid.Should().Be(requester);
    }

    /// <summary>
    /// Tests Delete Collection – Sets Deletion Date.
    /// </summary>
    [Fact]
    public void DeleteCollection_SetsDeletionDate()
    {
        var collection = CreateCollection();
        var before = DateTime.UtcNow;

        collection.DeleteCollection(Guid.CreateVersion7());

        collection.DeletionDate.Should().BeOnOrAfter(before);
    }

    #endregion

    #region ActivateCollection

    /// <summary>
    /// Tests Activate Collection – Sets Status To Active.
    /// </summary>
    [Fact]
    public void ActivateCollection_SetsStatusToActive()
    {
        var collection = CreateCollection(status: CollectionStatus.Deleted);
        collection.DeleteCollection(Guid.CreateVersion7());

        collection.ActivateCollection(Guid.CreateVersion7());

        collection.Status.Should().Be(CollectionStatus.Active);
    }

    /// <summary>
    /// Tests Activate Collection – Clears Deletion Date.
    /// </summary>
    [Fact]
    public void ActivateCollection_ClearsDeletionDate()
    {
        var collection = CreateCollection();
        collection.DeleteCollection(Guid.CreateVersion7());

        collection.ActivateCollection(Guid.CreateVersion7());

        collection.DeletionDate.Should().BeNull();
    }

    /// <summary>
    /// Tests Activate Collection – Clears Deleted By User Guid.
    /// </summary>
    [Fact]
    public void ActivateCollection_ClearsDeletedByUserGuid()
    {
        var collection = CreateCollection();
        collection.DeleteCollection(Guid.CreateVersion7());

        collection.ActivateCollection(Guid.CreateVersion7());

        collection.DeletedByUserGuid.Should().BeNull();
    }

    /// <summary>
    /// Tests Activate Collection – Sets Updated By User Guid.
    /// </summary>
    [Fact]
    public void ActivateCollection_SetsUpdatedByUserGuid()
    {
        var collection = CreateCollection();
        collection.DeleteCollection(Guid.CreateVersion7());
        var activator = Guid.CreateVersion7();

        collection.ActivateCollection(activator);

        collection.UpdatedByUserGuid.Should().Be(activator);
    }

    #endregion

    #region CheckChanges

    /// <summary>
    /// Tests Check Changes – Identical Data – Returns False.
    /// </summary>
    [Fact]
    public void CheckChanges_IdenticalData_ReturnsFalse()
    {
        var collection = CreateCollection(name: "A", description: "B", isPrivate: true);
        var same = CreateCollection(name: "A", description: "B", isPrivate: true);

        collection.CheckChanges(same).Should().BeFalse();
    }

    /// <summary>
    /// Tests Check Changes – Different Name – Returns True.
    /// </summary>
    [Fact]
    public void CheckChanges_DifferentName_ReturnsTrue()
    {
        var collection = CreateCollection(name: "Old");
        var updated = CreateCollection(name: "New");

        collection.CheckChanges(updated).Should().BeTrue();
    }

    /// <summary>
    /// Tests Check Changes – Different Description – Returns True.
    /// </summary>
    [Fact]
    public void CheckChanges_DifferentDescription_ReturnsTrue()
    {
        var collection = CreateCollection(description: null);
        var updated = CreateCollection(description: "new desc");

        collection.CheckChanges(updated).Should().BeTrue();
    }

    /// <summary>
    /// Tests Check Changes – Different Is Private – Returns True.
    /// </summary>
    [Fact]
    public void CheckChanges_DifferentIsPrivate_ReturnsTrue()
    {
        var collection = CreateCollection(isPrivate: false);
        var updated = CreateCollection(isPrivate: true);

        collection.CheckChanges(updated).Should().BeTrue();
    }

    #endregion

    #region UpdateData

    /// <summary>
    /// Tests Update Data – Copies All Fields.
    /// </summary>
    [Fact]
    public void UpdateData_CopiesAllFields()
    {
        var collection = CreateCollection(name: "Old", description: null, isPrivate: false);
        var newData = CreateCollection(name: "New", description: "Desc", isPrivate: true);
        var requester = Guid.CreateVersion7();

        collection.UpdateData(newData, requester);

        collection.Name.Should().Be("New");
        collection.Description.Should().Be("Desc");
        collection.IsPrivate.Should().BeTrue();
    }

    /// <summary>
    /// Tests Update Data – Sets Updated By User Guid.
    /// </summary>
    [Fact]
    public void UpdateData_SetsUpdatedByUserGuid()
    {
        var collection = CreateCollection();
        var requester = Guid.CreateVersion7();

        collection.UpdateData(CreateCollection(name: "X"), requester);

        collection.UpdatedByUserGuid.Should().Be(requester);
    }

    #endregion
}
