using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Admin.Response;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Admin.Interfaces;

namespace Zootify_Backend.Controllers.Admin;

public class AdminModeratorUserController : BaseRestController
{
    private readonly IAdminModeratorUserService _adminModeratorUserService;

    public AdminModeratorUserController(IAdminModeratorUserService adminModeratorUserService)
    {
        _adminModeratorUserService = adminModeratorUserService;
    }

    [Authorize(Roles = $"{Roles.Admin},{Roles.Moderator}")]
    [HttpGet]
    public async Task<ActionResult<List<GetAllUserResponseDto>>> GetAllUsersAsync(
        [FromQuery] GetAllUsersFilterDto query)
    {
        var users = await _adminModeratorUserService.GetAllUsersAsync(query);
        return Ok(users);
    }

    [Authorize(Roles = $"{Roles.Admin},{Roles.Moderator}")]
    [HttpGet("{userGuid:guid}")]
    public async Task<ActionResult<GetUserResponseDto>> GetUserAsync(Guid userGuid)
    {
        var user = await _adminModeratorUserService.GetUserByGuidAsync(userGuid);
        return Ok(user);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPatch("{userGuid:guid}")]
    public async Task<IActionResult> UpdateUserAsync(
        Guid userGuid,
        [FromBody] UpdateUserRequestDto updateUserRequestDto)
    {
        await _adminModeratorUserService.UpdateUserAsync(
            Authentication,
            userGuid,
            updateUserRequestDto);

        return Ok();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("{userGuid:guid}")]
    public async Task<IActionResult> ActivateUserAsync(
        Guid userGuid)
    {
        await _adminModeratorUserService.ActivateUserAsync(
            Authentication,
            userGuid);

        return Ok();
    }
}
