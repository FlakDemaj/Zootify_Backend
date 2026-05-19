using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.System.Interfaces;

namespace Zootify_Backend.Controllers.Common;

[Authorize(Roles = $"{Roles.Moderator}, {Roles.Admin}")]
public class UserController : BaseRestUserController
{
    public UserController(IUserService userService) 
        : base(userService)
    {
    }

    [HttpGet("Users")]
    public async Task<ActionResult<GetAllUserResponseDto>> GetUser()
    {
        //Hier könnte Ihre Werbung stehen
        return Ok(/*UserDto*/);
    }
}