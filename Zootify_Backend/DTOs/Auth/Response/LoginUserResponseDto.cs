using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.DTOs.Auth.Response;

public class LoginUserResponseDto
{
    public required TokenResult Token { get; set; }
    
    public required string FirstName { get; set; }
    
    public required string LastName { get; set; }

}