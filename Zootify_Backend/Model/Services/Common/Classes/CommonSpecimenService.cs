using AutoMapper;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
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
            request.Base64Images);
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

    [Transaction(ReadOnly = true)]
    public async Task<List<GetAllSpecimenDto>> GetAllSpecimensByStatusAsync(
        IAuthentication authentication,
        SpecimenStatus status)
    {
        var specimens = await
            _specimenRepository.GetAllSpecimensByStatusAsync(
                authentication,
                status);

        return _mapper.Map<List<GetAllSpecimenDto>>(specimens);
    }

    [Transaction(ReadOnly = true)]
    public async Task<List<GetAllSpecimenDto>> GetOwnSpecimensAsync(IAuthentication authentication)
    {
        var specimens = await
            _specimenRepository.GetOwnSpecimensAsync(
                authentication.UserGuid);

        return _mapper.Map<List<GetAllSpecimenDto>>(specimens);    }

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
        List<string> imagesBase64)
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
        List<string> imagesBase64)
    {
        
        var imagesToCreate = new List<SpecimenImageEntity>();
        
        foreach (var image in imagesBase64)
        {
            var imageEntity = new SpecimenImageEntity
            {
                ImageUrl = image,
                SpecimenGuid = specimen.Guid,
                UploadedAt = DateTime.UtcNow
            };

            imagesToCreate.Add(imageEntity);
        }
        
        await _specimenImageRepository.MassCreateAsync(imagesToCreate);
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
}