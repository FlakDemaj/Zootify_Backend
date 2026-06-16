namespace Zootify_Backend.Model.Auth;

/// <summary>
/// The result of issuing an authentication token: the token itself and its lifetime.
/// </summary>
public class TokenResult
{
    /// <summary>The issued (JWT) token string.</summary>
    public required string Token { get; init; }

    /// <summary>How many minutes from issuance until the token expires.</summary>
    public required int ExpiresInMinutes { get; init; }
}
