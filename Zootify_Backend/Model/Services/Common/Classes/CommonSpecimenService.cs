using AutoMapper;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Request.Helpers;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.DTOs.Common.Response.Helpers;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Providers.Nominatim;
using Zootify_Backend.Model.Services.Common.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Common.Classes;

/// <summary>
/// Handles business logic for creating, retrieving, and updating specimens along with their images and tags.
/// </summary>
public class CommonSpecimenService : ICommonSpecimenService
{
    private static readonly Guid AllSpecimensGuid = new("00000000-0000-0000-0000-000000000001");
    private static readonly Guid MyCollectionGuid = new("00000000-0000-0000-0000-000000000002");
    private static readonly Guid MyActiveGuid     = new("00000000-0000-0000-0000-000000000003");
    private static readonly Guid MyPendingGuid    = new("00000000-0000-0000-0000-000000000004");
    private static readonly Guid MyDeletedGuid    = new("00000000-0000-0000-0000-000000000005");
    private static readonly Guid MyRejectedGuid   = new("00000000-0000-0000-0000-000000000006");

    private readonly ISpecimenRepository _specimenRepository;
    
    private readonly ITagRepository _tagRepository;

    private readonly ITaxonomyRepository _taxonomyRepository;

    private readonly IRelationSpecimensTagRepository _specimenTagRepository;

    private readonly ISpecimenImageRepository _specimenImageRepository;

    private readonly INominatimProvider _nominatimProvider;

    private readonly IGeographicLocationRepository _geographicLocationRepository;
    
    private readonly ICommonTaxonomyNodeService _commonTaxonomyNodeService;

    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="CommonSpecimenService"/> with its required dependencies.
    /// </summary>
    /// <param name="specimenRepository">Repository for specimen persistence operations.</param>
    /// <param name="tagRepository">Repository for tag persistence operations.</param>
    /// <param name="mapper">AutoMapper instance for object mapping.</param>
    /// <param name="taxonomyRepository">Repository for taxonomy node queries.</param>
    /// <param name="specimenTagRepository">Repository for specimen-tag relation persistence operations.</param>
    /// <param name="specimenImageRepository">Repository for specimen image persistence operations.</param>
    /// <param name="nominatimProvider">Provider for reverse geographic location lookup.</param>
    /// <param name="geographicLocationRepository">Repository for geographic location persistence operations.</param>
    /// <param name="commonTaxonomyNodeService">Service used to build taxonomy ancestor chains.</param>
    public CommonSpecimenService(
        ISpecimenRepository specimenRepository,
        ITagRepository tagRepository,
        IMapper mapper,
        ITaxonomyRepository taxonomyRepository,
        IRelationSpecimensTagRepository specimenTagRepository,
        ISpecimenImageRepository specimenImageRepository,
        INominatimProvider nominatimProvider,
        IGeographicLocationRepository geographicLocationRepository, ICommonTaxonomyNodeService commonTaxonomyNodeService)
    {
        _specimenRepository = specimenRepository;
        _tagRepository = tagRepository;
        _mapper = mapper;
        _taxonomyRepository = taxonomyRepository;
        _specimenTagRepository = specimenTagRepository;
        _specimenImageRepository = specimenImageRepository;
        _nominatimProvider = nominatimProvider;
        _geographicLocationRepository = geographicLocationRepository;
        _commonTaxonomyNodeService = commonTaxonomyNodeService;
    }

    /// <summary>
    /// Returns the detailed view of a specimen within a given collection context, including its full taxonomy path.
    /// </summary>
    /// <param name="authentication">The optional authentication context used to enforce visibility rules.</param>
    /// <param name="specimenGuid">The GUID of the specimen to retrieve.</param>
    /// <param name="collectionGuid">The GUID of the collection context to validate membership against.</param>
    /// <returns>A detailed specimen response DTO including taxonomy path and audit information.</returns>
    /// <exception cref="ZootifyException">Thrown when no specimen with the given GUID exists.</exception>
    [Transaction(ReadOnly = true)]
    public async Task<GetSpecimenDetailResponseDto> GetSpecimenInCollectionByGuidAsync(
        IAuthentication? authentication,
        Guid specimenGuid,
        Guid collectionGuid)
    {
        var specimen = await _specimenRepository.GetSpecimenWithDetailByGuidAsync(specimenGuid);

        if (specimen == null)
        {
            throw new ZootifyException(ErrorCode.NoSpecimenFound);
        }

        CheckSpecimenVisibility(authentication, specimen);
        CheckSpecimenInCollection(authentication, specimen, collectionGuid);

        var result = _mapper.Map<GetSpecimenDetailResponseDto>(specimen);

        if (specimen.Species != null)
        {
            result.TaxonomyPath = await BuildTaxonomyPathAsync(specimen.Species);
        }
        else
        {
            result.TaxonomyPath = GetAllTaxonomyNodesByFilterDto.DeletedTaxonomys();
        }
        return result;
    }

