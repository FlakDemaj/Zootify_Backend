using Zootify_Backend.Enums;

namespace Zootify_Backend.Model.Auth;

/// <summary>
/// The interface for the authentication
/// </summary>
public interface IAuthentication
{
    public Guid UserGuid { get; }

    public UserRole Role { get; }
}