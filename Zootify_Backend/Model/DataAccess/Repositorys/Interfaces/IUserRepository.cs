using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Models.DataAccess.Repositorys.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

public interface IUserRepository : IBaseRepository<UserEntity>
{
    public Task<UserEntity?> GetUserByEmail(string email, bool tracking = false);

    public Task<UserEntity?> GetUserByResetPasswordTokenWithResetPassword
        (string token);

    public Task<UserEntity?> GetUserByEmailWithResetPassword(
        string email);
    
    public Task<UserEntity?> GetUserByRegistrationTokenAsync(string email);

    public Task<UserEntity?> GetUserByEmailWithRegistrationToken(string email);
}