    /// <summary>
    /// Throws a <see cref="ZootifyException"/> if the caller does not have permission to view the given specimen.
    /// </summary>
    /// <param name="authentication">The optional authentication context of the caller.</param>
    /// <param name="specimen">The specimen whose visibility to check.</param>
    /// <exception cref="ZootifyException">Thrown when the caller does not have permission.</exception>
    private static void CheckSpecimenVisibility(IAuthentication? authentication, SpecimenEntity specimen)
    {
        if (authentication == null && !specimen.IsPublic)
        {
            throw new ZootifyException(ErrorCode.Forbidden);
        }

        if (authentication != null
            && authentication.Role == UserRole.User
            && !specimen.IsPublic
            && specimen.CreatedByUserGuid != authentication.UserGuid)
        {
            throw new ZootifyException(ErrorCode.Forbidden);
        }
    }

    /// <summary>
    /// Throws a <see cref="ZootifyException"/> if the specimen does not belong to the specified collection context.
    /// </summary>
    /// <param name="authentication">The optional authentication context used for ownership checks on virtual collections.</param>
    /// <param name="specimen">The specimen to validate against the collection.</param>
    /// <param name="collectionGuid">The GUID of the collection context to check membership in.</param>
    /// <exception cref="ZootifyException">Thrown when the specimen is not in the collection.</exception>
    private static void CheckSpecimenInCollection(
        IAuthentication? authentication,
        SpecimenEntity specimen,
        Guid collectionGuid)
    {
        if (collectionGuid == AllSpecimensGuid)
        {
            return;
        }

        if (collectionGuid == MyCollectionGuid
            || collectionGuid == MyActiveGuid
            || collectionGuid == MyPendingGuid
            || collectionGuid == MyDeletedGuid
            || collectionGuid == MyRejectedGuid)
        {
            if (authentication == null || specimen.CreatedByUserGuid != authentication.UserGuid)
            {
                throw new ZootifyException(ErrorCode.SpecimenNotInCollection);
            }

            if (collectionGuid == MyActiveGuid && specimen.Status != SpecimenStatus.Active)
            {
                throw new ZootifyException(ErrorCode.SpecimenNotInCollection);
            }

            if (collectionGuid == MyPendingGuid && specimen.Status != SpecimenStatus.Pending)
            {
                throw new ZootifyException(ErrorCode.SpecimenNotInCollection);
            }

            if (collectionGuid == MyDeletedGuid && specimen.Status != SpecimenStatus.Deleted)
            {
                throw new ZootifyException(ErrorCode.SpecimenNotInCollection);
            }

            if (collectionGuid == MyRejectedGuid && specimen.Status != SpecimenStatus.Rejected)
            {
                throw new ZootifyException(ErrorCode.SpecimenNotInCollection);
            }

            return;
        }

        if (!specimen.Collections.Any(sc => sc.CollectionGuid == collectionGuid))
        {
            throw new ZootifyException(ErrorCode.SpecimenNotInCollection);
        }
    }

    /// <summary>
    /// Builds the ordered taxonomy ancestor chain from the given species node to the root.
    /// </summary>
    /// <param name="species">The species-rank taxonomy node to build the path from.</param>
    /// <returns>An ordered list of taxonomy node DTOs from the highest rank to the species level.</returns>
    private async Task<List<GetAllTaxonomyNodesByFilterDto>> BuildTaxonomyPathAsync(TaxonomyNodeEntity species)
    {
        var nodes = new List<TaxonomyNodeEntity>();
        await _commonTaxonomyNodeService.GetTaxonomyTreeAsync(species, nodes);
        return _mapper.Map<List<GetAllTaxonomyNodesByFilterDto>>(nodes)
            .OrderBy(n => n.Rank)
            .ToList();
    }

