using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Admin;

[Authorize(Roles = Roles.Admin)]
public class UserController : BaseRestController
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("admin/update/{userGuid}")]
    public async Task<IActionResult> UpdateUserAsync(
        Guid userGuid,
        [FromBody] UpdateUserRequestDto updateUserRequestDto)
    {
        await _userService.UpdateUserAsync(
            Authentication,
            userGuid,
            updateUserRequestDto);

        return Ok();
    }
}