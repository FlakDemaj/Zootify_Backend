namespace Zootify_Backend.DTOs.Auth.Request;

public class ResetPasswordRequestDto 
{
    public required string Password { get; set; }
    
    public required string RepeatedPassword { get; set; }

}