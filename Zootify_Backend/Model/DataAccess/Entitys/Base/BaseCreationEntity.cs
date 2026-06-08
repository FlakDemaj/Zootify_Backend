namespace Zootify_Backend.Model.DataAccess.Entitys.Base;

public abstract class BaseCreationEntity : BaseGuidEntity
{
    public Guid? CreatedByUserGuid { get; private set; }
    public UserEntity? CreatedByUser { get; private set; }
    public DateTime CreationDate { get; private set; } = DateTime.UtcNow;

    public void SetCreatedBy(Guid userGuid)
    {
        CreatedByUserGuid = userGuid;
        CreationDate = DateTime.UtcNow;
    }
    
}