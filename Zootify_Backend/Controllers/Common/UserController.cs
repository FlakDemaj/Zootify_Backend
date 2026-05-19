using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Common;

[Authorize(Roles = $"{Roles.Moderator}, {Roles.Admin}")]
public class UserController : BaseRestController
{
    private readonly IUserService _userService;
    
    public UserController(IUserService userService) 
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<GetAllUserResponseDto>> GetUser()
    {
        //Hier könnte Ihre Werbung stehen
        return Ok(/*UserDto*/);
    }

    [HttpGet("{userGuid}")]
    public async Task<ActionResult<GetUserResponseDto>> GetUser(Guid userGuid)
    {
        var user = await _userService.GetUserByGuidAsync(userGuid);
        return Ok(user);
    }
}