    /// <summary>
    /// Creates a new specimen, resolves its geographic location, and persists associated tags and image.
    /// </summary>
    /// <param name="authentication">The authentication context of the creating user.</param>
    /// <param name="request">The specimen creation data.</param>
    /// <returns>A DTO containing the GUID of the newly created specimen.</returns>
    [Transaction(ReadOnly = false)]
    public async Task<CreatedObjectResponseDto> CreateSpecimenAsync(
        IAuthentication authentication,
        CreateSpecimenRequestDto request)
    {
        var specimen = await BuildSpecimen(authentication.UserGuid, request);
        var tagsToCreate = _mapper.Map<List<TagEntity>>(request.TagsToCreate);
        var existingTags = await GetExistingTagsAsync(request.Tags);

        return await PersistSpecimenAsync(
            specimen,
            tagsToCreate,
            existingTags,
            request.Location,
            request.Base64Image);
    }

    /// <summary>
    /// Returns all tags whose names start with the given prefix, ordered alphabetically.
    /// </summary>
    /// <param name="startsWith">The prefix to filter tag names by.</param>
    /// <returns>A list of matching tag filter DTOs.</returns>
    [Transaction(ReadOnly = true)]
    public async Task<List<GetAllTagsByFilterDto>> GetAllTagsAsync(
        string startsWith)
    {
        var tags = await _tagRepository
            .GetTagsByStartsWithNameAsync(startsWith);
        
        var mappedTags = _mapper.Map<List<GetAllTagsByFilterDto>>(tags);
        
        return mappedTags.OrderBy(t => t.TagName).ToList();
    }
    
    /// <summary>
    /// Replaces the image attached to a specimen with the provided Base64-encoded image, verifying ownership.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="specimenGuid">The GUID of the specimen whose image to update.</param>
    /// <param name="request">The new Base64 image data.</param>
    /// <exception cref="ZootifyException">Thrown when the object is not in active status.</exception>
    /// <exception cref="ZootifyException">Thrown when no changes are detected.</exception>
    [Transaction(ReadOnly = false)]
    public async Task UpdateSpecimenImageAsync(
        IAuthentication authentication,
        Guid specimenGuid,
        UpdateSpecimenImageRequestDto request)
    {
        var specimen = await CheckSpecimenWithOwnership(
            specimenGuid,
            authentication,
            includeImage: true);

        if (specimen.Status != SpecimenStatus.Active)
        {
            throw new ZootifyException(ErrorCode.ObjectIsNotActive);
        }

        if (specimen.Image?.ImageBase64 == request.ImageBase64)
        {
            throw new ZootifyException(ErrorCode.NoUpdatedData);
        }
        
        await UpdateSpecimenImageAsync(
            request.ImageBase64,
            specimen,
            authentication.UserGuid);
    }

    /// <summary>
    /// Replaces the tag associations of a specimen with the provided set of tags, creating new tags as needed.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="specimenGuid">The GUID of the specimen whose tags to update.</param>
    /// <param name="request">The new set of tags, which may include both existing and new tags.</param>
    /// <exception cref="ZootifyException">Thrown when the object is not in active status.</exception>
    [Transaction(ReadOnly = false)]
    public async Task UpdateSpecimenTagsAsync(
        IAuthentication authentication,
        Guid specimenGuid,
        UpdateSpecimenTagsRequestDto request)
    {
        var specimen = await CheckSpecimenWithOwnership(
            specimenGuid,
            authentication,
            includeTags: true);

        if (specimen.Status != SpecimenStatus.Active)
        {
            throw new ZootifyException(ErrorCode.ObjectIsNotActive);
        }

        var existingTags = request
            .SpecimenTags
            .Where(t => t.TagGuid != null)
            .ToList();
        
        var mappedExistingTags = _mapper.Map<List<TagEntity>>(existingTags);
        
        var notExistingTags = request
            .SpecimenTags
            .Except(existingTags)
            .ToList();
        
        var mappedNotExistingTags = _mapper.Map<List<TagEntity>>(notExistingTags);
        
        await UpdateSpecimenTagsAsync(
            authentication,
            mappedExistingTags,
            mappedNotExistingTags,
            specimen);
    }

