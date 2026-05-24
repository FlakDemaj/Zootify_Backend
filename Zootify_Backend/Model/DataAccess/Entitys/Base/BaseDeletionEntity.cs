namespace Zootify_Backend.Model.DataAccess.Entitys.Base;

public abstract class BaseDeletionEntity : BaseUpdateEntity
{
    public Guid? DeletedByUserGuid { get; private set; }
    public UserEntity? DeletedByUser { get; private set; }
    public DateTime? DeletionDate { get; private set; }
    
    public void SetDeletedBy(Guid updatedBy)
    {
        DeletedByUserGuid = updatedBy;
        DeletionDate = DateTime.UtcNow;
    }
    
    public void RemoveDeletion()
    {
        DeletedByUserGuid = null;
        DeletionDate = null;
    }
}