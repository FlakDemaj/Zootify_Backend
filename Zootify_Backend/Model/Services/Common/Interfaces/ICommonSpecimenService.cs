using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

public interface ICommonSpecimenService
{
    Task<CreatedObjectResponseDto> CreateSpecimenAsync(
        IAuthentication authentication,
        CreateSpecimenRequestDto request);

    Task<List<GetAllTagsByFilterDto>> GetAllTagsAsync(string startsWith);

    Task<List<GetAllSpecimenDto>> GetAllSpecimensByStatusAsync(
        IAuthentication authentication,
        SpecimenStatus status);
    
    Task<List<GetAllSpecimenDto>> GetOwnSpecimensAsync(
        IAuthentication authentication);

}