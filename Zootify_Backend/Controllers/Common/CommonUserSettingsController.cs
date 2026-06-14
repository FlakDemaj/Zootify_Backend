using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Common;

/// <summary>
/// Exposes user settings management endpoints accessible to all authenticated roles.
/// </summary>
[Authorize(Roles = $"{Roles.Moderator}, {Roles.Admin}, {Roles.User}")]
public class CommonUserSettingsController : BaseRestController
{
    private readonly ICommonUserSettingsService _commonUserSettingsService;

    /// <summary>
    /// Initializes a new <see cref="CommonUserSettingsController"/> with its required service.
    /// </summary>
    /// <param name="commonUserSettingsService">The service used to manage user settings.</param>
    public CommonUserSettingsController(ICommonUserSettingsService commonUserSettingsService)
    {
        _commonUserSettingsService = commonUserSettingsService;
    }

    /// <summary>
    /// Returns the authenticated user's personal settings.
    /// </summary>
    /// <returns>The user's personal settings.</returns>
    [HttpGet]
    public async Task<ActionResult<GetUserSettingsResponseDto>> GetUserSettingsAsync()
    {
        return Ok(await _commonUserSettingsService.GetUserSettingsAsync(Authentication));
    }

    /// <summary>
    /// Updates the authenticated user's personal settings such as dark mode and language preference.
    /// </summary>
    /// <param name="requestDto">The new settings values to apply.</param>
    [HttpPatch]
    public async Task<IActionResult> UpdateUserSettingsAsync(
        [FromBody] UpdateUserSettingsRequestDto requestDto)
    {
        await _commonUserSettingsService.UpdateUserSettingsAsync(
            Authentication,
            requestDto);

        return Ok();
    }
    
    /// <summary>
    /// Updates the authenticated user's language.
    /// </summary>
    /// <param name="requestDto">The new language value to apply.</param>
    [HttpPatch("language")]
    public async Task<IActionResult> UpdateUserSettingsLanguageAsync(
        [FromBody] UpdateUserSettingsLanguageRequestDto requestDto)
    {
        await _commonUserSettingsService.UpdateUserSettingsLanguageAsync(
            Authentication,
            requestDto);

        return Ok();
    }
}
