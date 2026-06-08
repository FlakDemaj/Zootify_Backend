using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Enums;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Models.DataAccess.DBContext;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

public class SpecimenRepository : BaseRepository<SpecimenEntity>, ISpecimenRepository
{
    public SpecimenRepository(ZootifyDbContext context) : base(context)
    {
    }

    public async Task<SpecimenEntity?> GetSpecimenByGuidAsync(Guid guid)
    {
        return await Context.GetTrackingSet<SpecimenEntity>(true)
            .FirstOrDefaultAsync(s => s.Guid == guid);
    }

    public async Task<List<SpecimenEntity>> GetAllSpecimensByStatusAsync(
        IAuthentication authentication,
        SpecimenStatus status)
    {
        var query =
            CreateSpecimenQuery()
                .Where(sp => sp.Status == status);

        if (authentication.Role == UserRole.User)
        {
            query = query.Where(sp => sp.IsPublic);
        }

        if (authentication.Role == UserRole.User && status == SpecimenStatus.Pending)
        {
            query = query.Where(sp => sp.CreatedByUserGuid == authentication.UserGuid);
        }

        return await query.ToListAsync();
    }

    public async Task<List<SpecimenEntity>> GetOwnSpecimensAsync(Guid userGuid)
    {
        return await CreateSpecimenQuery()
            .Where(sp => sp.CreatedByUserGuid == userGuid)
            .ToListAsync();
    }

    public async Task<int> CountByStatusAsync(SpecimenStatus status, IAuthentication? authentication)
    {
        var query = Context.GetTrackingSet<SpecimenEntity>()
            .Where(sp => sp.Status == status);

        if (authentication == null || authentication.Role == UserRole.User)
            query = query.Where(sp => sp.IsPublic);

        if (authentication?.Role == UserRole.User)
            query = query.Where(sp => sp.CreatedByUserGuid == authentication.UserGuid);

        return await query.CountAsync();
    }

    public async Task<int> CountOwnAsync(Guid userGuid)
    {
        return await Context.GetTrackingSet<SpecimenEntity>()
            .Where(sp => sp.CreatedByUserGuid == userGuid)
            .CountAsync();
    }

    public async Task<int> CountPublicAsync()
    {
        return await Context.GetTrackingSet<SpecimenEntity>()
            .Where(sp => sp.IsPublic)
            .CountAsync();
    }

    private IQueryable<SpecimenEntity> CreateSpecimenQuery()
    {
        return
            Context
                .GetTrackingSet<SpecimenEntity>()
                .Include(sp => sp.CreatedByUser)
                .Include(sp => sp.UpdatedByUser)
                .Include(sp => sp.DeletedByUser)
                .Include(sp => sp.GeographicLocation)
                .ThenInclude(g => g.Parent)
                .ThenInclude(g => g.Parent)
                .ThenInclude(g => g.Parent)
                .ThenInclude(g => g.Parent)
                .ThenInclude(g => g.Parent);
    }
}