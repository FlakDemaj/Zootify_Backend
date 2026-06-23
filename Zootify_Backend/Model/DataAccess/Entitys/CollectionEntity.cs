using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

/// <summary>
/// Represents a user-owned collection that groups specimens together.
/// </summary>
public class CollectionEntity : BaseDeletionEntity
{
    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool IsPublic { get; set; } = true;

    public CollectionStatus Status { get; set; } = CollectionStatus.Active;

    public ICollection<SpecimenCollectionEntity> Collection { get; set; } = new List<SpecimenCollectionEntity>();

    /// <summary>
    /// Determines whether the editable collection fields differ from the given data.
    /// </summary>
    /// <param name="newData">The candidate data to compare against the current values.</param>
    /// <returns><c>true</c> if name, description, or visibility differ; otherwise <c>false</c>.</returns>
    public bool CheckChanges(CollectionEntity newData)
    {
        return Name != newData.Name
            || Description != newData.Description
            || IsPublic != newData.IsPublic;
    }

    /// <summary>
    /// Soft-deletes the collection by setting its status to deleted, recording the requester
    /// and set the collection to private.
    /// </summary>
    /// <param name="requesterGuid">The GUID of the user performing the deletion.</param>
    public void DeleteCollection(Guid requesterGuid)
    {
        Status = CollectionStatus.Deleted;
        IsPublic = false;
        SetDeletedBy(requesterGuid);
    }

    /// <summary>
    /// Restores a soft-deleted collection to active status and records the requester as the updater.
    /// </summary>
    /// <param name="requesterGuid">The GUID of the user performing the activation.</param>
    public void ActivateCollection(Guid requesterGuid)
    {
        Status = CollectionStatus.Active;
        RemoveDeletion();
        SetUpdatedBy(requesterGuid);
    }

    /// <summary>
    /// Applies the given collection data to this entity and records the requester as the updater.
    /// </summary>
    /// <param name="newData">The new data to apply.</param>
    /// <param name="requesterGuid">The GUID of the user performing the update.</param>
    public void UpdateData(CollectionEntity newData, Guid requesterGuid)
    {
        Name = newData.Name;
        Description = newData.Description;
        IsPublic = newData.IsPublic;
        SetUpdatedBy(requesterGuid);
    }
}