using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.System.Request;
using Zootify_Backend.DTOs.System.Response;
using Zootify_Backend.Model.Services.System.Interfaces;

namespace Zootify_Backend.Controllers.System;

/// <summary>
/// Controller for managing authentication and user registration flows.
/// </summary>
[AllowAnonymous]
public class AuthController : BaseRestController
{
    private readonly ISystemAuthService _systemAuthService;

    /// <summary>
    /// Initializes a new <see cref="AuthController"/> with its required service.
    /// </summary>
    /// <param name="systemAuthService">The service handling authentication and user registration logic.</param>
    public AuthController(ISystemAuthService systemAuthService)
    {
        _systemAuthService = systemAuthService;
    }
    
    #region Registration
    

    /// <summary>
    /// Registers a new user. Returns 201 Created on success.
    /// </summary>
    /// <param name="registerUserRequestDto">The registration data including email, name, and password.</param>
    [HttpPost("Register")]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] RegisterUserRequestDto registerUserRequestDto)
    {
        await _systemAuthService.RegisterAsync(registerUserRequestDto);
        return Created();
    }

    /// <summary>
    /// Confirms a user's registration using the token sent by email.
    /// Returns 200 OK on success.
    /// </summary>
    /// <param name="token">The registration token sent to the user by email.</param>
    [HttpGet("confirm-registration")]
    public async Task<IActionResult> ConfirmRegistrationAsync(
        [FromQuery] string token)
    {
        await _systemAuthService.ConfirmRegistrationAsync(token);
        return Ok();
    }
    
    /// <summary>
    /// Sends a new registration confirmation email to a pending user.
    /// Returns 204 No Content on success.
    /// </summary>
    /// <param name="request">Contains the email address of the pending user.</param>
    [HttpPost("new-registration-email")]
    public async Task<IActionResult> SendNewConfirmationRegistrationEmailAsync(
        [FromBody] SendNewRegistrationEmailRequestDto request)
    {
        await _systemAuthService.SendNewRegistrationEmailAsync(request);
        return NoContent();
    }

    #endregion

    #region Login

    /// <summary>
    /// Authenticates a user with email and password.
    /// Returns 200 OK with a JWT token on success.
    /// </summary>
    /// <param name="user">The login credentials including email and password.</param>
    /// <returns>A DTO containing the user's name, JWT token, and settings.</returns>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginUserResponseDto>> LoginAsync(
        [FromBody] LoginUserRequestDto user)
    {
        return Ok(await _systemAuthService.LoginAsync(user));
    }

    /// <summary>
    /// Sends a password-reset email to the given user.
    /// Returns 204 No Content on success.
    /// </summary>
    /// <param name="request">Contains the email address of the user requesting a password reset.</param>
    [HttpPost("new/reset-password")]
    public async Task<IActionResult> SendNewResetPasswordEmailAsync(
        [FromBody] SendNewResetPasswordEmailRequestDto request)
    {
        await _systemAuthService.SendNewResetPasswordEmailAsync(request);
        return NoContent();
    }

    /// <summary>
    /// Resets the user's password using a valid reset token.
    /// Returns 200 OK on success.
    /// </summary>
    /// <param name="token">The reset token sent to the user by email.</param>
    /// <param name="request">The new password data including the password and its confirmation.</param>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPasswordAsync(
        [FromQuery] string token,
        [FromBody] ResetPasswordRequestDto request)
    {
        await _systemAuthService.ResetPasswordAsync(request, token);
        return Ok();
    }

    #endregion
}
