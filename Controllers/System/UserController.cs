using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Request;
using Zootify_Backend.DTOs.Response;
using Zootify_Backend.Models.Services.Interfaces;

namespace Zootify_Backend.Controllers.System;

/// <summary>
/// Controller for managing the own user in the system area
/// Provides endpoint for put
/// </summary>
public class UserController : BaseRestController
{
    private IUserService UserService { get; }

    public UserController(IUserService userService)
    {
        UserService = userService;
    }

    [AllowAnonymous]
    [HttpPost("Register")]
    public async Task<ActionResult<RegisterUserResponseDto>> RegisterAsync(
        [FromBody] RegisterUserRequestDto registerUserRequestDto)
    {
        return Ok(await UserService.RegisterAsync(registerUserRequestDto));
    }
    
    [AllowAnonymous]
    [HttpGet("confirm-registration")]
    public async Task<IActionResult> ConfirmRegistrationAsync([FromQuery] string token)
    {
        return Ok(await UserService.ConfirmRegistrationAsync(token));
    }
}