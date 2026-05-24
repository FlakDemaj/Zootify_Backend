using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Model.Services.Admin.Interfaces;

public interface IAdminUserServices
{
    Task UpdateUserAsync(
        IAuthentication authentication,
        Guid userToUpdateGuid,
        UpdateUserRequestDto updateUserRequestDto);

    Task ActivateUserAsync(
        IAuthentication authentication,
        Guid userToDeleteGuid);
}