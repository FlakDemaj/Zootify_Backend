namespace Zootify_Backend.Models.DataAccess.Entitys.Base;

public class BaseUpdateEntity : BaseCreationEntity
{
    public Guid? UpdatedBy { get; set; }
    public UserEntity? UpdatedByUser { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
}