using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Models.DataAccess.DBContext;
using Zootify_Backend.Models.DataAccess.Repositorys.Classes;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

public class UserRepository : BaseRepository<UserEntity>, IUserRepository
{
    public UserRepository(ZootifyDbContext context)
        : base(context)
    {
    }

    public async Task<UserEntity?> GetUserByEmail(
        string email, bool tracking = false)
    {
        return await 
            Context
                .GetTrackingSet<UserEntity>(tracking)
                .Where(x => x.Email == email)
                .FirstOrDefaultAsync();
    }
    
    public async Task<UserEntity?> GetUserByEmailWithResetPassword(
        string email)
    {
        return await 
            Context
                .GetTrackingSet<UserEntity>(true)
                .Include(u => u.ResetPasswordToken)
                .Where(x => x.Email == email)
                .FirstOrDefaultAsync();
    }

    public async Task<UserEntity?> GetUserByResetPasswordTokenWithResetPassword(
        string token)
    {
        return await
            Context
                .GetTrackingSet<UserEntity>(true)
                .Include(u => u.ResetPasswordToken)
                .Where(u => u.ResetPasswordToken != null
                            && u.ResetPasswordToken.Token == token)
                .FirstOrDefaultAsync();
    }

    public async Task<UserEntity?> GetUserByEmailWithRegistrationToken(string email)
    {
        return await 
            Context
                .GetTrackingSet<UserEntity>(true)
                .Include(u => u.RegistrationToken)
                .Where(x => x.Email == email)
                .FirstOrDefaultAsync();
    }
    
    public async Task<UserEntity?> GetUserByRegistrationTokenAsync(string token)
    {
        return await
            Context
                .GetTrackingSet<UserEntity>(true)
                .Include(u => u.RegistrationToken)
                .Where(u => u.RegistrationToken != null
                            && u.RegistrationToken.Token == token)
                .FirstOrDefaultAsync();
    }
}