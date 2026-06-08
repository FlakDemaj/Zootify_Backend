using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

public interface ICommonCollectionService
{
    Task<CreatedObjectResponseDto> CreateCollectionAsync(
        IAuthentication authentication,
        CreateCollectionRequestDto request);

    Task<List<GetCollectionResponseDto>> GetPublicCollectionsAsync(IAuthentication? authentication);

    Task AddSpecimenToCollectionAsync(
        IAuthentication authentication,
        Guid collectionGuid,
        Guid specimenGuid);

    Task UpdateCollectionAsync(
        IAuthentication authentication,
        Guid collectionGuid,
        UpdateCollectionRequestDto request);

    Task DeleteCollectionAsync(
        IAuthentication authentication,
        Guid collectionGuid);

    Task ActivateCollectionAsync(
        IAuthentication authentication,
        Guid collectionGuid);

    Task RemoveSpecimenFromCollectionAsync(
        IAuthentication authentication,
        Guid collectionGuid,
        Guid specimenGuid);
}
