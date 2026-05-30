using AutoMapper;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Providers.Nominatim;
using Zootify_Backend.Model.Services.Common.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Common.Classes;

public class CommonSpecimenService : ICommonSpecimenService
{
    private readonly ISpecimenRepository _specimenRepository;

    private readonly IApprovalSpecimenRepository _approvalSpecimenRepository;

    private readonly ITagRepository _tagRepository;

    private readonly ITaxonomyRepository _taxonomyRepository;

    private readonly IRelationSpecimensTagRepository _specimenTagRepository;

    private readonly ISpecimenImageRepository _specimenImageRepository;

    private readonly INominatimProvider _nominatimProvider;

    private readonly IGeographicLocationRepository _geographicLocationRepository;

    private readonly IMapper _mapper;

    public CommonSpecimenService(
        ISpecimenRepository specimenRepository,
        ITagRepository tagRepository,
        IApprovalSpecimenRepository approvalSpecimenRepository,
        IMapper mapper,
        ITaxonomyRepository taxonomyRepository,
        IRelationSpecimensTagRepository specimenTagRepository,
        ISpecimenImageRepository specimenImageRepository,
        INominatimProvider nominatimProvider,
        IGeographicLocationRepository geographicLocationRepository)
    {
        _specimenRepository = specimenRepository;
        _tagRepository = tagRepository;
        _approvalSpecimenRepository = approvalSpecimenRepository;
        _mapper = mapper;
        _taxonomyRepository = taxonomyRepository;
        _specimenTagRepository = specimenTagRepository;
        _specimenImageRepository = specimenImageRepository;
        _nominatimProvider = nominatimProvider;
        _geographicLocationRepository = geographicLocationRepository;
    }

    [Transaction(ReadOnly = false)]
    public async Task<CreatedObjectResponseDto> CreateSpecimenAsync(
        CreateSpecimenRequestDto request)
    {
        var needApproval = request.Taxonomies.Any(taxonomy => taxonomy.Guid == null);

        var specimen = BuildSpecimen(request, needApproval);
        var tagsToCreate = _mapper.Map<List<TagEntity>>(request.TagsToCreate);
        var existingTags = await GetExistingTagsAsync(request.Tags);
        var taxonomyTree = await BuildTaxonomyTreeAsync(request.Taxonomies);

        return await PersistSpecimenAsync(
            specimen,
            tagsToCreate,
            existingTags,
            taxonomyTree,
            request.Location,
            request.Base64Images,
            needApproval);
    }

    private SpecimenEntity BuildSpecimen(
        CreateSpecimenRequestDto request,
        bool needApproval)
    {
        var specimen = _mapper.Map<SpecimenEntity>(request);

        specimen.SetLocation(
            request.Location.Latitude,
            request.Location.Longitude);

        if (needApproval)
        {
            specimen.Status = SpecimenStatus.Pending;
        }
        else
        {
            specimen.Status = SpecimenStatus.Active;
        }

        return specimen;
    }

    private async Task<CreatedObjectResponseDto> PersistSpecimenAsync(
        SpecimenEntity specimen,
        List<TagEntity> tagsToCreate,
        List<TagEntity> existingTags,
        List<TaxonomyNodeEntity> taxonomyTree,
        GeoDto geoDto,
        List<string> imagesBase64,
        bool needApproval)
    {
        var locationGuid = await ResolveGeographicLocationAsync(geoDto);

        await PersistNewTaxonomyNodesAsync(taxonomyTree, needApproval);

        specimen.SetSpecies(taxonomyTree);
        specimen.GeographicLocationGuid = locationGuid;

        await _specimenRepository.CreateAsync(specimen);

        var allTags = await PersistAndCombineTagsAsync(tagsToCreate, existingTags);

        await LinkTagsToSpecimenAsync(specimen, allTags);
        await PersistSpecimenImagesAsync(specimen, imagesBase64);

        if (needApproval)
        {
            await CreatePendingApprovalAsync(specimen);
        }

        return new CreatedObjectResponseDto
        {
            CreatedObjectGuid = specimen.Guid
        };
    }

