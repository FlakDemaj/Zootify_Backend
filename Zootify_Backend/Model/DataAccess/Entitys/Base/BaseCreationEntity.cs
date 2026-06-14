namespace Zootify_Backend.Model.DataAccess.Entitys.Base;

/// <summary>
/// Extends <see cref="BaseGuidEntity"/> with audit fields tracking who created the entity and when.
/// </summary>
public abstract class BaseCreationEntity : BaseGuidEntity
{
    public Guid? CreatedByUserGuid { get; private set; }
    public UserEntity? CreatedByUser { get; private set; }
    public DateTime CreationDate { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Records the user who created this entity and sets the creation timestamp to now.
    /// </summary>
    /// <param name="userGuid">The GUID of the user who created the entity.</param>
    public void SetCreatedBy(Guid userGuid)
    {
        CreatedByUserGuid = userGuid;
        CreationDate = DateTime.UtcNow;
    }
    
}