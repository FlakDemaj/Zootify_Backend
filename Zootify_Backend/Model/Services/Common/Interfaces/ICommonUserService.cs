using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

public interface ICommonUserService
{
    Task<GetUserResponseDto> GetUserByGuidAsync(
        IAuthentication authentication, 
        Guid guid);

    Task UpdateOwnUserAsync(
        IAuthentication authentication,
        UpdateOwnUserRequestDto updateOwnUserRequestDto);

    Task DeleteUserAsync(
        IAuthentication authentication,
        Guid userToDeleteGuid);

    Task UpdateUserAsync(
        UserEntity newUser,
        UserEntity oldUser,
        Guid requesterGuid);
    
    Task<GetOwnUserData> GetOwnUserDataAsync(IAuthentication authentication);
}