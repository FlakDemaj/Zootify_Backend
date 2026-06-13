using Zootify_Backend.Enums;

namespace Zootify_Backend.Model.Auth;

/// <summary>
/// The interface for the authentication
/// </summary>
public interface IAuthentication
{
    /// <summary>
    /// Gets the unique identifier of the authenticated user.
    /// </summary>
    public Guid UserGuid { get; }

    /// <summary>
    /// Gets the role assigned to the authenticated user.
    /// </summary>
    public UserRole Role { get; }
}