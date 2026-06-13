using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Server.Middlewares;

/// <summary>
/// Middleware that builds an <see cref="IAuthentication"/> object from the JWT claims and stores it in the HTTP context,
/// or throws if the endpoint requires authentication but no valid claims are present.
/// </summary>
public class AuthenticationMiddleware
{
    private RequestDelegate Next { get; }

    /// <summary>
    /// Initializes a new <see cref="AuthenticationMiddleware"/> with the next middleware delegate.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public AuthenticationMiddleware(RequestDelegate next)
    {
        Next = next;
    }

    /// <summary>
    /// Attempts to build an <see cref="IAuthentication"/> from the request claims and adds it to the context items,
    /// then passes the request to the next middleware.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
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

    /// <summary>
    /// Attempts to extract the user GUID and role from the JWT claims and construct an <see cref="Authentication"/> object.
    /// </summary>
    /// <param name="context">The current HTTP context containing the user's claims principal.</param>
    /// <returns>An <see cref="Authentication"/> instance if claims are valid; otherwise <c>null</c>.</returns>
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

    /// <summary>
    /// Checks whether the matched endpoint is decorated with <see cref="AllowAnonymousAttribute"/>.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns><c>true</c> if the endpoint allows anonymous access or no endpoint is matched; otherwise <c>false</c>.</returns>
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
