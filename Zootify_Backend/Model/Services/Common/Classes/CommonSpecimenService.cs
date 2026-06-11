using AutoMapper;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
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

    [Transaction(ReadOnly = true)]
    public async Task<GetSpecimenDetailResponseDto> GetSpecimenByGuidAsync(
        IAuthentication? authentication,
        Guid specimenGuid,
        Guid collectionGuid)
    {
        var specimen = await _specimenRepository.GetSpecimenWithDetailByGuidAsync(specimenGuid);

        if (specimen == null)
        {
            throw new ZootifyException(ErrorCode.NoSpecimenWithThisGuid);
        }

        CheckSpecimenVisibility(authentication, specimen);
        CheckSpecimenInCollection(authentication, specimen, collectionGuid);

        var result = _mapper.Map<GetSpecimenDetailResponseDto>(specimen);
        result.TaxonomyPath = await BuildTaxonomyPathAsync(specimen.Species);
        return result;
    }

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

    private async Task<List<GetAllTaxonomyNodesByFilterDto>> BuildTaxonomyPathAsync(TaxonomyNodeEntity species)
    {
        var nodes = new List<TaxonomyNodeEntity>();
        await _commonTaxonomyNodeService.GetTaxonomyTreeAsync(species, nodes);
        return _mapper.Map<List<GetAllTaxonomyNodesByFilterDto>>(nodes)
            .OrderBy(n => n.Rank)
            .ToList();
    }

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

    [Transaction(ReadOnly = true)]
    public async Task<List<GetAllTagsByFilterDto>> GetAllTagsAsync(
        string startsWith)
    {
        var tags = await _tagRepository
            .GetTagsByStartsWithNameAsync(startsWith);
        
        var mappedTags = _mapper.Map<List<GetAllTagsByFilterDto>>(tags);
        
        return mappedTags.OrderBy(t => t.TagName).ToList();
    }
    
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

        if (specimen.Image?.ImageBase64 == request.ImageBase64)
        {
            throw new ZootifyException(ErrorCode.NoUpdatedData);
        }
        
        await UpdateSpecimenImageAsync(
            request.ImageBase64,
            specimen,
            authentication.UserGuid);
    }

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

    [Transaction(ReadOnly = false)]
    public async Task UpdateSpecimenAsync(
        IAuthentication authentication,
        Guid specimenGuid,
        UpdateSpecimenRequestDto request)
    {
        var specimen = await CheckSpecimenWithOwnership(specimenGuid, authentication);
        
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

    private async Task<bool> CheckIfApprovalIsNeeded(
        TaxonomyNodeEntity taxonomyNode)
    {
        var taxonomyTreeList = new List<TaxonomyNodeEntity>();
        
        await _commonTaxonomyNodeService.GetTaxonomyTreeAsync(taxonomyNode, taxonomyTreeList);
        
        return taxonomyTreeList.Any(tn => tn.ApprovalStatus == ApprovalStatus.Pending);
    }

    private async Task<TaxonomyNodeEntity> CheckSpeciesAsync(Guid specimenGuid)
    {
        var species = await _taxonomyRepository
            .GetTaxonomyNodeByGuidAsync(specimenGuid);

        if (species == null)
        {
            throw new ZootifyException(ErrorCode.NoTaxonomyWithThisGuid);
        }

        if (species.Rank != TaxonomyRank.Species)
        {
            throw new ZootifyException(ErrorCode.TaxonomyRankIsNoSpecies);
        }
        
        return species;
    }

    private async Task<CreatedObjectResponseDto> PersistSpecimenAsync(
        SpecimenEntity specimen,
        List<TagEntity> tagsToCreate,
        List<TagEntity> existingTags,
        GeoDto geoDto,
        string imagesBase64)
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

    private async Task PersistSpecimenImagesAsync(
        SpecimenEntity specimen,
        string imagesBase64)
    {
        
        var imageEntity = new SpecimenImageEntity
        {
            ImageBase64 = imagesBase64,
            SpecimenGuid = specimen.Guid,
            UploadedAt = DateTime.UtcNow
        };
        
        await _specimenImageRepository.CreateAsync(imageEntity);
    }

    private async Task<List<TagEntity>> GetExistingTagsAsync(List<Guid> guids)
    {
        var tags = await _tagRepository.GetTagByGuidAsync(guids);

        if (tags.Count == 0 && guids.Count > 0)
        {
            throw new ZootifyException(ErrorCode.NoTagsFound);
        }

        return tags;
    }

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
            throw new ZootifyException(ErrorCode.NoSpecimenWithThisGuid);
        }
        
        if (authentication.Role == UserRole.User
            && authentication.UserGuid != specimen.CreatedByUserGuid)
        {
            throw new ZootifyException(ErrorCode.Forbidden);
        }
        
        return specimen;
    }
}