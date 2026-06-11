using Zootify_Backend.DTOs.Anonym.Response;

namespace Zootify_Backend.Model.Services.Anonym.Interface;

public interface IAnonymSpecimenImageService
{
    Task<GetImageBase64ResponseDto> GetImageBase64Async(Guid guid);
}