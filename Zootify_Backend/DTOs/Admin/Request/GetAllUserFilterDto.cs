using Zootify_Backend.DTOs.Base;
using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Admin.Request;

public class GetAllUsersFilterDto : BaseSortAndPagingDto
{
    public string? FirstName { get; init; }
    
    public string? LastName { get; init; }
    
    public string? Email { get; init; }
    
    public UserRole? Role { get; init; }
    
    public UserStatus? Status { get; init; }
    
}