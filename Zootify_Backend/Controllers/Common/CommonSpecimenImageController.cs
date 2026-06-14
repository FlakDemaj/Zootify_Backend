using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Common;

/// <summary>
/// Exposes specimen image retrieval endpoints accessible to all authenticated roles and to anonymous users.
/// </summary>
[Authorize(Roles = $"{Roles.Moderator}, {Roles.Admin}, {Roles.User}")]
public class CommonSpecimenImageController : BaseRestController
{
    private readonly ICommonSpecimenImageService _commonSpecimenImageService;

    /// <summary>
    /// Initializes a new <see cref="CommonSpecimenImageController"/> with its required service.
    /// </summary>
    /// <param name="commonSpecimenImageService">The service used to retrieve specimen images.</param>
    public CommonSpecimenImageController(ICommonSpecimenImageService commonSpecimenImageService)
    {
        _commonSpecimenImageService = commonSpecimenImageService;
    }

    /// <summary>
    /// Returns the Base64-encoded image data for the specimen image with the given GUID.
    /// </summary>
    /// <param name="imageGuid">The GUID of the specimen image to retrieve.</param>
    /// <returns>A response DTO containing the Base64-encoded image string.</returns>
    [AllowAnonymous]
    [HttpGet("{imageGuid:guid}")]
    public async Task<ActionResult<GetImageBase64ResponseDto>> GetSpecimenImageAsync(Guid imageGuid)
    {
        return Ok(await _commonSpecimenImageService.GetImageBase64Async(imageGuid));
    }
}
