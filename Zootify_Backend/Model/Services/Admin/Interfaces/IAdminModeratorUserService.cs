using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Admin.Response;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Model.Services.Admin.Interfaces;

public interface IAdminModeratorUserService
{
    Task<GetUserResponseDto> GetUserByGuidAsync(Guid guid);

    Task<List<GetAllUserResponseDto>> GetAllUsersAsync(GetAllUsersFilterDto query);

    Task UpdateUserAsync(
        IAuthentication authentication,
        Guid userToUpdateGuid,
        UpdateUserRequestDto updateUserRequestDto);

    Task ActivateUserAsync(
        IAuthentication authentication,
        Guid userToActivateGuid);
}
