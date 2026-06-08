using System.Globalization;
using System.Resources;
using AutoMapper;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Services.Common.Interfaces;
using Zootify_Backend.Resources;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Common.Classes;

public class CommonCollectionService : ICommonCollectionService
{
    private static readonly ResourceManager CollectionNamesManager =
        new("Zootify_Backend.Resources.CollectionNames", typeof(CollectionNames).Assembly);

    private static readonly Guid AllSpecimensGuid   = new("00000000-0000-0000-0000-000000000001");
    private static readonly Guid MyCollectionGuid   = new("00000000-0000-0000-0000-000000000002");
    private static readonly Guid MyActiveGuid       = new("00000000-0000-0000-0000-000000000003");
    private static readonly Guid MyPendingGuid      = new("00000000-0000-0000-0000-000000000004");
    private static readonly Guid MyDeletedGuid      = new("00000000-0000-0000-0000-000000000005");

    private readonly ICollectionRepository _collectionRepository;
    private readonly ISpecimenCollectionRepository _specimenCollectionRepository;
    private readonly ISpecimenRepository _specimenRepository;
    private readonly IMapper _mapper;

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

    [Transaction(ReadOnly = true)]
    public async Task<List<GetCollectionResponseDto>> GetPublicCollectionsAsync(IAuthentication? authentication)
    {
        var collections = await _collectionRepository.GetPublicCollectionsAsync();
        var result = _mapper.Map<List<GetCollectionResponseDto>>(collections);

        result.InsertRange(0, await BuildStaticCollectionsAsync(authentication));

        return result;
    }

    private async Task<List<GetCollectionResponseDto>> BuildStaticCollectionsAsync(IAuthentication? authentication)
    {
        if (authentication == null)
            return [];

        var culture = CultureInfo.CurrentUICulture;

        var allPublicCount = await _specimenRepository.CountPublicAsync();
        var myCount        = await _specimenRepository.CountOwnAsync(authentication.UserGuid);
        var activeCount    = await _specimenRepository.CountByStatusAsync(SpecimenStatus.Active,  authentication);
        var pendingCount   = await _specimenRepository.CountByStatusAsync(SpecimenStatus.Pending, authentication);
        var deletedCount   = await _specimenRepository.CountByStatusAsync(SpecimenStatus.Deleted, authentication);

        return
        [
            StaticCollection(AllSpecimensGuid,  "AllSpecimens", culture, allPublicCount),
            StaticCollection(MyCollectionGuid,  "MyCollection", culture, myCount),
            StaticCollection(MyActiveGuid,      "MyActive",     culture, activeCount),
            StaticCollection(MyPendingGuid,     "MyPending",    culture, pendingCount),
            StaticCollection(MyDeletedGuid,     "MyDeleted",    culture, deletedCount)
        ];
    }

    private static GetCollectionResponseDto StaticCollection(
        Guid guid, string key, CultureInfo culture, int specimenCount) => new()
    {
        Guid          = guid,
        Name          = CollectionNamesManager.GetString(key, culture) ?? key,
        IsPrivate     = true,
        SpecimenCount = specimenCount
    };

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
            throw new ZootifyException(ErrorCode.NoSpecimenWithThisGuid);
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

        var newData = _mapper.Map<CollectionEntity>(request);

        if (!collection.CheckChanges(newData))
        {
            throw new ZootifyException(ErrorCode.NoUpdatedData);
        }

        collection.UpdateData(newData, authentication.UserGuid);
        await _collectionRepository.UpdateAsync(collection);
    }

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

        collection.SetDeletedBy(authentication.UserGuid);
        await _collectionRepository.UpdateAsync(collection);
    }
    
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

        collection.RemoveDeletion();
        collection.SetUpdatedBy(authentication.UserGuid);
        await _collectionRepository.UpdateAsync(collection);
    }

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

    private static void CheckCollectionOwnership(IAuthentication authentication, CollectionEntity collection)
    {
        if (authentication.Role != UserRole.Admin
            && authentication.Role != UserRole.Moderator
            && authentication.UserGuid != collection.CreatedByUserGuid)
        {
            throw new ZootifyException(ErrorCode.Forbidden);
        }
    }
}
