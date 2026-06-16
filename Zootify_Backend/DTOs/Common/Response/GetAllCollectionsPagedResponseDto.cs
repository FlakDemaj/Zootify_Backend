namespace Zootify_Backend.DTOs.Common.Response;

/// <summary>
/// A paged list of collections together with the total match count.
/// </summary>
public class GetAllCollectionsPagedResponseDto
{
    /// <summary>The total number of matching collections across all pages.</summary>
    public int TotalCount { get; init; }

    /// <summary>The collections on the current page.</summary>
    public List<GetCollectionResponseDto> Collections { get; init; } = [];
}
