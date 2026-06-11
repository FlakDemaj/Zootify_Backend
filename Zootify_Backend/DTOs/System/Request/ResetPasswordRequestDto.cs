namespace Zootify_Backend.DTOs.System.Request;

public class ResetPasswordRequestDto 
{
    public required string Password { get; init; }
    
    public required string RepeatedPassword { get; init; }

}