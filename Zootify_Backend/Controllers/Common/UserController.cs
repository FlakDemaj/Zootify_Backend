using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Common;

/// <summary>
/// Controllers used by more than one roles
/// </summary>
[Authorize(Roles = $"{Roles.Moderator}, {Roles.Admin}, {Roles.User}")]
public class UserController : BaseRestController
{
    private readonly ICommonUserService _commonUserService;
    
    public UserController(ICommonUserService commonUserService) 
    {
        _commonUserService = commonUserService;
    }

    [HttpGet]
    public async Task<ActionResult<GetAllUserResponseDto>> GetUser()
    {
        //Hier könnte Ihre Werbung stehen
        return Ok(/*UserDto*/);
    }

    [HttpGet("common/{userGuid}")]
    public async Task<ActionResult<GetUserResponseDto>> GetUserAsync(Guid userGuid)
    {
        var user = await _commonUserService.GetUserByGuidAsync(Authentication,userGuid);
        return Ok(user);
    }
    
    [HttpPatch("common/updateOwnUser")]
    public async Task<IActionResult> UpdateUserAsync(
        Guid userGuid,
        [FromBody] UpdateOwnUserRequestDto updateOwnUserRequestDto)
    {
        await _commonUserService.UpdateOwnUserAsync(
            Authentication,
            updateOwnUserRequestDto);

        return Ok();
    }
    
    [HttpDelete("common/delete/{userGuid}")]
    public async Task<IActionResult> DeleteUserAsync(
        Guid userGuid)
    {
        await _commonUserService.DeleteUserAsync(
            Authentication,
            userGuid);

        return Ok();
    }
}