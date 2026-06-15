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

/// <summary>
/// Repository for querying and persisting <see cref="SpecimenEntity"/> records.
/// </summary>
public class SpecimenRepository : BaseRepository<SpecimenEntity>, ISpecimenRepository
{
    /// <summary>
    /// Initializes a new <see cref="SpecimenRepository"/> with the given database context.
    /// </summary>
    /// <param name="context">The database context to use for persistence operations.</param>
    public SpecimenRepository(ZootifyDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Returns the specimen with the given GUID, optionally loading image and tag data, or <c>null</c> if not found.
    /// </summary>
    /// <param name="guid">The GUID of the specimen to retrieve.</param>
    /// <param name="includeImage">Whether to eagerly load the specimen's image.</param>
    /// <param name="includeTags">Whether to eagerly load the specimen's tag associations.</param>
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

    /// <summary>
    /// Returns the number of specimens owned by the authenticated user that have the given status.
    /// </summary>
    /// <param name="status">The specimen status to count.</param>
    /// <param name="authentication">The authentication context of the user whose specimens are counted.</param>
    public async Task<int> CountByStatusAsync(SpecimenStatus status, IAuthentication authentication)
    {
        return await Context.GetTrackingSet<SpecimenEntity>()
            .Where(sp => sp.CreatedByUserGuid == authentication.UserGuid)
            .Where(sp => sp.Status == status)
            .CountAsync();
    }

    /// <summary>
    /// Returns the total number of specimens created by the given user.
    /// </summary>
    /// <param name="userGuid">The GUID of the user whose specimens are counted.</param>
    public async Task<int> CountOwnAsync(Guid userGuid)
    {
        return await Context.GetTrackingSet<SpecimenEntity>()
            .Where(sp => sp.CreatedByUserGuid == userGuid)
            .CountAsync();
    }

    /// <summary>
    /// Returns the total number of public specimens across all users.
    /// </summary>
    public async Task<int> CountPublicAsync()
    {
        return await Context.GetTrackingSet<SpecimenEntity>()
            .Where(sp => sp.IsPublic)
            .CountAsync();
    }

    /// <summary>
    /// Returns a paged list of specimens belonging to the specified collection, applying visibility rules based on the caller's authentication.
    /// </summary>
    /// <param name="collectionGuid">The GUID of the collection to retrieve specimens from.</param>
    /// <param name="authentication">The optional authentication context used for visibility filtering.</param>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <returns>A tuple containing the page of specimens and the total matching count.</returns>
    public async Task<(List<SpecimenEntity> Items, int TotalCount)> GetSpecimensInCollectionPagedAsync(
        Guid collectionGuid,
        string? filterByName,
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

        if (!string.IsNullOrWhiteSpace(filterByName))
        {
            query = query.Where(sp => sp.Name.Contains(filterByName));
        }

        var total = await query.CountAsync();

        var items = await ApplySort(query, paging.SortBy, paging.SortDirection)
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync();

        return (items, total);
    }

    /// <summary>
    /// Returns a paged list of specimens, optionally filtered by owner and status.
    /// </summary>
    /// <param name="userGuid">Optional GUID of the owning user; if <c>null</c>, only public specimens are returned.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <returns>A tuple containing the page of specimens and the total matching count.</returns>
    public async Task<(List<SpecimenEntity> Items, int TotalCount)> GetSpecimensPagedAsync(
        Guid? userGuid,
        string? filterByName,
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

        if (!string.IsNullOrWhiteSpace(filterByName))
        {
            query = query.Where(sp => sp.Name.Contains(filterByName));
        }

        var total = await query.CountAsync();

        var items = await ApplySort(query, paging.SortBy, paging.SortDirection)
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync();

        return (items, total);
    }

    /// <summary>
    /// Returns a paged list of specimens for the public overview, ordered by creation date descending,
    /// eagerly loading the image, taxonomy chain, and geographic location chain. Non-public specimens
    /// are only included for moderators and admins; an optional city name filters the result.
    /// </summary>
    /// <param name="authentication">The optional authentication context; anonymous or regular users see only public specimens.</param>
    /// <param name="filterByCity">Optional case-insensitive city name to filter by; <c>null</c> for no filter.</param>
    /// <param name="paging">Paging parameters (page and page size).</param>
    /// <returns>A tuple containing the page of specimens and the total matching count.</returns>
    public async Task<(List<SpecimenEntity> Items, int TotalCount)> GetPublicOverviewPagedAsync(
        IAuthentication? authentication,
        string? filterByCity,
        BaseSortAndPagingDto paging)
    {
        // AsNoTracking (read-only); only to-one navigations are included, so paging stays a single query.
        var query = Context.GetTrackingSet<SpecimenEntity>()
            .Include(sp => sp.Image)
            // Five levels of Parent cover the full taxonomy depth (Phylum → … → Species),
            // so the order-rank ancestor is always loaded.
            .Include(sp => sp.Species)
                .ThenInclude(t => t!.Parent)
                .ThenInclude(t => t!.Parent)
                .ThenInclude(t => t!.Parent)
                .ThenInclude(t => t!.Parent)
                .ThenInclude(t => t!.Parent)
            // Five levels of Parent cover the full geographic hierarchy depth
            // (Country → State → City → Postcode → Street → HouseNumber).
            .Include(sp => sp.GeographicLocation)
                .ThenInclude(g => g.Parent)
                .ThenInclude(g => g.Parent)
                .ThenInclude(g => g.Parent)
                .ThenInclude(g => g.Parent)
                .ThenInclude(g => g.Parent)
            .AsQueryable();

        // Not logged in: only public. Regular users: public plus their own. Moderators and admins: everything.
        if (authentication == null)
        {
            query = query.Where(sp => sp.IsPublic);
        }
        else if (authentication.Role == UserRole.User)
        {
            query = query.Where(sp => sp.IsPublic
                                      || sp.CreatedByUserGuid == authentication.UserGuid);
        }

        if (!string.IsNullOrWhiteSpace(filterByCity))
        {
            var city = filterByCity.ToLower();

            // The city node may sit anywhere in the chain (intermediate levels can be missing), so the
            // leaf and its ancestors are checked. ToLower keeps the comparison case-insensitive.
            query = query.Where(sp =>
                (sp.GeographicLocation.Level == GeographicLevel.City
                 && sp.GeographicLocation.Name.ToLower() == city)
                || (sp.GeographicLocation.Parent!.Level == GeographicLevel.City
                    && sp.GeographicLocation.Parent!.Name.ToLower() == city)
                || (sp.GeographicLocation.Parent!.Parent!.Level == GeographicLevel.City
                    && sp.GeographicLocation.Parent!.Parent!.Name.ToLower() == city)
                || (sp.GeographicLocation.Parent!.Parent!.Parent!.Level == GeographicLevel.City
                    && sp.GeographicLocation.Parent!.Parent!.Parent!.Name.ToLower() == city)
                || (sp.GeographicLocation.Parent!.Parent!.Parent!.Parent!.Level == GeographicLevel.City
                    && sp.GeographicLocation.Parent!.Parent!.Parent!.Parent!.Name.ToLower() == city)
                || (sp.GeographicLocation.Parent!.Parent!.Parent!.Parent!.Parent!.Level == GeographicLevel.City
                    && sp.GeographicLocation.Parent!.Parent!.Parent!.Parent!.Parent!.Name.ToLower() == city));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(sp => sp.CreationDate)
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync();

        return (items, total);
    }

    /// <summary>
    /// Returns the specimens with the given GUIDs, eagerly loading their species and tags, for export.
    /// </summary>
    /// <param name="guids">The GUIDs of the specimens to load.</param>
    /// <returns>The matching specimens with their species and tags loaded.</returns>
    public async Task<List<SpecimenEntity>> GetSpecimensByGuidsWithSpeciesAsync(List<Guid> guids)
    {
        return await Context.GetTrackingSet<SpecimenEntity>()
            .Include(sp => sp.Species)
            .Include(sp => sp.SpecimensTags)
                .ThenInclude(st => st.Tag)
            .Where(sp => guids.Contains(sp.Guid))
            .ToListAsync();
    }

    /// <summary>
    /// Returns the specimen with the given GUID, eagerly loading all detail navigation properties including audit users, image, species, collections, and geographic location chain, or <c>null</c> if not found.
    /// </summary>
    /// <param name="guid">The GUID of the specimen to retrieve.</param>
    public async Task<SpecimenEntity?> GetSpecimenWithDetailByGuidAsync(Guid guid)
    {
        return await Context.GetTrackingSet<SpecimenEntity>(true)
            .Include(sp => sp.CreatedByUser)
            .Include(sp => sp.UpdatedByUser)
            .Include(sp => sp.DeletedByUser)
            .Include(sp => sp.Image)
            .Include(sp => sp.Species)
            .Include(sp => sp.Collections)
            // Five levels of Parent cover the full geographic hierarchy depth
            // (Country → State → City → Postcode → Street → HouseNumber); EF cannot
            // load a self-referencing chain recursively, so each level is included explicitly.
            .Include(sp => sp.GeographicLocation)
                .ThenInclude(g => g.Parent)
                .ThenInclude(g => g.Parent)
                .ThenInclude(g => g.Parent)
                .ThenInclude(g => g.Parent)
                .ThenInclude(g => g.Parent)
            .FirstOrDefaultAsync(sp => sp.Guid == guid);
    }

    /// <summary>
    /// Returns all specimens whose taxonomy species GUID is in the given list, applying optional visibility and status filters.
    /// </summary>
    /// <param name="speciesGuids">The list of species GUIDs to match against.</param>
    /// <param name="authentication">The optional authentication context used for visibility filtering.</param>
    /// <param name="status">Optional status filter.</param>
    public async Task<List<SpecimenEntity>> GetSpecimensBySpeciesGuidsAsync(
        List<Guid> speciesGuids,
        IAuthentication? authentication = null,
        SpecimenStatus? status = null)
    {
        var query = Context.GetTrackingSet<SpecimenEntity>(true)
            .Where(sp => sp.TaxonomySpeciesGuid.HasValue
                         && speciesGuids.Contains(sp.TaxonomySpeciesGuid.Value));

        if (authentication == null)
        {
            query = query.Where(sp => sp.IsPublic);
        }
        else if (authentication.Role == UserRole.User)
        {
            query = query.Where(sp => sp.IsPublic || sp.CreatedByUserGuid == authentication.UserGuid);
        }

        if (status.HasValue)
        {
            query = query.Where(sp => sp.Status == status.Value);
        }

        return await query.ToListAsync();
    }

    /// <summary>
    /// Applies ordering to the specimen query based on the given sort field and direction.
    /// </summary>
    /// <param name="query">The queryable to sort.</param>
    /// <param name="sortBy">The name of the field to sort by.</param>
    /// <param name="sortDirection">The sort direction.</param>
    /// <returns>The ordered queryable.</returns>
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

    /// <summary>
    /// Returns a batch of soft-deleted specimens whose deletion date is older than the given cutoff, for purging.
    /// </summary>
    /// <param name="cutoff">The exclusive upper bound for the deletion date.</param>
    /// <param name="batchSize">The maximum number of specimens to return.</param>
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

    /// <summary>
    /// Returns a batch of rejected specimens whose rejection date is older than the given cutoff, for purging.
    /// </summary>
    /// <param name="cutoff">The exclusive upper bound for the rejection date.</param>
    /// <param name="batchSize">The maximum number of specimens to return.</param>
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

    /// <summary>
    /// Builds a base queryable for <see cref="SpecimenEntity"/> with audit users and geographic location chain eagerly loaded.
    /// </summary>
    /// <returns>The base <see cref="IQueryable{SpecimenEntity}"/> with navigation properties included.</returns>
    private IQueryable<SpecimenEntity> CreateSpecimenQuery()
    {
        return Context.GetTrackingSet<SpecimenEntity>()
            .Include(sp => sp.CreatedByUser)
            .Include(sp => sp.UpdatedByUser)
            .Include(sp => sp.DeletedByUser)
            // Five levels of Parent cover the full geographic hierarchy depth
            // (Country → State → City → Postcode → Street → HouseNumber); EF cannot
            // load a self-referencing chain recursively, so each level is included explicitly.
            .Include(sp => sp.GeographicLocation)
            .ThenInclude(g => g.Parent)
            .ThenInclude(g => g.Parent)
            .ThenInclude(g => g.Parent)
            .ThenInclude(g => g.Parent)
            .ThenInclude(g => g.Parent);
    }
}
