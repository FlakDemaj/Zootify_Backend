using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

public interface ICommonUserSettingsService
{
    Task UpdateUserSettingsAsync(
        IAuthentication authentication,
        UpdateUserSettingsRequestDto requestDto);
}
