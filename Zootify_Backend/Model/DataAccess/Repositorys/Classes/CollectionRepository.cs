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
/// Repository for querying and persisting <see cref="CollectionEntity"/> records.
/// </summary>
public class CollectionRepository : BaseRepository<CollectionEntity>, ICollectionRepository
{
    /// <summary>
    /// Initializes a new <see cref="CollectionRepository"/> with the given database context.
    /// </summary>
    /// <param name="context">The database context to use for persistence operations.</param>
    public CollectionRepository(ZootifyDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Returns a filtered list of collections visible to the caller, applying authentication-based visibility rules, name filtering, and sorting.
    /// </summary>
    /// <param name="authentication">The optional authentication context used for visibility filtering.</param>
    /// <param name="filterByName">Optional name fragment to filter collections by.</param>
    /// <param name="paging">Paging and sorting parameters (used for ordering).</param>
    /// <param name="skip">The number of matching collections to skip.</param>
    /// <param name="take">The maximum number of collections to return.</param>
    /// <returns>A list of matching <see cref="CollectionEntity"/> instances.</returns>
    public async Task<List<CollectionEntity>> GetAllCollectionsByNameFilterAsync(
        IAuthentication? authentication,
        string? filterByName,
        BaseSortAndPagingDto paging,
        int skip,
        int take)
    {
        // Regular and anonymous callers only ever see active specimens that are public or their own; moderators
        // and admins see every active specimen. Inactive specimens are never counted for anyone.
        var userGuid = authentication?.UserGuid;
        var restrictVisibility = authentication == null || authentication.Role == UserRole.User;

        // Filtered include so the specimen-collection links the mapper counts already exclude
        // inactive (and, for restricted callers, non-visible) specimens.
        IQueryable<CollectionEntity> query = Context.GetTrackingSet<CollectionEntity>()
            .Include(c => c.CreatedByUser)
            .Include(c => c.Collection.Where(sc =>
                sc.Specimen.Status == SpecimenStatus.Active
                && (!restrictVisibility
                    || sc.Specimen.IsPublic
                    || sc.Specimen.CreatedByUserGuid == userGuid)));

        query = AddFilter(query, authentication, filterByName, paging);

        return await query.Skip(skip).Take(take).ToListAsync();
    }

    /// <summary>
    /// Returns the collection with the given GUID, including its specimen-collection links, or <c>null</c> if not found.
    /// </summary>
    /// <param name="guid">The GUID of the collection to retrieve.</param>
    public async Task<CollectionEntity?> GetCollectionByGuidAsync(Guid guid)
    {
        return await Context.GetTrackingSet<CollectionEntity>()
            .Include(c => c.Collection)
            .FirstOrDefaultAsync(c => c.Guid == guid);
    }

    /// <summary>
    /// Returns the collection with the given GUID, eagerly loading audit user navigation properties, or <c>null</c> if not found.
    /// </summary>
    /// <param name="guid">The GUID of the collection to retrieve.</param>
    public async Task<CollectionEntity?> GetCollectionWithAuditAsync(Guid guid)
    {
        return await Context.GetTrackingSet<CollectionEntity>()
            .Include(c => c.CreatedByUser)
            .Include(c => c.UpdatedByUser)
            .Include(c => c.DeletedByUser)
            .FirstOrDefaultAsync(c => c.Guid == guid);
    }

    /// <summary>
    /// Returns a batch of soft-deleted collections whose deletion date is older than the given cutoff.
    /// </summary>
    /// <param name="cutoff">The maximum allowed deletion date; collections deleted before this date are eligible for purge.</param>
    /// <param name="batchSize">The maximum number of collections to return.</param>
    public async Task<List<CollectionEntity>> GetDeletedCollectionsForPurgeAsync(DateTime cutoff, int batchSize)
    {
        return await Context.GetTrackingSet<CollectionEntity>()
            .Where(c => c.Status == CollectionStatus.Deleted
                && c.DeletionDate != null
                && c.DeletionDate < cutoff)
            .OrderBy(c => c.DeletionDate)
            .Take(batchSize)
            .ToListAsync();
    }

    /// <summary>
    /// Applies visibility, name, and sort filters to the given collection query.
    /// </summary>
    /// <param name="query">The base queryable to apply filters to.</param>
    /// <param name="authentication">The optional authentication context for visibility rules.</param>
    /// <param name="filterByName">Optional name fragment filter.</param>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <returns>The filtered and ordered queryable.</returns>
    private static IQueryable<CollectionEntity> AddFilter(
        IQueryable<CollectionEntity> query,
        IAuthentication? authentication,
        string? filterByName,
        BaseSortAndPagingDto paging)
    {
        query = ApplyVisibilityAndNameFilter(query, authentication, filterByName);

        return ApplyOrderBy(query, paging.SortBy, paging.SortDirection);
    }

    /// <summary>
    /// Applies authentication-based visibility rules and the optional name filter to the query
    /// (without ordering or paging), so it can be shared by the list and count queries.
    /// </summary>
    /// <param name="query">The base collection queryable to filter.</param>
    /// <param name="authentication">The optional authentication context used for visibility filtering.</param>
    /// <param name="filterByName">Optional name fragment to filter collections by.</param>
    /// <returns>The filtered queryable.</returns>
    private static IQueryable<CollectionEntity> ApplyVisibilityAndNameFilter(
        IQueryable<CollectionEntity> query,
        IAuthentication? authentication,
        string? filterByName)
    {
        if (authentication == null)
        {
            query = query.Where(c => c.IsPublic && c.Status == CollectionStatus.Active);
        }

        if (authentication != null && authentication.Role == UserRole.User)
        {
            query = query.Where(c => (c.IsPublic && c.Status == CollectionStatus.Active)
                                     || c.CreatedByUserGuid == authentication.UserGuid);
        }

        if (!string.IsNullOrWhiteSpace(filterByName))
        {
            query = query.Where(c => c.Name.Contains(filterByName));
        }

        return query;
    }

    /// <summary>
    /// Applies ordering to the collection query based on the given sort field and direction.
    /// </summary>
    /// <param name="query">The queryable to sort.</param>
    /// <param name="sortBy">The name of the field to sort by.</param>
    /// <param name="sortDirection">The sort direction.</param>
    /// <returns>The ordered queryable.</returns>
    private static IQueryable<CollectionEntity> ApplyOrderBy(
        IQueryable<CollectionEntity> query,
        string? sortBy,
        SortDirection sortDirection)
    {
        var desc = sortDirection == SortDirection.Desc;

        if (!string.IsNullOrWhiteSpace(sortBy) && sortBy == "name")
        {
            return desc ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name);
        }

        if (!string.IsNullOrWhiteSpace(sortBy) && sortBy == "isPublic")
        {
            return desc ? query.OrderByDescending(c => c.IsPublic) : query.OrderBy(c => c.IsPublic);
        }

        if (!string.IsNullOrWhiteSpace(sortBy) && sortBy == "createdAt")
        {
            return desc ? query.OrderByDescending(c => c.CreationDate) : query.OrderBy(c => c.CreationDate);
        }

        return query.OrderBy(c => c.Guid);
    }
}
