namespace Zootify_Backend.DTOs.Common.Response;

/// <summary>
/// A paged list of public specimen overviews together with the total match count.
/// </summary>
public class GetPublicSpecimenOverviewResponseDto
{
    /// <summary>The total number of matching specimens across all pages.</summary>
    public int TotalCount { get; init; }

    /// <summary>The specimen overviews on the current page.</summary>
    public List<GetPublicSpecimenOverviewDto> Specimens { get; init; } = [];
}
