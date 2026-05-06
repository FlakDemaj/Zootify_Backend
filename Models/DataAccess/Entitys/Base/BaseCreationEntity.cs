namespace Zootify_Backend.Models.DataAccess.Entitys.Base;

public abstract class BaseCreationEntity : BaseGuidEntity
{
    public Guid? CreatedByUserGuid { get; private set; }
    public UserEntity? CreatedByUser { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    
}