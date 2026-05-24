using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Common.Interfaces;

public interface IUserRepository : IBaseRepository<UserEntity>
{
    Task<UserEntity?> GetUserByGuidWithAuditInformationAsync(Guid guid);
    
    Task<UserEntity?> GetUserByGuid(Guid guid);
}