using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Zootify_Backend.Configuration;
using Zootify_Backend.DTOs.Response;
using Zootify_Backend.Enums;

namespace Zootify_Backend.Model.Auth;

public class TokenManager : ITokenManager
{
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public TokenManager()
    {
        _tokenHandler = new JwtSecurityTokenHandler();
    }
    
    public LoginUserResponseDto GenerateAccessToken(
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

        return new LoginUserResponseDto
        {
            Token = accessToken,
            ExpiresInMinutes = ZootifyConfiguration.AccessTokenExpiresInMinutes
        };
    }

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