using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Models.DataAccess.DBContext;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

public class UserSettingsRepository : BaseRepository<UserSettingsEntity>, IUserSettingsRepository
{
    public UserSettingsRepository(ZootifyDbContext context) : base(context)
    {
    }

    public Task<UserSettingsEntity?> GetByUserGuidAsync(Guid userGuid)
    {
        return Context
            .GetTrackingSet<UserSettingsEntity>()
            .FirstOrDefaultAsync(s => s.UserGuid == userGuid);
    }
}