    private async Task PersistNewTaxonomyNodesAsync(
        List<TaxonomyNodeEntity> taxonomyTree,
        bool needApproval)
    {
        var newNodes = taxonomyTree
            .Where(node => node.ApprovalStatus == ApprovalStatus.Pending);

        foreach (var node in newNodes)
        {
            if (needApproval)
            {
                node.ApprovalStatus = ApprovalStatus.Pending;
            }
            else
            {
                node.ApprovalStatus = ApprovalStatus.Approved;
            }

            await _taxonomyRepository.CreateAsync(node);
        }
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
        foreach (var tag in tags)
        {
            var relation = new RelationSpecimensTagEntity
            {
                SpecimenGuid = specimen.Guid,
                TagGuid = tag.Guid
            };

            await _specimenTagRepository.CreateAsync(relation);
        }
    }

    private async Task PersistSpecimenImagesAsync(
        SpecimenEntity specimen,
        List<string> imagesBase64)
    {
        foreach (var image in imagesBase64)
        {
            var imageEntity = new SpecimenImageEntity
            {
                ImageUrl = image,
                SpecimenGuid = specimen.Guid,
                UploadedAt = DateTime.UtcNow
            };

            await _specimenImageRepository.CreateAsync(imageEntity);
        }
    }

    private async Task CreatePendingApprovalAsync(SpecimenEntity specimen)
    {
        var approval = new ApprovalSpecimenEntity
        {
            ApprovalStatus = ApprovalStatus.Pending,
            SpecimenGuid = specimen.Guid,
            Comment = string.Empty
        };

        await _approvalSpecimenRepository.CreateAsync(approval);
    }

    private async Task<List<TaxonomyNodeEntity>> BuildTaxonomyTreeAsync(
        List<TaxonomyDto> taxonomies)
    {
        var sortedTaxonomies = taxonomies
            .OrderBy(taxonomy => taxonomy.Rank)
            .ToList();

        var taxonomyTree = new List<TaxonomyNodeEntity>();

        for (var i = 0; i < sortedTaxonomies.Count; i++)
        {
            var taxonomy = sortedTaxonomies[i];

            if (taxonomy.Guid != null)
            {
                var existingNode = await LoadExistingTaxonomyNodeAsync(taxonomy.Guid.Value);
                taxonomyTree.Add(existingNode);
            }
            else
            {
                var parentGuid = ResolveParentGuid(taxonomyTree, i);
                var newNode = BuildNewTaxonomyNode(taxonomy, parentGuid);
                taxonomyTree.Add(newNode);
            }
        }

        return taxonomyTree;
    }

    private async Task<TaxonomyNodeEntity> LoadExistingTaxonomyNodeAsync(Guid guid)
    {
        var node = await _taxonomyRepository.GetTaxonomyNodeByGuidAsync(guid);

        if (node == null)
        {
            throw new ZootifyException(ErrorCode.NoTaxonomyWithThisGuid);
        }

        return node;
    }

    private static Guid? ResolveParentGuid(
        List<TaxonomyNodeEntity> taxonomyTree,
        int currentIndex)
    {
        if (currentIndex == 0)
        {
            return null;
        }

        return taxonomyTree[currentIndex - 1].Guid;
    }

    private static TaxonomyNodeEntity BuildNewTaxonomyNode(
        TaxonomyDto taxonomy,
        Guid? parentGuid)
    {
        return new TaxonomyNodeEntity
        {
            Name = taxonomy.Name,
            ApprovalStatus = ApprovalStatus.Pending,
            Rank = taxonomy.Rank,
            ParentGuid = parentGuid
        };
    }

    private async Task<List<TagEntity>> GetExistingTagsAsync(List<Guid> guids)
    {
        var tags = new List<TagEntity>();

        foreach (var guid in guids)
        {
            var tag = await _tagRepository.GetTagByGuidAsync(guid);

            if (tag == null)
            {
                throw new ZootifyException(ErrorCode.NoTagWithThisGuid);
            }

            tags.Add(tag);
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