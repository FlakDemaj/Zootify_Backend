using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Enums;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Models.DataAccess.DBContext;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

public class CollectionRepository : BaseRepository<CollectionEntity>, ICollectionRepository
{
    public CollectionRepository(ZootifyDbContext context) : base(context)
    {
    }

    public async Task<List<CollectionEntity>> GetAllCollectionsByNameFilterAsync(
        IAuthentication? authentication,
        string? filterByName,
        string? orderBy,
        string? orderDirection)
    {
        var query = Context.GetTrackingSet<CollectionEntity>();

        query = AddFilter(query, authentication, filterByName, orderBy, orderDirection);

        return await query.ToListAsync();
    }

    public async Task<CollectionEntity?> GetCollectionByGuidAsync(Guid guid)
    {
        return await Context.GetTrackingSet<CollectionEntity>()
            .Include(c => c.Collection)
            .FirstOrDefaultAsync(c => c.Guid == guid);
    }

    public async Task<CollectionEntity?> GetCollectionWithAuditAsync(Guid guid)
    {
        return await Context.GetTrackingSet<CollectionEntity>()
            .Include(c => c.CreatedByUser)
            .Include(c => c.UpdatedByUser)
            .Include(c => c.DeletedByUser)
            .FirstOrDefaultAsync(c => c.Guid == guid);
    }

    private static IQueryable<CollectionEntity> AddFilter(
        IQueryable<CollectionEntity> query,
        IAuthentication? authentication,
        string? filterByName,
        string? orderBy,
        string? orderDirection)
    {
        if (authentication == null)
        {
            query = query.Where(c => !c.IsPrivate && c.Status == CollectionStatus.Active);
        }

        if (authentication != null && authentication.Role == UserRole.User)
        {
            query = query.Where(c => (!c.IsPrivate && c.Status == CollectionStatus.Active)
                                     || c.CreatedByUserGuid == authentication.UserGuid);
        }

        if (!string.IsNullOrWhiteSpace(filterByName))
        {
            query = query.Where(c => c.Name.Contains(filterByName));
        }

        return ApplyOrderBy(query, orderBy, orderDirection);
    }

    private static IQueryable<CollectionEntity> ApplyOrderBy(
        IQueryable<CollectionEntity> query,
        string? orderBy,
        string? orderDirection)
    {
        var desc = string.Equals(orderDirection, "desc", StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(orderBy) && orderBy == "name")
        {
            return desc ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name);
        }

        if (!string.IsNullOrWhiteSpace(orderBy) && orderBy == "isPrivate")
        {
            return desc ? query.OrderByDescending(c => c.IsPrivate) : query.OrderBy(c => c.IsPrivate);
        }

        if (!string.IsNullOrWhiteSpace(orderBy) && orderBy == "createdAt")
        {
            return desc ? query.OrderByDescending(c => c.CreationDate) : query.OrderBy(c => c.CreationDate);
        }

        return query.OrderBy(c => c.Guid);
    }
}
