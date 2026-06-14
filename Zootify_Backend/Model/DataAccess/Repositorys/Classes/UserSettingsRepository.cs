using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.DBContext;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

/// <summary>
/// Repository for querying and persisting <see cref="UserSettingsEntity"/> records.
/// </summary>
public class UserSettingsRepository : BaseRepository<UserSettingsEntity>, IUserSettingsRepository
{
    /// <summary>
    /// Initializes a new <see cref="UserSettingsRepository"/> with the given database context.
    /// </summary>
    /// <param name="context">The database context to use for persistence operations.</param>
    public UserSettingsRepository(ZootifyDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Returns the settings record for the user with the given GUID, or <c>null</c> if not found.
    /// </summary>
    /// <param name="userGuid">The GUID of the user whose settings are retrieved.</param>
    public Task<UserSettingsEntity?> GetByUserGuidAsync(Guid userGuid)
    {
        return Context
            .GetTrackingSet<UserSettingsEntity>()
            .FirstOrDefaultAsync(s => s.UserGuid == userGuid);
    }
}
