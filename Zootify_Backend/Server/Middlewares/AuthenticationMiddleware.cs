using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Zootify_Backend.Enums;
using Zootify_Backend.Models.Auth;

namespace Zootify_Backend.Server.Middlewares;

public class AuthenticationMiddleware
{
    private RequestDelegate Next { get; }

    public AuthenticationMiddleware(RequestDelegate next)
    {
        Next = next;
    }

    public async Task Invoke(HttpContext context)
    {

        if (CheckIfAllowAnonymousExists(context))
        {
            await Next(context);
            return;
        }

        var userGuid = CheckUserGuid(context);

        var userRole = CheckUserRole(context);
        
        var authentication = new Authentication(
            userGuid,
            userRole);

        context.Items.Add(nameof(IAuthentication), authentication);

        await Next(context);
    }

    private static Guid CheckUserGuid(HttpContext context)
    {
        var userGuidString =
            context
                .User
                .Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?
                .Value;

        if (string.IsNullOrWhiteSpace(userGuidString) 
            || !Guid.TryParse(userGuidString, out var userGuid))
        {
            throw new ZootifyException(ErrorCode.DefaultErrorCode);
        }

        return userGuid;
    }

    private static AuthenticationType CheckUserRole(HttpContext context)
    {
        var userRoleString =
            context
                .User
                .Claims
                .FirstOrDefault(
                    c => c.Type == ClaimTypes.Role)?
                .Value;

        if (string.IsNullOrWhiteSpace(userRoleString) 
            || !Enum.TryParse<UserRole>(userRoleString, out var userRole))
        {
            throw new ZootifyException(ErrorCode.DefaultErrorCode);
        }

        switch (userRole)
        {
            case UserRole.Moderator:
                return AuthenticationType.ModeratorAuthentication;
            case UserRole.Admin:
                return AuthenticationType.AdminAuthentication;
            case UserRole.User:
                return AuthenticationType.UserAuthentication;
            default:
                throw new ZootifyException(ErrorCode.DefaultErrorCode);
        }
    }

    private static bool CheckIfAllowAnonymousExists(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            return true;
        }

        var allowAnonymous = endpoint.Metadata.Any(f => f is AllowAnonymousAttribute);
        return allowAnonymous;
    }

}
