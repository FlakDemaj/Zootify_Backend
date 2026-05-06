namespace Zootify_Backend.Models.DataAccess.Entitys.Base;

public abstract class BaseDeletionEntity : BaseUpdateEntity
{
    public Guid? DeletedByUserGuid { get; private set; }
    public UserEntity? DeletedByUser { get; private set; }
    public DateTime? DeletionDate { get; private set; }
}