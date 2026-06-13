using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Common;

/// <summary>
/// Exposes user self-service endpoints accessible to all authenticated roles.
/// </summary>
[Authorize(Roles = $"{Roles.Moderator}, {Roles.Admin}, {Roles.User}")]
public class CommonUserController : BaseRestController
{
    private readonly ICommonUserService _commonUserService;

    /// <summary>
    /// Initializes a new <see cref="CommonUserController"/> with its required service.
    /// </summary>
    /// <param name="commonUserService">The service used to manage user self-service operations.</param>
    public CommonUserController(ICommonUserService commonUserService)
    {
        _commonUserService = commonUserService;
    }

    /// <summary>
    /// Updates the authenticated user's own profile data.
    /// </summary>
    /// <param name="userGuid">The GUID of the user (unused route parameter; identity is taken from the token).</param>
    /// <param name="updateOwnUserRequestDto">The new profile data to apply.</param>
    [HttpPatch]
    public async Task<IActionResult> UpdateOwnUserAsync(
        Guid userGuid,
        [FromBody] UpdateOwnUserRequestDto updateOwnUserRequestDto)
    {
        await _commonUserService.UpdateOwnUserAsync(
            Authentication,
            updateOwnUserRequestDto);

        return Ok();
    }

    /// <summary>
    /// Soft-deletes a user account. Admins may delete any user; regular users may only delete themselves.
    /// </summary>
    /// <param name="userGuid">The GUID of the user to delete.</param>
    [HttpDelete("{userGuid}")]
    public async Task<IActionResult> DeleteUserAsync(
        Guid userGuid)
    {
        await _commonUserService.DeleteUserAsync(
            Authentication,
            userGuid);

        return Ok();
    }

    /// <summary>
    /// Returns the authenticated user's own profile and settings data.
    /// </summary>
    /// <returns>A response DTO containing the current user's data.</returns>
    [HttpGet("ownData")]
    public async Task<ActionResult<GetOwnUserDataResponse>> GetOwnUserDataAsync()
    {
        var userData = await _commonUserService
            .GetOwnUserDataAsync(Authentication);
        return Ok(userData);
    }
}