    /// <summary>
    /// Soft-deletes a specimen after verifying that the caller has ownership rights.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="specimenGuid">The GUID of the specimen to delete.</param>
    [Transaction(ReadOnly = false)]
    public async Task DeleteSpecimenAsync(IAuthentication authentication, Guid specimenGuid)
    {
        var specimen = await CheckSpecimenWithOwnership(specimenGuid, authentication);
        specimen.DeleteSpecimen(authentication.UserGuid);
        await _specimenRepository.UpdateAsync(specimen);
    }

    /// <summary>
    /// Restores a previously soft-deleted specimen to active status after verifying ownership.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="specimenGuid">The GUID of the specimen to activate.</param>
    [Transaction(ReadOnly = false)]
    public async Task ActivateSpecimenAsync(IAuthentication authentication, Guid specimenGuid)
    {
        var specimen = await CheckSpecimenWithOwnership(specimenGuid, authentication);
        specimen.ActivateSpecimen(authentication.UserGuid);
        await _specimenRepository.UpdateAsync(specimen);
    }

    /// <summary>
    /// Returns all specimens belonging to the given taxonomy node or any of its descendant species.
    /// </summary>
    /// <param name="authentication">The optional authentication context used to enforce visibility rules.</param>
    /// <param name="taxonomyGuid">The GUID of the taxonomy node to search under.</param>
    /// <returns>A list of specimen DTOs matching the taxonomy subtree.</returns>
    /// <exception cref="ZootifyException">Thrown when no taxonomy node with the given GUID exists.</exception>
    [Transaction(ReadOnly = true)]
    public async Task<List<SpecimenDto>> GetSpecimensByTaxonomyNodeGuidAsync(
        IAuthentication? authentication,
        Guid taxonomyGuid)
    {
        var taxonomyNode = await _taxonomyRepository.GetTaxonomyNodeByGuidAsync(taxonomyGuid);

        if (taxonomyNode == null)
        {
            throw new ZootifyException(ErrorCode.NoTaxonomyFound);
        }

        var allNodes = await _taxonomyRepository.GetAllNodesForTreeAsync();
        var byGuid   = allNodes.ToDictionary(n => n.Guid);
        var byParent = allNodes.ToLookup(n => n.ParentGuid);

        var speciesGuids = CollectDescendantSpeciesGuids(taxonomyGuid, byGuid, byParent);

        var specimens = await _specimenRepository.GetSpecimensBySpeciesGuidsAsync(speciesGuids, authentication);

        return _mapper.Map<List<SpecimenDto>>(specimens);
    }

    /// <summary>
    /// Collects the GUIDs of all species-rank nodes in the subtree rooted at the given node.
    /// </summary>
    /// <param name="rootGuid">The GUID of the root node to start the search from.</param>
    /// <param name="byGuid">Dictionary of all taxonomy nodes keyed by GUID.</param>
    /// <param name="byParent">Lookup of nodes keyed by their parent GUID.</param>
    /// <returns>A list of GUIDs for all species-rank descendant nodes.</returns>
    private static List<Guid> CollectDescendantSpeciesGuids(
        Guid rootGuid,
        Dictionary<Guid, TaxonomyNodeEntity> byGuid,
        ILookup<Guid?, TaxonomyNodeEntity> byParent)
    {
        var result = new List<Guid>();
        CollectSpeciesRecursive(rootGuid, byGuid, byParent, result);
        return result;
    }

    /// <summary>
    /// Recursively traverses the taxonomy subtree and adds species-rank node GUIDs to the accumulator list.
    /// </summary>
    /// <param name="nodeGuid">The GUID of the current node to process.</param>
    /// <param name="byGuid">Dictionary of all taxonomy nodes keyed by GUID.</param>
    /// <param name="byParent">Lookup of nodes keyed by their parent GUID.</param>
    /// <param name="speciesGuids">The accumulator list to which species GUIDs are appended.</param>
    private static void CollectSpeciesRecursive(
        Guid nodeGuid,
        Dictionary<Guid, TaxonomyNodeEntity> byGuid,
        ILookup<Guid?, TaxonomyNodeEntity> byParent,
        List<Guid> speciesGuids)
    {
        if (!byGuid.TryGetValue(nodeGuid, out var node))
        {
            return;
        }

        if (node.Rank == TaxonomyRank.Species)
        {
            speciesGuids.Add(nodeGuid);
            return;
        }

        foreach (var child in byParent[nodeGuid])
        {
            CollectSpeciesRecursive(child.Guid, byGuid, byParent, speciesGuids);
        }
    }

