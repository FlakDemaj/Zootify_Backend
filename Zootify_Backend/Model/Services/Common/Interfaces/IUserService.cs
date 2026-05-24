using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

public interface IUserService
{
    Task<GetUserResponseDto> GetUserByGuidAsync(
        IAuthentication authentication, 
        Guid guid);

    Task UpdateUserAsync(
        IAuthentication authentication,
        Guid userToUpdateGuid,
        UpdateUserRequestDto updateUserRequestDto);
}