using Zootify_Backend.Enums;

namespace Zootify_Backend.Models.Auth;

/// <summary>
/// The interface for the authentication
/// </summary>
public interface IAuthentication
{
    public Guid UserGuid { get; }

    public AuthenticationType AuthenticationType { get; }
}