    /// <summary>
    /// Updates the core fields of an existing specimen after verifying ownership.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="specimenGuid">The GUID of the specimen to update.</param>
    /// <param name="request">The new data to apply to the specimen.</param>
    /// <exception cref="ZootifyException">Thrown when the object is not in active status.</exception>
    /// <exception cref="ZootifyException">Thrown when no changes are detected.</exception>
    [Transaction(ReadOnly = false)]
    public async Task UpdateSpecimenAsync(
        IAuthentication authentication,
        Guid specimenGuid,
        UpdateSpecimenRequestDto request)
    {
        var specimen = await CheckSpecimenWithOwnership(specimenGuid, authentication);

        if (specimen.Status != SpecimenStatus.Active)
        {
            throw new ZootifyException(ErrorCode.ObjectIsNotActive);
        }

        var mappedSpecimen = _mapper.Map<SpecimenEntity>(request);
        
        var specimenChanges = specimen.CheckChanges(mappedSpecimen);

        // If no data are updated, throw exception
        if (!specimenChanges)
        {
            throw new ZootifyException(ErrorCode.NoUpdatedData);
        }
    
        specimen.UpdateData(authentication.UserGuid, mappedSpecimen);
        
        await _specimenRepository.UpdateAsync(specimen);
    }

    /// <summary>
    /// Applies the full tag replacement on a specimen: removes obsolete tags, creates new ones, and links all resulting tags.
    /// </summary>
    /// <param name="authentication">The authentication context used to stamp the update audit fields.</param>
    /// <param name="existingTags">Tags that already exist in the database and should be linked to the specimen.</param>
    /// <param name="notExistingTags">New tags that must be created and then linked to the specimen.</param>
    /// <param name="specimen">The specimen entity whose tags are being replaced.</param>
    /// <exception cref="ZootifyException">Thrown when no changes are detected.</exception>
    private async Task UpdateSpecimenTagsAsync(
        IAuthentication authentication,
        List<TagEntity> existingTags,
        List<TagEntity> notExistingTags,
        SpecimenEntity specimen)
    {
        var specimenTagGuids = specimen.SpecimensTags
            .Select(t => t.TagGuid)
            .OrderBy(g => g)
            .ToList();

        var requestTagGuids = existingTags
            .Select(t => t.Guid)
            .OrderBy(g => g)
            .ToList();

        var tagsUnchanged = specimenTagGuids.SequenceEqual(requestTagGuids);

        if (tagsUnchanged && notExistingTags.Count == 0)
        {
            throw new ZootifyException(ErrorCode.NoUpdatedData);
        }

        // Tags entfernen, die nicht mehr im Request sind
        var tagsToRemove = specimen.SpecimensTags
            .Where(st => !requestTagGuids.Contains(st.TagGuid))
            .ToList();

        await _specimenTagRepository.MassDeleteAsync(tagsToRemove);

        // Neue Tags in DB anlegen
        var createdTags = new List<TagEntity>();

        foreach (var tag in notExistingTags)
        {
            var createdTag = await _tagRepository.CreateAsync(tag);
            createdTags.Add(createdTag);
        }

        // Tags verknüpfen, die dem Specimen noch nicht zugeordnet sind
        var tagsToLink = existingTags
            .Where(t => !specimenTagGuids.Contains(t.Guid))
            .Concat(createdTags)
            .ToList();

        await LinkTagsToSpecimenAsync(specimen, tagsToLink);
        specimen.SetUpdatedBy(authentication.UserGuid);

        await _specimenRepository.UpdateAsync(specimen);
    }

    /// <summary>
    /// Replaces or removes the persisted image on the given specimen entity.
    /// </summary>
    /// <param name="newImage">The new Base64 image string, or <c>null</c> to remove the existing image.</param>
    /// <param name="specimen">The specimen entity whose image to update.</param>
    /// <param name="requester">The GUID of the user performing the update, used for audit stamping.</param>
    private async Task UpdateSpecimenImageAsync(
        string? newImage,
        SpecimenEntity specimen,
        Guid requester)
    {
        if (newImage == null && specimen.Image != null)
        {
            await _specimenImageRepository.DeleteAsync(specimen.Image);
        }

        if (newImage != null && specimen.Image != null)
        {
            await _specimenImageRepository.DeleteAsync(specimen.Image);

            specimen.CreateImage(
                newImage,
                requester);
        }
        
        if (newImage!= null && specimen.Image == null)
        {
            specimen.CreateImage(
                newImage,
                requester);
        }

        await _specimenRepository.UpdateAsync(specimen);
    }

