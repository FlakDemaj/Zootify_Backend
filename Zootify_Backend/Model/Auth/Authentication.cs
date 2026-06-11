using Zootify_Backend.Enums;

namespace Zootify_Backend.Model.Auth;

/// <summary>
/// The authentication object
/// </summary>
public class Authentication : IAuthentication
{
    public Guid UserGuid { get; }

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
