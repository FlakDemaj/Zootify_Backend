using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Model.Services.Common.Classes;

public class CommonSpecimenImageService : ICommonSpecimenImageService
{
    private readonly ISpecimenImageRepository _specimenImageRepository;

    public CommonSpecimenImageService(ISpecimenImageRepository specimenImageRepository)
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