    /// <summary>
    /// Constructs and partially initializes a <see cref="SpecimenEntity"/> from the creation request,
    /// resolving the taxonomy species and determining the initial approval status.
    /// </summary>
    /// <param name="userGuid">The GUID of the user creating the specimen.</param>
    /// <param name="request">The specimen creation data.</param>
    /// <returns>A <see cref="SpecimenEntity"/> ready to be persisted.</returns>
    private async Task<SpecimenEntity> BuildSpecimen(
        Guid userGuid,
        CreateSpecimenRequestDto request)
    {
        var species = await CheckSpeciesAsync(request.TaxonomySpeciesGuid);
        var specimen = _mapper.Map<SpecimenEntity>(request);
        specimen.SetCreatedBy(userGuid);
        specimen.Status = await CheckIfApprovalIsNeeded(species) 
            ? SpecimenStatus.Pending 
            : SpecimenStatus.Active;

        specimen.SetLocation(
            request.Location.Latitude,
            request.Location.Longitude);
        
        return specimen;
    }

    /// <summary>
    /// Returns <c>true</c> if any node in the ancestor chain of the given taxonomy node has a pending approval status.
    /// </summary>
    /// <param name="taxonomyNode">The taxonomy node whose ancestor chain to check.</param>
    /// <returns><c>true</c> if approval is needed; otherwise <c>false</c>.</returns>
    private async Task<bool> CheckIfApprovalIsNeeded(
        TaxonomyNodeEntity taxonomyNode)
    {
        var taxonomyTreeList = new List<TaxonomyNodeEntity>();

        await _commonTaxonomyNodeService.GetTaxonomyTreeAsync(taxonomyNode, taxonomyTreeList);

        // A specimen is placed in Pending if *any* ancestor node is still pending,
        // not just the immediate species node.
        return taxonomyTreeList.Any(tn => tn.ApprovalStatus == ApprovalStatus.Pending);
    }

    /// <summary>
    /// Retrieves the taxonomy node for the given GUID and validates that it is a species-rank node.
    /// </summary>
    /// <param name="specimenGuid">The GUID of the taxonomy species node to validate.</param>
    /// <returns>The validated species-rank <see cref="TaxonomyNodeEntity"/>.</returns>
    /// <exception cref="ZootifyException">Thrown when no taxonomy node with the given GUID exists.</exception>
    /// <exception cref="ZootifyException">Thrown when the taxonomy node is not of species rank.</exception>
    private async Task<TaxonomyNodeEntity> CheckSpeciesAsync(Guid specimenGuid)
    {
        var species = await _taxonomyRepository
            .GetTaxonomyNodeByGuidAsync(specimenGuid);

        if (species == null)
        {
            throw new ZootifyException(ErrorCode.NoTaxonomyFound);
        }

        if (species.Rank != TaxonomyRank.Species)
        {
            throw new ZootifyException(ErrorCode.TaxonomyRankIsNoSpecies);
        }
        
        return species;
    }

    /// <summary>
    /// Persists the specimen entity to the database along with its geographic location, tags, and optional image.
    /// </summary>
    /// <param name="specimen">The specimen entity to persist.</param>
    /// <param name="tagsToCreate">New tags that must be created and linked to the specimen.</param>
    /// <param name="existingTags">Existing tags that should be linked to the specimen.</param>
    /// <param name="geoDto">The geographic coordinate data used to resolve or create the location chain.</param>
    /// <param name="imagesBase64">Optional Base64-encoded image to attach to the specimen.</param>
    /// <returns>A DTO containing the GUID of the newly created specimen.</returns>
    private async Task<CreatedObjectResponseDto> PersistSpecimenAsync(
        SpecimenEntity specimen,
        List<TagEntity> tagsToCreate,
        List<TagEntity> existingTags,
        GeoDto geoDto,
        string? imagesBase64)
    {
        var locationGuid = await ResolveGeographicLocationAsync(geoDto);
        
        specimen.GeographicLocationGuid = locationGuid;

        await _specimenRepository.CreateAsync(specimen);

        var allTags = await PersistAndCombineTagsAsync(tagsToCreate, existingTags);

        await LinkTagsToSpecimenAsync(specimen, allTags);
        await PersistSpecimenImagesAsync(specimen, imagesBase64);
        
        return new CreatedObjectResponseDto
        {
            CreatedObjectGuid = specimen.Guid
        };
    }

