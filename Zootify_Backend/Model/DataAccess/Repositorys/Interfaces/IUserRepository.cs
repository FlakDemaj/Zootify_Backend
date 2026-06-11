using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

public interface IUserRepository : IBaseRepository<UserEntity>
{
    Task<UserEntity?> GetUserByGuidWithAuditInformationAsync(Guid guid);

    Task<UserEntity?> GetActiveUserByGuid(Guid guid);

    Task<UserEntity?> GetDeletedUserByGuid(Guid guid);

    Task<List<UserEntity>> GetAllUsersAsync(
        GetAllUsersFilterDto query);
    
    Task<List<UserEntity>> GetAllAdminsAndModeratorUsersWithSettingsAsync();

    Task<List<UserEntity>> GetUsersByGuidsAsync(List<Guid> guids);

    Task<List<UserEntity>> GetActiveUsersByNameStartsWithAsync(string startsWith, int limit = 20);

    Task<List<UserEntity>> GetDeletedUsersForPurgeAsync(DateTime cutoff, int batchSize);
}