using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Admin.Response;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Admin.Interfaces;

namespace Zootify_Backend.Controllers.Admin;

/// <summary>
/// Exposes user management endpoints for Admin and Moderator roles.
/// </summary>
public class AdminModeratorUserController : BaseRestController
{
    private readonly IAdminModeratorUserService _adminModeratorUserService;

    /// <summary>
    /// Initializes a new <see cref="AdminModeratorUserController"/> with its required service.
    /// </summary>
    /// <param name="adminModeratorUserService">The service used to manage user operations.</param>
    public AdminModeratorUserController(IAdminModeratorUserService adminModeratorUserService)
    {
        _adminModeratorUserService = adminModeratorUserService;
    }

    /// <summary>
    /// Returns a paged, filtered list of all users in the system.
    /// </summary>
    /// <param name="query">Filter and paging parameters.</param>
    /// <returns>A list of user response DTOs matching the filter criteria.</returns>
    [Authorize(Roles = $"{Roles.Admin},{Roles.Moderator}")]
    [HttpGet]
    public async Task<ActionResult<List<GetAllUserResponseDto>>> GetAllUsersAsync(
        [FromQuery] GetAllUsersFilterDto query)
    {
        var users = await _adminModeratorUserService.GetAllUsersAsync(query);
        return Ok(users);
    }

    /// <summary>
    /// Returns detailed information for a single user identified by their GUID.
    /// </summary>
    /// <param name="userGuid">The GUID of the user to retrieve.</param>
    /// <returns>A detailed user response DTO.</returns>
    [Authorize(Roles = $"{Roles.Admin},{Roles.Moderator}")]
    [HttpGet("{userGuid:guid}")]
    public async Task<ActionResult<GetUserResponseDto>> GetUserAsync(Guid userGuid)
    {
        var user = await _adminModeratorUserService.GetUserByGuidAsync(userGuid);
        return Ok(user);
    }

    /// <summary>
    /// Updates a user's data. Only an Admin may perform this action and cannot update themselves.
    /// </summary>
    /// <param name="userGuid">The GUID of the user to update.</param>
    /// <param name="updateUserRequestDto">The new data to apply to the user.</param>
    [Authorize(Roles = $"{Roles.Admin},{Roles.Moderator}")]
    [HttpPatch("{userGuid:guid}")]
    public async Task<IActionResult> UpdateUserAsync(
        Guid userGuid,
        [FromBody] UpdateUserRequestDto updateUserRequestDto)
    {
        await _adminModeratorUserService.UpdateUserAsync(
            Authentication,
            userGuid,
            updateUserRequestDto);

        return Ok();
    }

    /// <summary>
    /// Re-activates a previously deleted user account.
    /// </summary>
    /// <param name="userGuid">The GUID of the deleted user to activate.</param>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost("{userGuid:guid}")]
    public async Task<IActionResult> ActivateUserAsync(
        Guid userGuid)
    {
        await _adminModeratorUserService.ActivateUserAsync(
            Authentication,
            userGuid);

        return Ok();
    }
}
