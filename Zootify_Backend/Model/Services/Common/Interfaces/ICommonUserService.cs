using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

public interface ICommonUserService
{
    Task UpdateOwnUserAsync(
        IAuthentication authentication,
        UpdateOwnUserRequestDto updateOwnUserRequestDto);

    Task DeleteUserAsync(
        IAuthentication authentication,
        Guid userToDeleteGuid);

    Task<GetOwnUserDataResponse> GetOwnUserDataAsync(IAuthentication authentication);
}