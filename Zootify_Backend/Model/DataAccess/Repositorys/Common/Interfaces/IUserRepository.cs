using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Common.Interfaces;

public interface IUserRepository : IBaseRepository<UserEntity>
{
    Task<UserEntity?> GetUserByGuidWithAuditInformationAsync(Guid guid);

    Task<UserEntity?> GetActiveUserByGuid(Guid guid);

    Task<UserEntity?> GetDeletedUserByGuid(Guid guid);

    Task<List<UserEntity>> GetAllUsersAsync();
}