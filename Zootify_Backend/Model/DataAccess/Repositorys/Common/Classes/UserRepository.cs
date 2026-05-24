using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Enums;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Common.Interfaces;
using Zootify_Backend.Models.DataAccess.DBContext;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Common.Classes;

public class UserRepository : BaseRepository<UserEntity>, IUserRepository
{
    public UserRepository(ZootifyDbContext context) : base(context)
    {
    }

    public Task<UserEntity?> GetUserByGuidWithAuditInformationAsync(Guid guid)
    {
        return Context
            .GetTrackingSet<UserEntity>()
            .Include(u => u.UpdatedByUser)
            .Include(u => u.DeletedByUser)
            .FirstOrDefaultAsync(u => u.Guid == guid);
    }

    public async Task<UserEntity?> GetActiveUserByGuid(Guid guid)
    {
        return await Context.GetTrackingSet<UserEntity>()
            .FirstOrDefaultAsync(u => u.Guid == guid 
                                      && u.Status == UserStatus.Active);
    }

    public async Task<UserEntity?> GetDeletedUserByGuid(Guid guid)
    {
        return await Context.GetTrackingSet<UserEntity>()
            .FirstOrDefaultAsync(u => u.Guid == guid
                                      && u.Status == UserStatus.Deleted);    }
}