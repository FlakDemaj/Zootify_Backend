using Zootify_Backend.DTOs.Response;
using Zootify_Backend.Enums;

namespace Zootify_Backend.Model.Auth;

public interface ITokenManager
{
    public LoginUserResponseDto GenerateAccessToken(
        Guid userGuid,
        UserRole userRole);
}