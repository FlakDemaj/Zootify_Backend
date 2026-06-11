using Microsoft.EntityFrameworkCore;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.Enums;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.DBContext;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

public class SpecimenRepository : BaseRepository<SpecimenEntity>, ISpecimenRepository
{
    public SpecimenRepository(ZootifyDbContext context) : base(context)
    {
    }

    public async Task<SpecimenEntity?> GetSpecimenByGuidAsync(
        Guid guid,
        bool includeImage = false,
        bool includeTags = false)
    {
        var query = Context.GetTrackingSet<SpecimenEntity>(true);

        if (includeTags)
        {
            query = query.Include(sp => sp.SpecimensTags);
        }

        if (includeImage)
        {
            query = query.Include(sp => sp.Image);
        }

        return await query.FirstOrDefaultAsync(sp => sp.Guid == guid);
    }

    public async Task<int> CountByStatusAsync(SpecimenStatus status, IAuthentication authentication)
    {
        return await Context.GetTrackingSet<SpecimenEntity>()
            .Where(sp => sp.CreatedByUserGuid == authentication.UserGuid)
            .Where(sp => sp.Status == status)
            .CountAsync();
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

    public async Task<(List<SpecimenEntity> Items, int TotalCount)> GetSpecimensInCollectionPagedAsync(
        Guid collectionGuid,
        IAuthentication? authentication,
        BaseSortAndPagingDto paging)
    {
        var query = Context.GetTrackingSet<SpecimenEntity>()
            .Where(sp => sp.Collections.Any(sc => sc.CollectionGuid == collectionGuid));

        if (authentication == null)
        {
            query = query.Where(sp => sp.IsPublic);
        }

        if (authentication != null && authentication.Role == UserRole.User)
        {
            query = query.Where(sp => sp.IsPublic || sp.CreatedByUserGuid == authentication.UserGuid);
        }

        var total = await query.CountAsync();

        var items = await ApplySort(query, paging.SortBy, paging.SortDirection)
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(List<SpecimenEntity> Items, int TotalCount)> GetSpecimensPagedAsync(
        Guid? userGuid,
        SpecimenStatus? status,
        BaseSortAndPagingDto paging)
    {
        var query = Context.GetTrackingSet<SpecimenEntity>();

        if (userGuid.HasValue)
        {
            query = query.Where(sp => sp.CreatedByUserGuid == userGuid.Value);
        }
        else
        {
            query = query.Where(sp => sp.IsPublic);
        }

        if (status.HasValue)
        {
            query = query.Where(sp => sp.Status == status.Value);
        }

        var total = await query.CountAsync();

        var items = await ApplySort(query, paging.SortBy, paging.SortDirection)
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<SpecimenEntity?> GetSpecimenWithDetailByGuidAsync(Guid guid)
    {
        return await Context.GetTrackingSet<SpecimenEntity>(true)
            .Include(sp => sp.CreatedByUser)
            .Include(sp => sp.UpdatedByUser)
            .Include(sp => sp.DeletedByUser)
            .Include(sp => sp.Image)
            .Include(sp => sp.Species)
            .Include(sp => sp.Collections)
            .Include(sp => sp.GeographicLocation)
                .ThenInclude(g => g.Parent)
                .ThenInclude(g => g.Parent)
                .ThenInclude(g => g.Parent)
                .ThenInclude(g => g.Parent)
                .ThenInclude(g => g.Parent)
            .FirstOrDefaultAsync(sp => sp.Guid == guid);
    }

    public async Task<List<SpecimenEntity>> GetSpecimensBySpeciesGuidsAsync(
        List<Guid> speciesGuids,
        SpecimenStatus? status = null)
    {
        var query = Context.GetTrackingSet<SpecimenEntity>(true)
            .Where(sp => sp.TaxonomySpeciesGuid.HasValue
                         && speciesGuids.Contains(sp.TaxonomySpeciesGuid.Value));

        if (status.HasValue)
        {
            query = query.Where(sp => sp.Status == status.Value);
        }

        return await query.ToListAsync();
    }

    private static IQueryable<SpecimenEntity> ApplySort(
        IQueryable<SpecimenEntity> query,
        string? sortBy,
        SortDirection sortDirection)
    {
        var desc = sortDirection == SortDirection.Desc;
        return sortBy switch
        {
            "name"            => desc ? query.OrderByDescending(sp => sp.Name)            : query.OrderBy(sp => sp.Name),
            "latinName"       => desc ? query.OrderByDescending(sp => sp.LatinName)       : query.OrderBy(sp => sp.LatinName),
            "status"          => desc ? query.OrderByDescending(sp => sp.Status)          : query.OrderBy(sp => sp.Status),
            "dateOfDiscovery" => desc ? query.OrderByDescending(sp => sp.DateOfDiscovery) : query.OrderBy(sp => sp.DateOfDiscovery),
            "isPublic"        => desc ? query.OrderByDescending(sp => sp.IsPublic)        : query.OrderBy(sp => sp.IsPublic),
            _                 => query.OrderBy(sp => sp.Guid),
        };
    }

    public async Task<List<SpecimenEntity>> GetDeletedSpecimensForPurgeAsync(DateTime cutoff, int batchSize)
    {
        return await Context.GetTrackingSet<SpecimenEntity>()
            .Where(sp => sp.Status == SpecimenStatus.Deleted
                && sp.DeletionDate != null
                && sp.DeletionDate < cutoff)
            .OrderBy(sp => sp.DeletionDate)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task<List<SpecimenEntity>> GetRejectedSpecimensForPurgeAsync(DateTime cutoff, int batchSize)
    {
        return await Context.GetTrackingSet<SpecimenEntity>()
            .Where(sp => sp.Status == SpecimenStatus.Rejected
                && sp.RejectedAt != null
                && sp.RejectedAt < cutoff)
            .OrderBy(sp => sp.RejectedAt)
            .Take(batchSize)
            .ToListAsync();
    }

    private IQueryable<SpecimenEntity> CreateSpecimenQuery()
    {
        return Context.GetTrackingSet<SpecimenEntity>()
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
