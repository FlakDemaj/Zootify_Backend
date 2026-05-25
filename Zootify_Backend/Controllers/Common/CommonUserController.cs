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
public class CommonUserController : BaseRestController
{
    private readonly ICommonUserService _commonUserService;
    
    public CommonUserController(ICommonUserService commonUserService) 
    {
        _commonUserService = commonUserService;
    }

    [HttpGet]
    public async Task<ActionResult<List<GetAllUserResponseDto>>> 
        GetAllUsersAsync(
            [FromQuery] GetAllUsersFilterDto query)
    {
        var users = await _commonUserService
            .GetAllUsersAsync(
                Authentication,
                query);
        return Ok(users);
    }

    [HttpGet("{userGuid:guid}")]
    public async Task<ActionResult<GetUserResponseDto>> GetUserAsync(Guid userGuid)
    {
        var user = await _commonUserService.GetUserByGuidAsync(Authentication,userGuid);
        return Ok(user);
    }
    
    [HttpPatch]
    public async Task<IActionResult> UpdateUserAsync(
        Guid userGuid,
        [FromBody] UpdateOwnUserRequestDto updateOwnUserRequestDto)
    {
        await _commonUserService.UpdateOwnUserAsync(
            Authentication,
            updateOwnUserRequestDto);

        return Ok();
    }
    
    [HttpDelete("{userGuid}")]
    public async Task<IActionResult> DeleteUserAsync(
        Guid userGuid)
    {
        await _commonUserService.DeleteUserAsync(
            Authentication,
            userGuid);

        return Ok();
    }

    [HttpGet("ownData")]
    public async Task<ActionResult<GetOwnUserData>> GetOwnUserDataAsync()
    {
        var userData = await _commonUserService
            .GetOwnUserDataAsync(Authentication);
        return Ok(userData);
    }
}