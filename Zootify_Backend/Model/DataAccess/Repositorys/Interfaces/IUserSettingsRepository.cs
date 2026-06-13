using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

/// <summary>
/// Extends <see cref="IBaseRepository{T}"/> with user settings query operations.
/// </summary>
public interface IUserSettingsRepository : IBaseRepository<UserSettingsEntity>
{
    /// <summary>
    /// Returns the settings record for the given user GUID, or <c>null</c> if not found.
    /// </summary>
    /// <param name="userGuid">The GUID of the user whose settings to retrieve.</param>
    Task<UserSettingsEntity?> GetByUserGuidAsync(Guid userGuid);
}
