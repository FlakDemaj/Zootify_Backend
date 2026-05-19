using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Common.Interfaces;

public interface IUserRepository
{
    Task<UserEntity?> GetUserByGuidWithAuditInformationAsync(Guid guid);
}