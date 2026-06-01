using Zootify_Backend.DTOs.Base;
using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response;

public class GetAllUsersFilterDto : BaseSortAndPagingDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public UserRole? Role { get; set; }
    public UserStatus? Status { get; set; }
    
}