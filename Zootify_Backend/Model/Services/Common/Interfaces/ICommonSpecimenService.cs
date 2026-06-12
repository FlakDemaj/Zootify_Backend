using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Request.Helpers;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.DTOs.Common.Response.Helpers;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

public interface ICommonSpecimenService
{
    Task<CreatedObjectResponseDto> CreateSpecimenAsync(
        IAuthentication authentication,
        CreateSpecimenRequestDto request);

    Task<GetSpecimenDetailResponseDto> GetSpecimenInCollectionByGuidAsync(
        IAuthentication? authentication,
        Guid specimenGuid,
        Guid collectionGuid);

    Task<List<GetAllTagsByFilterDto>> GetAllTagsAsync(string startsWith);
    
    Task UpdateSpecimenAsync(
        IAuthentication authentication,
        Guid specimenGuid,
        UpdateSpecimenRequestDto request);

    Task UpdateSpecimenImageAsync(
        IAuthentication authentication,
        Guid specimenGuid,
        UpdateSpecimenImageRequestDto request);

    Task UpdateSpecimenTagsAsync(
        IAuthentication authentication,
        Guid specimenGuid,
        UpdateSpecimenTagsRequestDto request
    );

    Task<List<SpecimenDto>> GetSpecimensByTaxonomyNodeGuidAsync(IAuthentication? authentication, Guid taxonomyGuid);
}