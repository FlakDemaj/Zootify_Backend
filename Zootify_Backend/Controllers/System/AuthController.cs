using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Request;
using Zootify_Backend.DTOs.Response;
using Zootify_Backend.Model.Services.Interfaces;

namespace Zootify_Backend.Controllers.System;

/// <summary>
/// Controller for managing the own user in the system area
/// Provides endpoint for put
/// </summary>
[AllowAnonymous]
public class AuthController : BaseRestController
{
    private IUserService UserService { get; }

    public AuthController(IUserService userService)
    {
        UserService = userService;
    }

    #region Registration

    [HttpPost("Register")]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] RegisterUserRequestDto registerUserRequestDto)
    {
        await UserService.RegisterAsync(registerUserRequestDto);
        return Created();
    }
    
    [HttpGet("confirm-registration")]
    public async Task<IActionResult> ConfirmRegistrationAsync(
        [FromQuery] string token)
    {
        await UserService.ConfirmRegistrationAsync(token);
        return Ok();
    }
    
    [HttpPost("new/registration-email")]
    public async Task<IActionResult> SendNewConfirmationRegistrationEmailAsync(
        [FromBody] SendNewRegistrationEmailRequestDto request)
    {
        await UserService.SendNewRegistrationEmailAsync(request);
        return NoContent();
    }

    #endregion

    #region Login

    [HttpPost("login")]
    public async Task<ActionResult<LoginUserResponseDto>> LoginAsync(
        [FromBody] LoginUserRequestDto user)
    {
        return Ok(await UserService.LoginAsync(user));
    }

    [HttpPost("new/reset-password")]
    public async Task<IActionResult> SendNewResetPasswordEmailAsync(
        [FromBody] SendNewResetPasswordEmailRequestDto request)
    {
        await UserService.SendNewResetPasswordEmailAsync(request);
        return NoContent();
    }
    
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPasswordAsync(
        [FromBody] ResetPasswordRequestDto request,
        [FromQuery] string resetPasswordToken)
    {
        await UserService.ResetPasswordAsync(request, resetPasswordToken);
        return Ok();
    }
    
    #endregion
}