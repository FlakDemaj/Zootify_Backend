using System.Globalization;
using System.Resources;
using AutoMapper;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.DTOs.Common.Response.Helpers;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Services.Common.Interfaces;
using Zootify_Backend.Resources;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Common.Classes;

/// <summary>
/// Handles business logic for managing specimen collections, including virtual static collections.
/// </summary>
public class CommonCollectionService : ICommonCollectionService
{
    private static readonly ResourceManager CollectionNamesManager =
        new("Zootify_Backend.Resources.CollectionNames", typeof(CollectionNames).Assembly);

    private static readonly Guid AllSpecimensGuid   = new("00000000-0000-0000-0000-000000000001");
    private static readonly Guid MyCollectionGuid   = new("00000000-0000-0000-0000-000000000002");
    private static readonly Guid MyActiveGuid       = new("00000000-0000-0000-0000-000000000003");
    private static readonly Guid MyPendingGuid      = new("00000000-0000-0000-0000-000000000004");
    private static readonly Guid MyDeletedGuid      = new("00000000-0000-0000-0000-000000000005");
    private static readonly Guid MyRejectedGuid    = new("00000000-0000-0000-0000-000000000006");
    
    private readonly ICollectionRepository _collectionRepository;
    private readonly ISpecimenCollectionRepository _specimenCollectionRepository;
    private readonly ISpecimenRepository _specimenRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="CommonCollectionService"/> with its required dependencies.
    /// </summary>
    /// <param name="collectionRepository">Repository for collection persistence operations.</param>
    /// <param name="specimenCollectionRepository">Repository for specimen-collection link persistence operations.</param>
    /// <param name="specimenRepository">Repository for specimen persistence and count operations.</param>
    /// <param name="mapper">AutoMapper instance for object mapping.</param>
    public CommonCollectionService(
        ICollectionRepository collectionRepository,
        ISpecimenCollectionRepository specimenCollectionRepository,
        ISpecimenRepository specimenRepository,
        IMapper mapper)
    {
        _collectionRepository = collectionRepository;
        _specimenCollectionRepository = specimenCollectionRepository;
        _specimenRepository = specimenRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Creates a new collection owned by the authenticated user.
    /// </summary>
    /// <param name="authentication">The authentication context of the creating user.</param>
    /// <param name="request">The collection creation data.</param>
    /// <returns>A DTO containing the GUID of the newly created collection.</returns>
    [Transaction(ReadOnly = false)]
    public async Task<CreatedObjectResponseDto> CreateCollectionAsync(
        IAuthentication authentication,
        CreateCollectionRequestDto request)
    {
        var collection = _mapper.Map<CollectionEntity>(request);
        collection.SetCreatedBy(authentication.UserGuid);
        await _collectionRepository.CreateAsync(collection);

        return new CreatedObjectResponseDto { CreatedObjectGuid = collection.Guid };
    }

    /// <summary>
    /// Returns a paged list of all collections visible to the caller, merged with virtual static collections.
    /// The "All Specimens" static collection is included for everyone; the user-specific static collections
    /// are only included for authenticated callers.
    /// </summary>
    /// <param name="authentication">The optional authentication context; anonymous callers see only public active collections plus "All Specimens".</param>
    /// <param name="filterByName">Optional name fragment to filter collections by.</param>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <returns>A paged response containing matching collections and the total count.</returns>
    [Transaction(ReadOnly = true)]
    public async Task<GetAllCollectionsPagedResponseDto> GetAllCollectionsAsync(
        IAuthentication? authentication,
        string? filterByName,
        BaseSortAndPagingDto paging)
    {
        var statics = await BuildStaticCollectionsAsync(authentication);

        // When a name filter is given, search is fuzzy-augmented (substring matches plus typo-tolerant
        // near-misses), which must run in memory over the full set of visible collections.
        if (!string.IsNullOrWhiteSpace(filterByName))
        {
            return await GetFuzzyFilteredCollectionsAsync(authentication, filterByName, paging, statics);
        }

        // specimenCount sort must compare static (computed) and dynamic counts in a single ordering,
        // which cannot be expressed in the DB query, so that path materializes the full set.
        if (paging.SortBy == "specimenCount")
        {
            return await GetAllCollectionsSortedBySpecimenCountAsync(
                authentication, null, paging, statics);
        }

        // Stable order: static collections first (fixed order), then DB-ordered dynamic collections.
        // Only the dynamic slice needed for the requested page is fetched from the database.
        var dynamicTotal = await _collectionRepository
            .CountCollectionsByNameFilterAsync(authentication, null);

        var skip = (paging.Page - 1) * paging.PageSize;
        var take = paging.PageSize;

        var staticSlice = statics.Skip(skip).Take(take).ToList();
        var remaining = take - staticSlice.Count;

        var dynamicSlice = new List<GetCollectionResponseDto>();
        if (remaining > 0)
        {
            var dynamicSkip = Math.Max(0, skip - statics.Count);
            var dynamicEntities = await _collectionRepository.GetAllCollectionsByNameFilterAsync(
                authentication, null, paging, dynamicSkip, remaining);
            dynamicSlice = _mapper.Map<List<GetCollectionResponseDto>>(dynamicEntities);
        }

        return new GetAllCollectionsPagedResponseDto
        {
            TotalCount  = statics.Count + dynamicTotal,
            Collections = staticSlice.Concat(dynamicSlice).ToList(),
        };
    }

    /// <summary>
    /// Returns the collections matching the given name fragment using fuzzy-augmented search: static and
    /// all visible dynamic collections are loaded into memory, ranked by relevance (substring matches
    /// first, then typo-tolerant near-misses), then paged. An explicit specimen-count sort overrides the
    /// relevance order.
    /// </summary>
    /// <param name="authentication">The optional authentication context used for visibility filtering.</param>
    /// <param name="filterByName">The name fragment to fuzzy-match against.</param>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <param name="statics">The caller's static (virtual) collections to include in the match.</param>
    /// <returns>A paged response containing the matching collections and the total match count.</returns>
    private async Task<GetAllCollectionsPagedResponseDto> GetFuzzyFilteredCollectionsAsync(
        IAuthentication? authentication,
        string filterByName,
        BaseSortAndPagingDto paging,
        List<GetCollectionResponseDto> statics)
    {
        var dynamic = _mapper.Map<List<GetCollectionResponseDto>>(
            await _collectionRepository.GetAllCollectionsByNameFilterAsync(
                authentication, null, paging, 0, int.MaxValue));

        var matched = FuzzyMatch.SearchAugmented(
            filterByName, statics.Concat(dynamic), c => c.Name);

        if (paging.SortBy == "specimenCount")
        {
            matched = (paging.SortDirection == SortDirection.Desc
                    ? matched.OrderByDescending(c => c.SpecimenCount)
                    : matched.OrderBy(c => c.SpecimenCount))
                .ToList();
        }

        var paged = matched
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToList();

        return new GetAllCollectionsPagedResponseDto
        {
            TotalCount  = matched.Count,
            Collections = paged,
        };
    }

    /// <summary>
    /// Returns the combined static and dynamic collections ordered by specimen count. Because static
    /// counts are computed outside the database, the full visible set is materialized, sorted, and
    /// paged in memory.
    /// </summary>
    /// <param name="authentication">The optional authentication context used for visibility filtering.</param>
    /// <param name="filterByName">Optional name fragment to filter collections by; <c>null</c> for no filter.</param>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <param name="statics">The caller's static (virtual) collections to include.</param>
    /// <returns>A paged response containing the ordered collections and the total count.</returns>
    private async Task<GetAllCollectionsPagedResponseDto> GetAllCollectionsSortedBySpecimenCountAsync(
        IAuthentication? authentication,
        string? filterByName,
        BaseSortAndPagingDto paging,
        List<GetCollectionResponseDto> statics)
    {
        var dynamic = _mapper.Map<List<GetCollectionResponseDto>>(
            await _collectionRepository.GetAllCollectionsByNameFilterAsync(
                authentication, filterByName, paging, 0, int.MaxValue));

        var combined = statics.Concat(dynamic).ToList();

        combined = paging.SortDirection == SortDirection.Desc
            ? combined.OrderByDescending(c => c.SpecimenCount).ToList()
            : combined.OrderBy(c => c.SpecimenCount).ToList();

        var paged = combined
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToList();

        return new GetAllCollectionsPagedResponseDto
        {
            TotalCount  = combined.Count,
            Collections = paged,
        };
    }

    /// <summary>
    /// Returns a paged list of specimens belonging to the specified collection, dispatching to static or dynamic collection logic.
    /// </summary>
    /// <param name="authentication">The optional authentication context used to enforce visibility rules.</param>
    /// <param name="collectionGuid">The GUID of the collection to retrieve specimens from.</param>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <returns>A response containing the collection metadata and its paged specimens.</returns>
    /// <exception cref="ZootifyException">Thrown when the collection is not found.</exception>
    [Transaction(ReadOnly = true)]
    public async Task<GetAllSpecimenInCollectionResponseDto> GetAllSpecimensInCollectionAsync(
        IAuthentication? authentication,
        Guid collectionGuid,
        string? filterByName,
        BaseSortAndPagingDto paging)
    {
        if (IsStaticCollection(collectionGuid))
        {
            return await GetSpecimensFromStaticCollectionAsync(authentication, filterByName, collectionGuid, paging);
        }

        var collection = await _collectionRepository.GetCollectionWithAuditAsync(collectionGuid);

        if (collection == null)
        {
            throw new ZootifyException(ErrorCode.CollectionNotFound);
        }
        
        CheckCollectionVisibility(authentication, collection);

        var (items, total) = await 
            _specimenRepository
                .GetSpecimensInCollectionPagedAsync(collectionGuid, filterByName, authentication, paging);

        return new GetAllSpecimenInCollectionResponseDto
        {
            CollectionGuid  = collection.Guid,
            CollectionName  = collection.Name,
            CreatedByName   = FormatUserName(collection.CreatedByUser),
            CreatedAt       = collection.CreationDate,
            UpdatedByName   = FormatUserName(collection.UpdatedByUser),
            UpdatedAt       = collection.UpdatedDate,
            DeletedByName   = FormatUserName(collection.DeletedByUser),
            DeletedAt       = collection.DeletionDate,
            Status          = collection.Status,
            TotalCount      = total,
            Specimens       = _mapper.Map<List<SpecimenDto>>(items),
        };
    }

    /// <summary>
    /// Returns a paged list of specimens from a virtual static collection such as "All Specimens" or "My Active".
    /// "All Specimens" is accessible to everyone (only public specimens are returned); the user-specific
    /// collections require authentication.
    /// </summary>
    /// <param name="authentication">The optional authentication context; required for the user-specific collections.</param>
    /// <param name="filterByName">Optional name fragment to filter the specimens by.</param>
    /// <param name="collectionGuid">The well-known GUID identifying the static collection.</param>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <returns>A response containing the virtual collection metadata and its paged specimens.</returns>
    /// <exception cref="ZootifyException">Thrown when an anonymous caller requests a user-specific static collection.</exception>
    private async Task<GetAllSpecimenInCollectionResponseDto> GetSpecimensFromStaticCollectionAsync(
        IAuthentication? authentication,
        string? filterByName,
        Guid collectionGuid,
        BaseSortAndPagingDto paging)
    {
        var culture = CultureInfo.CurrentUICulture;
        var key = StaticCollectionKey(collectionGuid);
        var name = CollectionNamesManager.GetString(key, culture) ?? key;

        Guid? userGuid;
        SpecimenStatus? status;

        if (collectionGuid == AllSpecimensGuid)
        {
            // Public to everyone; a null user GUID makes the repository return only public specimens.
            (userGuid, status) = (null, null);
        }
        else
        {
            // The remaining static collections are tied to the current user and require authentication.
            if (authentication == null)
            {
                throw new ZootifyException(ErrorCode.Forbidden);
            }

            (userGuid, status) = collectionGuid switch
            {
                _ when collectionGuid == MyCollectionGuid  => (authentication.UserGuid, (SpecimenStatus?)null),
                _ when collectionGuid == MyActiveGuid      => (authentication.UserGuid, SpecimenStatus.Active),
                _ when collectionGuid == MyPendingGuid     => (authentication.UserGuid, SpecimenStatus.Pending),
                _ when collectionGuid == MyDeletedGuid     => (authentication.UserGuid, SpecimenStatus.Deleted),
                _                                          => (authentication.UserGuid, SpecimenStatus.Rejected),
            };
        }

        var (items, total) = await 
            _specimenRepository
                .GetSpecimensPagedAsync(
                    userGuid,
                    filterByName,
                    status,
                    paging);

        return new GetAllSpecimenInCollectionResponseDto
        {
            CollectionGuid = collectionGuid,
            CollectionName = name,
            Status         = CollectionStatus.Active,
            TotalCount     = total,
            Specimens      = _mapper.Map<List<SpecimenDto>>(items),
        };
    }

    /// <summary>
    /// Returns <c>true</c> if the given GUID corresponds to one of the well-known virtual static collections.
    /// </summary>
    /// <param name="guid">The collection GUID to check.</param>
    private static bool IsStaticCollection(Guid guid) =>
        guid == AllSpecimensGuid  || guid == MyCollectionGuid ||
        guid == MyActiveGuid      || guid == MyPendingGuid    ||
        guid == MyDeletedGuid     || guid == MyRejectedGuid;

    /// <summary>
    /// Returns the resource key string for the given static collection GUID, used to look up its localized name.
    /// </summary>
    /// <param name="guid">The well-known static collection GUID.</param>
    /// <returns>The resource key string for the static collection.</returns>
    private static string StaticCollectionKey(Guid guid)
    {
        if (guid == AllSpecimensGuid)
        {
            return "AllSpecimens";
        }

        if (guid == MyCollectionGuid)
        {
            return "MyCollection";
        }

        if (guid == MyActiveGuid)
        {
            return "MyActive";
        }

        if (guid == MyPendingGuid)
        {
            return "MyPending";
        }

        if (guid == MyDeletedGuid)
        {
            return "MyDeleted";
        }

        return "MyRejected";
    }

    /// <summary>
    /// Throws a <see cref="ZootifyException"/> if the caller does not have visibility rights for the given collection.
    /// </summary>
    /// <param name="authentication">The optional authentication context of the caller.</param>
    /// <param name="collection">The collection whose visibility to check.</param>
    /// <exception cref="ZootifyException">Thrown when the caller does not have permission.</exception>
    private static void CheckCollectionVisibility(IAuthentication? authentication, CollectionEntity collection)
    {
        var isPublicActive = !collection.IsPrivate && collection.Status == CollectionStatus.Active;
        if (isPublicActive)
        {
            return;
        }

        if (authentication == null ||
            authentication.Role == UserRole.User
            && authentication.UserGuid != collection.CreatedByUserGuid)
        {
            throw new ZootifyException(ErrorCode.Forbidden);
        }
    }

    /// <summary>
    /// Formats a user's full name as a trimmed string, or returns <c>null</c> if the user is not set.
    /// </summary>
    /// <param name="user">The user entity to format, or <c>null</c>.</param>
    /// <returns>The formatted full name, or <c>null</c> if <paramref name="user"/> is <c>null</c>.</returns>
    private static string? FormatUserName(UserEntity? user) =>
        user == null ? null : $"{user.FirstName} {user.LastName}".Trim();

    /// <summary>
    /// Builds the list of virtual static collection DTOs with up-to-date specimen counts. The "All Specimens"
    /// collection is included for everyone (it only ever exposes public specimens); the remaining,
    /// user-specific collections are only included for authenticated callers.
    /// </summary>
    /// <param name="authentication">The optional authentication context.</param>
    /// <returns>A list of virtual static collection response DTOs.</returns>
    private async Task<List<GetCollectionResponseDto>> BuildStaticCollectionsAsync(
        IAuthentication? authentication)
    {
        var culture = CultureInfo.CurrentUICulture;

        var allPublicCount = await _specimenRepository.CountPublicAsync();

        // "All Specimens" is visible to everyone, logged in or not.
        var statics = new List<GetCollectionResponseDto>
        {
            StaticCollection(AllSpecimensGuid, "AllSpecimens", culture, allPublicCount),
        };

        // The remaining static collections are tied to the current user and require authentication.
        if (authentication == null)
        {
            return statics;
        }

        var myCount       = await _specimenRepository.CountOwnAsync(authentication.UserGuid);
        var activeCount   = await _specimenRepository.CountByStatusAsync(SpecimenStatus.Active,  authentication);
        var pendingCount  = await _specimenRepository.CountByStatusAsync(SpecimenStatus.Pending, authentication);
        var deletedCount  = await _specimenRepository.CountByStatusAsync(SpecimenStatus.Deleted, authentication);
        var rejectedCount = await _specimenRepository.CountByStatusAsync(SpecimenStatus.Rejected, authentication);

        statics.AddRange(
        [
            StaticCollection(MyCollectionGuid,  "MyCollection", culture, myCount),
            StaticCollection(MyActiveGuid,      "MyActive",     culture, activeCount),
            StaticCollection(MyPendingGuid,     "MyPending",    culture, pendingCount),
            StaticCollection(MyDeletedGuid,     "MyDeleted",    culture, deletedCount),
            StaticCollection(MyRejectedGuid,    "MyRejected",   culture, rejectedCount),
        ]);

        return statics;
    }

    /// <summary>
    /// Creates a virtual static <see cref="GetCollectionResponseDto"/> with a localized name and specimen count.
    /// </summary>
    /// <param name="guid">The well-known GUID of the static collection.</param>
    /// <param name="key">The resource key used to look up the localized collection name.</param>
    /// <param name="culture">The current UI culture for localizing the collection name.</param>
    /// <param name="specimenCount">The number of specimens to display for this virtual collection.</param>
    /// <returns>A populated static collection response DTO.</returns>
    private static GetCollectionResponseDto StaticCollection(
        Guid guid, string key, CultureInfo culture, int specimenCount) => new()
    {
        Guid          = guid,
        Name          = CollectionNamesManager.GetString(key, culture) ?? key,
        IsPrivate     = true,
        SpecimenCount = specimenCount,
        CreatedAt = null
    };

    /// <summary>
    /// Adds a specimen to the specified collection, verifying ownership and that the specimen is not already present.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="collectionGuid">The GUID of the collection to add the specimen to.</param>
    /// <param name="specimenGuid">The GUID of the specimen to add.</param>
    /// <exception cref="ZootifyException">Thrown when the collection is not found.</exception>
    /// <exception cref="ZootifyException">Thrown when no specimen with the given GUID exists.</exception>
    /// <exception cref="ZootifyException">Thrown when the specimen is already in the collection.</exception>
    [Transaction(ReadOnly = false)]
    public async Task AddSpecimenToCollectionAsync(
        IAuthentication authentication,
        Guid collectionGuid,
        Guid specimenGuid)
    {
        var collection = await _collectionRepository.GetCollectionByGuidAsync(collectionGuid);

        if (collection == null)
        {
            throw new ZootifyException(ErrorCode.CollectionNotFound);
        }
        
        CheckCollectionOwnership(authentication, collection);

        var specimen = await _specimenRepository.GetSpecimenByGuidAsync(specimenGuid);

        if (specimen == null)
        {
            throw new ZootifyException(ErrorCode.NoSpecimenFound);
        }

        var existing = await _specimenCollectionRepository
            .GetByCollectionAndSpecimenAsync(collection.Guid, specimen.Guid);

        if (existing != null)
        {
            throw new ZootifyException(ErrorCode.SpecimenAlreadyInCollection);
        }

        var specimenCollection = new SpecimenCollectionEntity
        {
            CollectionGuid = collection.Guid,
            SpecimenGuid = specimen.Guid
        };

        specimen.Collections.Add(specimenCollection);
    }

    /// <summary>
    /// Updates the name or visibility of an existing collection after verifying ownership.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="collectionGuid">The GUID of the collection to update.</param>
    /// <param name="request">The new data to apply to the collection.</param>
    /// <exception cref="ZootifyException">Thrown when the collection is not found.</exception>
    /// <exception cref="ZootifyException">Thrown when the object is not in active status.</exception>
    /// <exception cref="ZootifyException">Thrown when no changes are detected.</exception>
    [Transaction(ReadOnly = false)]
    public async Task UpdateCollectionAsync(
        IAuthentication authentication,
        Guid collectionGuid,
        UpdateCollectionRequestDto request)
    {
        var collection = await _collectionRepository.GetCollectionByGuidAsync(collectionGuid);

        if (collection == null)
        {
            throw new ZootifyException(ErrorCode.CollectionNotFound);
        }

        CheckCollectionOwnership(authentication, collection);

        if (collection.Status != CollectionStatus.Active)
        {
            throw new ZootifyException(ErrorCode.ObjectIsNotActive);
        }

        var newData = _mapper.Map<CollectionEntity>(request);

        if (!collection.CheckChanges(newData))
        {
            throw new ZootifyException(ErrorCode.NoUpdatedData);
        }

        collection.UpdateData(newData, authentication.UserGuid);
        await _collectionRepository.UpdateAsync(collection);
    }

    /// <summary>
    /// Soft-deletes a collection after verifying that the caller is the owner.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="collectionGuid">The GUID of the collection to delete.</param>
    /// <exception cref="ZootifyException">Thrown when the collection is not found.</exception>
    [Transaction(ReadOnly = false)]
    public async Task DeleteCollectionAsync(
        IAuthentication authentication,
        Guid collectionGuid)
    {
        var collection = await _collectionRepository.GetCollectionByGuidAsync(collectionGuid);

        if (collection == null)
        {
            throw new ZootifyException(ErrorCode.CollectionNotFound);
        }

        CheckCollectionOwnership(authentication, collection);

        collection.DeleteCollection(authentication.UserGuid);
        await _collectionRepository.UpdateAsync(collection);
    }
    
    /// <summary>
    /// Restores a previously soft-deleted collection to active status after verifying ownership.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="collectionGuid">The GUID of the collection to activate.</param>
    /// <exception cref="ZootifyException">Thrown when the collection is not found.</exception>
    [Transaction(ReadOnly = false)]
    public async Task ActivateCollectionAsync(
        IAuthentication authentication,
        Guid collectionGuid)
    {
        var collection = await _collectionRepository.GetCollectionByGuidAsync(collectionGuid);

        if (collection == null)
        {
            throw new ZootifyException(ErrorCode.CollectionNotFound);
        }

        CheckCollectionOwnership(authentication, collection);

        collection.ActivateCollection(authentication.UserGuid);
        await _collectionRepository.UpdateAsync(collection);
    }

    /// <summary>
    /// Removes a specimen from the specified collection after verifying ownership of the collection.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="collectionGuid">The GUID of the collection to remove the specimen from.</param>
    /// <param name="specimenGuid">The GUID of the specimen to remove.</param>
    /// <exception cref="ZootifyException">Thrown when the collection is not found.</exception>
    /// <exception cref="ZootifyException">Thrown when the specimen is not in the collection.</exception>
    [Transaction(ReadOnly = false)]
    public async Task RemoveSpecimenFromCollectionAsync(
        IAuthentication authentication,
        Guid collectionGuid,
        Guid specimenGuid)
    {
        var collection = await _collectionRepository.GetCollectionByGuidAsync(collectionGuid);

        if (collection == null)
        {
            throw new ZootifyException(ErrorCode.CollectionNotFound);
        }
        
        CheckCollectionOwnership(authentication, collection);
        
        var link = await _specimenCollectionRepository
            .GetByCollectionAndSpecimenAsync(collectionGuid, specimenGuid);

        if (link == null)
        {
            throw new ZootifyException(ErrorCode.SpecimenNotInCollection);
        }

        await _specimenCollectionRepository.DeleteAsync(link);
    }

    /// <summary>
    /// Throws a <see cref="ZootifyException"/> if a regular user attempts to modify a collection they do not own.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="collection">The collection whose ownership to verify.</param>
    /// <exception cref="ZootifyException">Thrown when the caller does not have permission.</exception>
    private static void CheckCollectionOwnership(IAuthentication authentication, CollectionEntity collection)
    {
        if (authentication.Role == UserRole.User
            && authentication.UserGuid != collection.CreatedByUserGuid)
        {
            throw new ZootifyException(ErrorCode.Forbidden);
        }
    }
}
