namespace Zootify_Backend.Model.Auth;

public class TokenResult
{
    public required string Token { get; set; }
    
    public required int ExpiresInMinutes { get; set; }

}