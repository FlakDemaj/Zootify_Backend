using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

public class CollectionEntity : BaseDeletionEntity
{
    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool IsPrivate { get; set; }

    public CollectionStatus Status { get; set; } = CollectionStatus.Active;

    public ICollection<SpecimenCollectionEntity> Collection { get; set; } = new List<SpecimenCollectionEntity>();

    public bool CheckChanges(CollectionEntity newData)
    {
        return Name != newData.Name
            || Description != newData.Description
            || IsPrivate != newData.IsPrivate;
    }

    public void DeleteCollection(Guid requesterGuid)
    {
        Status = CollectionStatus.Deleted;
        SetDeletedBy(requesterGuid);
    }

    public void ActivateCollection(Guid requesterGuid)
    {
        Status = CollectionStatus.Active;
        RemoveDeletion();
        SetUpdatedBy(requesterGuid);
    }

    public void UpdateData(CollectionEntity newData, Guid requesterGuid)
    {
        Name = newData.Name;
        Description = newData.Description;
        IsPrivate = newData.IsPrivate;
        SetUpdatedBy(requesterGuid);
    }
}