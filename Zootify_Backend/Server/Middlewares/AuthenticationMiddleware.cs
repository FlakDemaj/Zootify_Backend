using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;

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
        var authentication = TryBuildAuthentication(context);

        if (authentication != null)
        {
            context.Items.Add(nameof(IAuthentication), authentication);
        }
        else if (!CheckIfAllowAnonymousExists(context))
        {
            throw new ZootifyException(ErrorCode.DefaultErrorCode);
        }

        await Next(context);
    }

    private static Authentication? TryBuildAuthentication(HttpContext context)
    {
        var userGuidString = context.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var userRoleString = context.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        if (string.IsNullOrWhiteSpace(userGuidString)
            || !Guid.TryParse(userGuidString, out var userGuid))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(userRoleString)
            || !Enum.TryParse<UserRole>(userRoleString, out var userRole))
        {
            return null;
        }

        return new Authentication(userGuid, userRole);
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
