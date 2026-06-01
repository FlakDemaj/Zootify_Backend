using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

public interface IUserSettingsRepository : IBaseRepository<UserSettingsEntity>
{
    Task<UserSettingsEntity?> GetByUserGuidAsync(Guid userGuid);
}
