using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

public interface ICommonSpecimenService
{
    Task<CreatedObjectResponseDto> CreateSpecimenAsync(CreateSpecimenRequestDto request);

    Task<List<GetAllTagsByFilterDto>> GetAllTagsAsync(string startsWith);
    
}