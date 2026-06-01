using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Common;

[Authorize(Roles = $"{Roles.Moderator}, {Roles.Admin}, {Roles.User}")]
public class CommonUserSettingsController : BaseRestController
{
    private readonly ICommonUserSettingsService _commonUserSettingsService;

    public CommonUserSettingsController(ICommonUserSettingsService commonUserSettingsService)
    {
        _commonUserSettingsService = commonUserSettingsService;
    }

    [HttpPatch]
    public async Task<IActionResult> UpdateUserSettingsAsync(
        [FromBody] UpdateUserSettingsRequestDto requestDto)
    {
        await _commonUserSettingsService.UpdateUserSettingsAsync(
            Authentication,
            requestDto);

        return Ok();
    }
}
