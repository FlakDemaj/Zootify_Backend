using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Model.Services.Common.Classes;

/// <summary>
/// Handles retrieval of specimen images stored as Base64-encoded strings.
/// </summary>
public class CommonSpecimenImageService : ICommonSpecimenImageService
{
    private readonly ISpecimenImageRepository _specimenImageRepository;

    /// <summary>
    /// Initializes a new <see cref="CommonSpecimenImageService"/> with its required repository.
    /// </summary>
    /// <param name="specimenImageRepository">Repository for specimen image persistence operations.</param>
    public CommonSpecimenImageService(ISpecimenImageRepository specimenImageRepository)
    {
        _specimenImageRepository = specimenImageRepository;
    }

    /// <summary>
    /// Returns the Base64-encoded image data for the specimen image with the given GUID.
    /// </summary>
    /// <param name="guid">The GUID of the specimen image to retrieve.</param>
    /// <returns>A response DTO containing the Base64-encoded image string.</returns>
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
