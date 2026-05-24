namespace Zootify_Backend.DTOs.Common.Response;

public class GetOwnUserData
{
    public required string FirstName { get; set; }

    public required string LastName { get; set; }
    
    public required string Email { get; set; }
}