using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Controllers.Base;

/// <summary>
/// Base rest controller, that provides the endpoints
/// </summary>
[Route("rest/[controller]/")]
public class BaseRestController : ControllerBase
{
    /// <summary>
    /// Gets the authentication, out of the httpContext, from the user who requested
    /// </summary>
    protected IAuthentication Authentication
    {
        get
        {
            HttpContext.Items.TryGetValue(nameof(IAuthentication), out var value);
            if (value is IAuthentication authentication)
            {
                return authentication;
            }

            throw new ZootifyException(ErrorCode.DefaultErrorCode);
        }
    }

    protected IAuthentication? TryGetAuthentication()
    {
        HttpContext.Items.TryGetValue(nameof(IAuthentication), out var value);
        if (value is IAuthentication authentication)
            return authentication;

        if (HttpContext.User.Identity?.IsAuthenticated != true)
            return null;

        var guidString = HttpContext.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var roleString = HttpContext.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        if (!Guid.TryParse(guidString, out var userGuid)
            || !Enum.TryParse<UserRole>(roleString, out var role))
            return null;

        return new Authentication(userGuid, role);
    }
}