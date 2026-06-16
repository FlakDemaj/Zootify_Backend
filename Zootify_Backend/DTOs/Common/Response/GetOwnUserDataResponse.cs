namespace Zootify_Backend.DTOs.Common.Response;

/// <summary>
/// The current user's own basic profile data.
/// </summary>
public class GetOwnUserDataResponse
{
    /// <summary>The user's first name.</summary>
    public required string FirstName { get; init; }

    /// <summary>The user's last name.</summary>
    public required string LastName { get; init; }

    /// <summary>The user's email address.</summary>
    public required string Email { get; init; }
}
