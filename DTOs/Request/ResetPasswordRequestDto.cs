namespace Zootify_Backend.DTOs.Request;

public class ResetPasswordRequestDto 
{
    public required string Password { get; set; }
    
    public required string RepeatedPassword { get; set; }

}