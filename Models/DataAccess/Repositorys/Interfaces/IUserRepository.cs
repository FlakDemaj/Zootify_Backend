using Zootify_Backend.Models.DataAccess.Entitys;

namespace Zootify_Backend.Models.DataAccess.Repositorys.Interfaces;

public interface IUserRepository : IBaseRepository<UserEntity>
{
    public Task<UserEntity?> GetUserByEmail(string email);
    
    public Task<UserEntity?> GetUserByRegistrationTokenAsync(string email);
}