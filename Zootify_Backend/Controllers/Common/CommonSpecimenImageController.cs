using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Common;

[Authorize(Roles = $"{Roles.Moderator}, {Roles.Admin}, {Roles.User}")]
public class CommonSpecimenImageController : BaseRestController
{
    private readonly ICommonSpecimenImageService _commonSpecimenImageService;

    public CommonSpecimenImageController(ICommonSpecimenImageService commonSpecimenImageService)
    {
        _commonSpecimenImageService = commonSpecimenImageService;
    }

    [AllowAnonymous]
    [HttpGet("{imageGuid:guid}")]
    public async Task<ActionResult<GetImageBase64ResponseDto>> GetSpecimenImageAsync(Guid imageGuid)
    {
        return Ok(await _commonSpecimenImageService.GetImageBase64Async(imageGuid));
    }
}
