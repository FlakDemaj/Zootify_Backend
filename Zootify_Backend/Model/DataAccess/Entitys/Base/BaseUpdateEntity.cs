using Zootify_Backend.Models.DataAccess.Entitys;
using Zootify_Backend.Models.DataAccess.Entitys.Base;

namespace Zootify_Backend.Model.DataAccess.Entitys.Base;

public abstract class BaseUpdateEntity : BaseCreationEntity
{
    public Guid? UpdatedByUserGuid { get; private set; }
    public UserEntity? UpdatedByUser { get; private set; }
    public DateTime? UpdatedDate { get; private set; }

    public void SetUpdatedBy(Guid? updatedBy)
    {
        UpdatedByUserGuid = updatedBy;
        UpdatedDate = DateTime.UtcNow;
    }
    
}