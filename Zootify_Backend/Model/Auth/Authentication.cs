using Zootify_Backend.Enums;

namespace Zootify_Backend.Model.Auth;

/// <summary>
/// The authentication object
/// </summary>
public class Authentication : IAuthentication
{
    /// <summary>
    /// Gets the unique identifier of the authenticated user.
    /// </summary>
    public Guid UserGuid { get; }

    /// <summary>
    /// Gets the role assigned to the authenticated user.
    /// </summary>
    public UserRole Role { get; }

    /// <summary>
    /// Initializes a new <see cref="Authentication"/> with the user's GUID and authentication type.
    /// </summary>
    public Authentication(Guid userGuid, UserRole role)
    {
        UserGuid = userGuid;
        Role = role;
    }
}
