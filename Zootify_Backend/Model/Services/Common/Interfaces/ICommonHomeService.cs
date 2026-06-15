using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

/// <summary>
/// Defines read operations for the public homepage animal overview.
/// </summary>
public interface ICommonHomeService
{
    /// <summary>
    /// Returns a paged public overview of specimens, newest first, optionally filtered by city.
    /// Non-public specimens are only visible to moderators and admins.
    /// </summary>
    /// <param name="authentication">The optional authentication context; anonymous or regular users see only public specimens.</param>
    /// <param name="filterByCity">Optional case-insensitive city name to filter by.</param>
    /// <param name="paging">Paging parameters (page and page size).</param>
    /// <returns>A paged response with the total count and the page of overview items.</returns>
    Task<GetPublicSpecimenOverviewResponseDto> GetPublicSpecimenOverviewAsync(
        IAuthentication? authentication,
        string? filterByCity,
        BaseSortAndPagingDto paging);
}
