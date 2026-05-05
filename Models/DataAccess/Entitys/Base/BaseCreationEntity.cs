namespace Zootify_Backend.Models.DataAccess.Entitys.Base;

public class BaseCreationEntity : BaseGuidEntity
{
    public Guid? CreatedBy { get; set; }
    public UserEntity? CreatedByUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
}