    /// <summary>
    /// Persists new tags to the database and combines them with the existing tags into a single list.
    /// </summary>
    /// <param name="tagsToCreate">New tags to create and add to the combined list.</param>
    /// <param name="existingTags">Tags that already exist in the database.</param>
    /// <returns>A combined list of all tag entities including both created and pre-existing ones.</returns>
    private async Task<List<TagEntity>> PersistAndCombineTagsAsync(
        List<TagEntity> tagsToCreate,
        List<TagEntity> existingTags)
    {
        var allTags = new List<TagEntity>(existingTags);

        foreach (var tag in tagsToCreate)
        {
            var createdTag = await _tagRepository.CreateAsync(tag);
            allTags.Add(createdTag);
        }

        return allTags;
    }

    /// <summary>
    /// Creates specimen-tag relation records for each tag in the given list and persists them in bulk.
    /// </summary>
    /// <param name="specimen">The specimen entity to link the tags to.</param>
    /// <param name="tags">The tags to associate with the specimen.</param>
    private async Task LinkTagsToSpecimenAsync(
        SpecimenEntity specimen,
        List<TagEntity> tags)
    {
        var relationsToCreate = new List<RelationSpecimensTagEntity>();
        
        foreach (var tag in tags)
        {
            var relation = new RelationSpecimensTagEntity
            {
                SpecimenGuid = specimen.Guid,
                TagGuid = tag.Guid
            };

            relationsToCreate.Add(relation);
        }
        
        await _specimenTagRepository.MassCreateAsync(relationsToCreate);
    }

    /// <summary>
    /// Creates and persists a specimen image entity if a Base64-encoded image is provided.
    /// </summary>
    /// <param name="specimen">The specimen entity to attach the image to.</param>
    /// <param name="imagesBase64">The Base64-encoded image string, or <c>null</c> to skip image creation.</param>
    private async Task PersistSpecimenImagesAsync(
        SpecimenEntity specimen,
        string? imagesBase64)
    {
        if (imagesBase64 == null)
        {
            return;
        }

        var imageEntity = new SpecimenImageEntity
        {
            ImageBase64 = imagesBase64,
            SpecimenGuid = specimen.Guid,
            UploadedAt = DateTime.UtcNow
        };
        
        await _specimenImageRepository.CreateAsync(imageEntity);
    }

    /// <summary>
    /// Fetches tag entities for the given list of GUIDs and throws if no tags are found when GUIDs were provided.
    /// </summary>
    /// <param name="guids">The list of tag GUIDs to look up.</param>
    /// <returns>The list of found <see cref="TagEntity"/> instances.</returns>
    /// <exception cref="ZootifyException">Thrown when no tags are found for the given GUIDs.</exception>
    private async Task<List<TagEntity>> GetExistingTagsAsync(List<Guid> guids)
    {
        var tags = await _tagRepository.GetTagByGuidAsync(guids);

        if (tags.Count == 0 && guids.Count > 0)
        {
            throw new ZootifyException(ErrorCode.NoTagsFound);
        }

        return tags;
    }

