using Zootify_Backend.DTOs.Anonym.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Services.Anonym.Interface;

namespace Zootify_Backend.Model.Services.Anonym.Classes;

public class AnonymSpecimenImageService : IAnonymSpecimenImageService
{
    private readonly ISpecimenImageRepository _specimenImageRepository;

    public AnonymSpecimenImageService(ISpecimenImageRepository specimenImageRepository)
    {
        _specimenImageRepository = specimenImageRepository;
    }

    public async Task<GetImageBase64ResponseDto> GetImageBase64Async(Guid guid)
    {
        var image = await _specimenImageRepository.GetSpecimenImageByGuid(guid);

        if (image == null)
        {
            throw new ZootifyException(ErrorCode.ImageNotFound);
        }

        return new GetImageBase64ResponseDto
        {
            Base64 = image.ImageBase64
        };
    }
}