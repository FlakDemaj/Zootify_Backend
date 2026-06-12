using Zootify_Backend.DTOs.Common.Response;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

public interface ICommonSpecimenImageService
{
    Task<GetImageBase64ResponseDto> GetImageBase64Async(Guid guid);
}
