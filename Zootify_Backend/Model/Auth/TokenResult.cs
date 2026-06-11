namespace Zootify_Backend.Model.Auth;

public class TokenResult
{
    public required string Token { get; init; }
    
    public required int ExpiresInMinutes { get; init; }

}