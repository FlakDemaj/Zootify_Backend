using Zootify_Backend.Enums;

namespace Zootify_Backend.Models.Auth;

/// <summary>
/// The authentication object
/// </summary>
public class Authentication : IAuthentication
{
    public Guid UserGuid { get; }

    public AuthenticationType AuthenticationType { get; set; }

    /// <summary>
    /// Initializes a new <see cref="Authentication"/> with the user's GUID and authentication type.
    /// </summary>
    public Authentication(Guid userGuid, AuthenticationType authenticationType)
    {
        UserGuid = userGuid;
        AuthenticationType = authenticationType;
    }
}
