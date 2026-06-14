namespace Zootify_Backend.Model.DataAccess.Entitys.Base;

/// <summary>
/// Extends <see cref="BaseUpdateEntity"/> with audit fields tracking soft-deletion.
/// </summary>
public abstract class BaseDeletionEntity : BaseUpdateEntity
{
    public Guid? DeletedByUserGuid { get; private set; }
    public UserEntity? DeletedByUser { get; private set; }
    public DateTime? DeletionDate { get; private set; }
    
    /// <summary>
    /// Records the user who soft-deleted this entity and sets the deletion timestamp to now.
    /// </summary>
    /// <param name="updatedBy">The GUID of the user who performed the deletion.</param>
    public void SetDeletedBy(Guid updatedBy)
    {
        DeletedByUserGuid = updatedBy;
        DeletionDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Clears the soft-deletion audit fields, restoring the entity to a non-deleted state.
    /// </summary>
    public void RemoveDeletion()
    {
        DeletedByUserGuid = null;
        DeletionDate = null;
    }
}