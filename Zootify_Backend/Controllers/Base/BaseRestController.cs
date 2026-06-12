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

    protected IAuthentication? OptionalAuthentication
    {
        get
        {
            HttpContext.Items.TryGetValue(nameof(IAuthentication), out var value);
            return value as IAuthentication;
        }
    }
}