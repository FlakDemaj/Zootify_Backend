using Zootify_Backend;

namespace Zootify.Tests.Unit.Domain;

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

    [Fact]
    public void DeleteCollection_SetsStatusToDeleted()
    {
        var collection = CreateCollection();

        collection.DeleteCollection(Guid.CreateVersion7());

        collection.Status.Should().Be(CollectionStatus.Deleted);
    }

    [Fact]
    public void DeleteCollection_SetsDeletedByUserGuid()
    {
        var collection = CreateCollection();
        var requester = Guid.CreateVersion7();

        collection.DeleteCollection(requester);

        collection.DeletedByUserGuid.Should().Be(requester);
    }

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

    [Fact]
    public void ActivateCollection_SetsStatusToActive()
    {
        var collection = CreateCollection(status: CollectionStatus.Deleted);
        collection.DeleteCollection(Guid.CreateVersion7());

        collection.ActivateCollection(Guid.CreateVersion7());

        collection.Status.Should().Be(CollectionStatus.Active);
    }

    [Fact]
    public void ActivateCollection_ClearsDeletionDate()
    {
        var collection = CreateCollection();
        collection.DeleteCollection(Guid.CreateVersion7());

        collection.ActivateCollection(Guid.CreateVersion7());

        collection.DeletionDate.Should().BeNull();
    }

    [Fact]
    public void ActivateCollection_ClearsDeletedByUserGuid()
    {
        var collection = CreateCollection();
        collection.DeleteCollection(Guid.CreateVersion7());

        collection.ActivateCollection(Guid.CreateVersion7());

        collection.DeletedByUserGuid.Should().BeNull();
    }

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

    [Fact]
    public void CheckChanges_IdenticalData_ReturnsFalse()
    {
        var collection = CreateCollection(name: "A", description: "B", isPrivate: true);
        var same = CreateCollection(name: "A", description: "B", isPrivate: true);

        collection.CheckChanges(same).Should().BeFalse();
    }

    [Fact]
    public void CheckChanges_DifferentName_ReturnsTrue()
    {
        var collection = CreateCollection(name: "Old");
        var updated = CreateCollection(name: "New");

        collection.CheckChanges(updated).Should().BeTrue();
    }

    [Fact]
    public void CheckChanges_DifferentDescription_ReturnsTrue()
    {
        var collection = CreateCollection(description: null);
        var updated = CreateCollection(description: "new desc");

        collection.CheckChanges(updated).Should().BeTrue();
    }

    [Fact]
    public void CheckChanges_DifferentIsPrivate_ReturnsTrue()
    {
        var collection = CreateCollection(isPrivate: false);
        var updated = CreateCollection(isPrivate: true);

        collection.CheckChanges(updated).Should().BeTrue();
    }

    #endregion

    #region UpdateData

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
