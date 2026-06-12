namespace Zootify_Backend.DTOs.Common.Response;

public class GetOwnUserDataResponse
{
    public required string FirstName { get; init; }

    public required string LastName { get; init; }
    
    public required string Email { get; init; }
}