using Zootify_Backend.DTOs.Base;
using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Admin.Request;

/// <summary>
/// Filter, sort, and paging parameters for listing users (admin view).
/// </summary>
public class GetAllUsersFilterDto : BaseSortAndPagingDto
{
    /// <summary>Optional first-name fragment to filter by.</summary>
    public string? FirstName { get; init; }

    /// <summary>Optional last-name fragment to filter by.</summary>
    public string? LastName { get; init; }

    /// <summary>Optional email fragment to filter by.</summary>
    public string? Email { get; init; }

    /// <summary>Optional role to filter by.</summary>
    public UserRole? Role { get; init; }

    /// <summary>Optional account status to filter by.</summary>
    public UserStatus? Status { get; init; }
}