    /// <summary>
    /// Resolves geographic coordinates to a hierarchical location chain using the Nominatim provider,
    /// creating missing levels on demand, and returns the GUID of the most specific level found.
    /// </summary>
    /// <param name="geoData">The latitude and longitude to resolve.</param>
    /// <returns>The GUID of the most specific resolved <see cref="GeographicLocationEntity"/>.</returns>
    /// <exception cref="ZootifyException">Thrown when the geographic address cannot be resolved.</exception>
    private async Task<Guid> ResolveGeographicLocationAsync(GeoDto geoData)
    {
        var location = await _nominatimProvider.SearchLocationAsync(geoData);

        if (location == null || location.Address.CountryCode == null)
        {
            throw new ZootifyException(ErrorCode.AddressDoesNotExist);
        }

        var address = location.Address;

        // The country is always required and forms the root of the chain.
        var current = await GetOrCreateLocationAsync(
            address.CountryCode.ToUpper(),
            GeographicLevel.Country,
            null);

        // Nominatim does not always return every intermediate level.
        // Missing levels are skipped, so each present level is attached to
        // the closest present level above it (e.g. Road may hang directly
        // under State if City and Postcode are missing).
        current = await AppendLocationLevelAsync(address.State, GeographicLevel.State, current);
        current = await AppendLocationLevelAsync(address.City, GeographicLevel.City, current);
        current = await AppendLocationLevelAsync(address.Postcode, GeographicLevel.Postcode, current);
        current = await AppendLocationLevelAsync(address.Road, GeographicLevel.Street, current);
        current = await AppendLocationLevelAsync(address.HouseNumber, GeographicLevel.HouseNumber, current);

        return current.Guid;
    }

    /// <summary>
    /// Appends a geographic level to the chain if the name is present; returns the current parent unchanged if the level is missing.
    /// </summary>
    /// <param name="name">The name of the geographic level, or <c>null</c> if not provided by the API.</param>
    /// <param name="level">The geographic level type to append.</param>
    /// <param name="parent">The current leaf node in the geographic location chain.</param>
    /// <returns>The newly created or existing location entity for this level, or <paramref name="parent"/> if the level is absent.</returns>
    private async Task<GeographicLocationEntity> AppendLocationLevelAsync(
        string? name,
        GeographicLevel level,
        GeographicLocationEntity parent)
    {
        // Level not provided by the API: keep the current parent and move on.
        if (name == null)
        {
            return parent;
        }

        return await GetOrCreateLocationAsync(name, level, parent.Guid);
    }

    /// <summary>
    /// Returns an existing geographic location matching the given name and optional parent, or creates a new one if none exists.
    /// </summary>
    /// <param name="name">The name of the geographic location.</param>
    /// <param name="level">The geographic level of the location.</param>
    /// <param name="parentGuid">The GUID of the parent location, or <c>null</c> for root-level locations.</param>
    /// <returns>The existing or newly created <see cref="GeographicLocationEntity"/>.</returns>
    private async Task<GeographicLocationEntity> GetOrCreateLocationAsync(
        string name,
        GeographicLevel level,
        Guid? parentGuid)
    {
        GeographicLocationEntity? existing;

        if (parentGuid == null)
        {
            existing = await _geographicLocationRepository
                .GetGeographicLocationByLocationNameAsync(name);
        }
        else
        {
            existing = await _geographicLocationRepository
                .GetGeographicLocationByLocationNameAsync(name, parentGuid.Value);
        }

        if (existing != null)
        {
            return existing;
        }

        var newLocation = new GeographicLocationEntity
        {
            Name = name,
            Level = level,
            ParentGuid = parentGuid
        };

        return await _geographicLocationRepository.CreateAsync(newLocation);
    }

    /// <summary>
    /// Retrieves a specimen by GUID and verifies that the caller has ownership rights, optionally including image and tag data.
    /// </summary>
    /// <param name="specimenGuid">The GUID of the specimen to retrieve.</param>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="includeImage">Whether to eagerly load the specimen's image.</param>
    /// <param name="includeTags">Whether to eagerly load the specimen's tag associations.</param>
    /// <returns>The found and ownership-verified <see cref="SpecimenEntity"/>.</returns>
    /// <exception cref="ZootifyException">Thrown when no specimen with the given GUID exists.</exception>
    /// <exception cref="ZootifyException">Thrown when the caller does not have permission.</exception>
    private async Task<SpecimenEntity> CheckSpecimenWithOwnership(
        Guid specimenGuid,
        IAuthentication authentication,
        bool includeImage = false,
        bool includeTags = false)
    {
        var specimen = await _specimenRepository.GetSpecimenByGuidAsync(
            specimenGuid,
            includeImage: includeImage,
            includeTags: includeTags);

        if (specimen == null)
        {
            throw new ZootifyException(ErrorCode.NoSpecimenFound);
        }
        
        if (authentication.Role == UserRole.User
            && authentication.UserGuid != specimen.CreatedByUserGuid)
        {
            throw new ZootifyException(ErrorCode.Forbidden);
        }
        
        return specimen;
    }
}