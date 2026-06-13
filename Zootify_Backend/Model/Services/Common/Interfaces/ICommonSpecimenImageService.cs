using Zootify_Backend.DTOs.Common.Response;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

/// <summary>
/// Defines operations for retrieving specimen images.
/// </summary>
public interface ICommonSpecimenImageService
{
    /// <summary>
    /// Returns the Base64-encoded image data for the specimen image with the given GUID.
    /// </summary>
    /// <param name="guid">The GUID of the specimen image to retrieve.</param>
    /// <returns>A response DTO containing the Base64-encoded image string.</returns>
    Task<GetImageBase64ResponseDto> GetImageBase64Async(Guid guid);
}
