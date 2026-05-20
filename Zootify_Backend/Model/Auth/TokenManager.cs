using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Zootify_Backend.Configuration;
using Zootify_Backend.DTOs.Auth.Response;
using Zootify_Backend.Enums;

namespace Zootify_Backend.Model.Auth;

/// <summary>
/// Creates and signs JWT access tokens using the configured HMAC-SHA256 key and token settings.
/// </summary>
public class TokenManager : ITokenManager
{
    private readonly JwtSecurityTokenHandler _tokenHandler;

    /// <summary>
    /// Initializes a new <see cref="TokenManager"/> and creates the underlying JWT handler.
    /// </summary>
    public TokenManager()
    {
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    /// <summary>
    /// Generates a signed JWT access token for the given user.
    /// </summary>
    /// <param name="userGuid">The unique identifier of the user.</param>
    /// <param name="userRole">The role assigned to the user.</param>
    /// <returns>A DTO containing the token string and its expiry duration in minutes.</returns>
    public TokenResult GenerateAccessToken(
        Guid userGuid,
        UserRole userRole)
    {
        var claims = CreateUserClaims(userGuid, userRole);

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(ZootifyConfiguration.AccessTokenKey));

        var creds = new SigningCredentials(key,
            SecurityAlgorithms.HmacSha256);

        var token = CreateToken(
            claims, creds);

        var accessToken = _tokenHandler.WriteToken(token);

        return new TokenResult
        {
            Token = accessToken,
            ExpiresInMinutes = ZootifyConfiguration.AccessTokenExpiresInMinutes
        };
    }

    /// <summary>
    /// Builds the <see cref="JwtSecurityToken"/> with issuer, audience, expiry, and signing credentials.
    /// </summary>
    private JwtSecurityToken CreateToken(
        List<Claim> claims,
        SigningCredentials creds)
    {
        var token = new JwtSecurityToken
        (
            issuer: ZootifyConfiguration.AccessTokenIssuer,
            audience: ZootifyConfiguration.AccessTokenAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(ZootifyConfiguration.AccessTokenExpiresInMinutes),
            signingCredentials: creds
        );

        return token;
    }

    /// <summary>
    /// Builds the standard claim set for a user token (subject, JTI, issued-at, role).
    /// </summary>
    private static List<Claim> CreateUserClaims(
        Guid userGuid,
        UserRole userRole)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userGuid.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),

            new(ClaimTypes.Role, userRole.ToString()),
        };

        return claims;
    }
}
