namespace Zootify_Backend.Models.DataAccess.Entitys.Base;

public class BaseDeletionEntity : BaseUpdateEntity
{
    public Guid? DeletedBy { get; set; }
    public UserEntity? DeletedByUser { get; set; }
    public DateTime DeletionDate { get; set; } = DateTime.UtcNow;
}