namespace Zootify_Backend.DTOs.System.Request;

public class ResetPasswordRequestDto 
{
    public required string Password { get; set; }
    
    public required string RepeatedPassword { get; set; }

}