using Zootify_Backend.Models.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.Entitys.Base;

/// <summary>
/// Extends <see cref="BaseCreationEntity"/> with audit fields tracking the last modification.
/// </summary>
public abstract class BaseUpdateEntity : BaseCreationEntity
{
    public Guid? UpdatedByUserGuid { get; private set; }
    public UserEntity? UpdatedByUser { get; private set; }
    public DateTime? UpdatedDate { get; private set; }

    /// <summary>
    /// Records the user who performed the last update and sets the update timestamp to now.
    /// Pass <c>null</c> for system-triggered updates (e.g., token confirmation).
    /// </summary>
    public void SetUpdatedBy(Guid? updatedBy)
    {
        UpdatedByUserGuid = updatedBy;
        UpdatedDate = DateTime.UtcNow;
    }
}
