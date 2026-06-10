using Zootify_Backend.Enums;

namespace Zootify_Backend.Model.Auth;

/// <summary>
/// Provides JWT access token generation for authenticated users.
/// </summary>
public interface ITokenManager
{
    /// <summary>
    /// Generates a signed JWT access token for the given user.
    /// </summary>
    /// <param name="userGuid">The unique identifier of the user.</param>
    /// <param name="userRole">The role assigned to the user.</param>
    /// <returns>A DTO containing the token string and its expiry duration in minutes.</returns>
    public TokenResult GenerateAccessToken(
        Guid userGuid,
        UserRole userRole);
}
