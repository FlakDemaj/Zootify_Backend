using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Admin;

[Authorize(Roles = Roles.Admin)]
public class UserController : BaseRestController
{
    private readonly IAdminUserServices _adminUserServices;

    public UserController(IAdminUserServices adminUserServices)
    {
        _adminUserServices = adminUserServices;
    }

    [HttpPatch("admin/update/{userGuid}")]
    public async Task<IActionResult> UpdateUserAsync(
        Guid userGuid,
        [FromBody] UpdateUserRequestDto updateUserRequestDto)
    {
        await _adminUserServices.UpdateUserAsync(
            Authentication,
            userGuid,
            updateUserRequestDto);

        return Ok();
    }
    
    [HttpPost("admin/activate/{userGuid}")]
    public async Task<IActionResult> ActivateUserAsync(
        Guid userGuid)
    {
        await _adminUserServices.ActivateUserAsync(
            Authentication,
            userGuid);

        return Ok();
    }
}