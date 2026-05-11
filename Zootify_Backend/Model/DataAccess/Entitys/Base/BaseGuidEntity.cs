namespace Zootify_Backend.Models.DataAccess.Entitys.Base;

public abstract class BaseGuidEntity
{
    public Guid Guid { get; private set; } = Guid.CreateVersion7();
}