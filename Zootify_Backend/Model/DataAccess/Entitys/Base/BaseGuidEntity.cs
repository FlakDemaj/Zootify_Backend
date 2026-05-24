namespace Zootify_Backend.Model.DataAccess.Entitys.Base;

public abstract class BaseGuidEntity
{
    public Guid Guid { get; private set; } = Guid.CreateVersion7();
    
}