using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Auth.Request;
using Zootify_Backend.DTOs.Auth.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.System.Interfaces;

namespace Zootify_Backend.Controllers.System;

/// <summary>
/// Controller for managing authentication and user registration flows.
/// </summary>
public class AuthController : BaseRestUserController
{

    #region Registration

    public AuthController(IUserService userService) : base(userService)
    {
    }

    /// <summary>
    /// Registers a new user. Returns 201 Created on success.
    /// </summary>
    [HttpPost("Register")]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] RegisterUserRequestDto registerUserRequestDto)
    {
        await UserService.RegisterAsync(registerUserRequestDto);
        return Created();
    }

    /// <summary>
    /// Confirms a user's registration using the token sent by email.
    /// Returns 200 OK on success.
    /// </summary>
    [HttpGet("confirm-registration")]
    public async Task<IActionResult> ConfirmRegistrationAsync(
        [FromQuery] string token)
    {
        await UserService.ConfirmRegistrationAsync(token);
        return Ok();
    }

    /// <summary>
    /// Sends a new registration confirmation email to a pending user.
    /// Returns 204 No Content on success.
    /// </summary>
    [HttpPost("new-registration-email")]
    public async Task<IActionResult> SendNewConfirmationRegistrationEmailAsync(
        [FromBody] SendNewRegistrationEmailRequestDto request)
    {
        await UserService.SendNewRegistrationEmailAsync(request);
        return NoContent();
    }

    #endregion

    #region Login

    /// <summary>
    /// Authenticates a user with email and password.
    /// Returns 200 OK with a JWT token on success.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginUserResponseDto>> LoginAsync(
        [FromBody] LoginUserRequestDto user)
    {
        return Ok(await UserService.LoginAsync(user));
    }

    /// <summary>
    /// Sends a password-reset email to the given user.
    /// Returns 204 No Content on success.
    /// </summary>
    [HttpPost("new/reset-password")]
    public async Task<IActionResult> SendNewResetPasswordEmailAsync(
        [FromBody] SendNewResetPasswordEmailRequestDto request)
    {
        await UserService.SendNewResetPasswordEmailAsync(request);
        return NoContent();
    }

    /// <summary>
    /// Resets the user's password using a valid reset token.
    /// Returns 200 OK on success.
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPasswordAsync(
        [FromQuery] string token,
        [FromBody] ResetPasswordRequestDto request)
    {
        await UserService.ResetPasswordAsync(request, token);
        return Ok();
    }

    #endregion
}
