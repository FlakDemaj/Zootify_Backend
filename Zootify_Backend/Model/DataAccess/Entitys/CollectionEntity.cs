using Zootify_Backend.Model.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys;

public class CollectionEntity : BaseDeletionEntity
{
    public string Name { get; set; }

    public string? Description { get; set; }

    public bool IsPrivate { get; set; }

    public ICollection<SpecimenCollectionEntity> Collection { get; set; } = new List<SpecimenCollectionEntity>();

    public bool CheckChanges(CollectionEntity newData) =>
        Name != newData.Name
        || Description != newData.Description
        || IsPrivate != newData.IsPrivate;

    public void UpdateData(CollectionEntity newData, Guid requesterGuid)
    {
        Name = newData.Name;
        Description = newData.Description;
        IsPrivate = newData.IsPrivate;
        SetUpdatedBy(requesterGuid);
    }
}