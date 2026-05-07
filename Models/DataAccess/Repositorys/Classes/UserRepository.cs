using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Enums;
using Zootify_Backend.Extensions;
using Zootify_Backend.Models.DataAccess.DBContext;
using Zootify_Backend.Models.DataAccess.Entitys;
using Zootify_Backend.Models.DataAccess.Repositorys.Interfaces;

namespace Zootify_Backend.Models.DataAccess.Repositorys.Classes;

public class UserRepository : BaseRepository<UserEntity>, IUserRepository
{
    public UserRepository(ZootifyDbContext context)
        : base(context)
    {
    }

    public async Task<UserEntity?> GetUserByEmail(string email)
    {
        return await 
            Context
                .GetNoTrackingSet<UserEntity>()
                .Where(x => x.Email == email)
                .FirstOrDefaultAsync();
    }
    
    public async Task<UserEntity?> GetUserByEmailWithRegistrationToken(string email)
    {
        return await 
            Context
                .GetTrackingSet<UserEntity>()
                .Include(u => u.RegistrationToken)
                .Where(x => x.Email == email)
                .FirstOrDefaultAsync();
    }
    
    public async Task<UserEntity?> GetUserByRegistrationTokenAsync(string token)
    {
        return await
            Context
                .GetTrackingSet<UserEntity>()
                .Include(u => u.RegistrationToken)
                .Where(u => u.RegistrationToken != null
                            && u.RegistrationToken.Token == token)
                .FirstOrDefaultAsync();
    }
}